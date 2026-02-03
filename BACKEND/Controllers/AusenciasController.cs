using System.Globalization;
using System.Security.Claims;
using FichajeApp.Data;
using FichajeApp.DTOs;
using FichajeApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FichajeApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AusenciasController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AusenciasController(ApplicationDbContext db) => _db = db;

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : 0;

        private bool IsAdmin =>
            User.IsInRole("Admin")
            || User.HasClaim(c => c.Type == "role" && c.Value == "Admin")
            || User.HasClaim(c => c.Type == "roles" && c.Value == "Admin");

        private static AusenciaDto Map(Ausencia a) => new AusenciaDto
        {
            Id = a.Id,
            EmpleadoId = a.EmpleadoId,
            EmpleadoNombre = a.Empleado != null ? $"{a.Empleado.Nombre} {a.Empleado.Apellidos}" : null,
            FechaInicio = a.FechaInicio,
            FechaFin = a.FechaFin,
            Motivo = a.Motivo,
            Aceptada = a.Aceptada
        };

        // Nuevo helper: parsea input y devuelve DateTime "local" (Kind = Unspecified)
        // - Si el input es "date-only" (yyyy-MM-dd o dd/MM/yyyy) devuelve la fecha (sin hora).
        // - Si incluye hora, toma la hora tal cual (asumiendo hora local si no viene zona).
        // El resultado se devolverá con DateTimeKind.Unspecified para que EF guarde exactamente la misma hora en
        // columnas "timestamp without time zone".
        private bool TryParseToLocalDateTime(string? input, out DateTime local)
        {
            local = default;
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.Trim();

            // Date-only ISO
            if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtIso))
            {
                local = DateTime.SpecifyKind(dtIso.Date, DateTimeKind.Unspecified);
                return true;
            }

            // Date-only es (dd/MM/yyyy)
            if (DateTime.TryParseExact(input, "dd/MM/yyyy", new CultureInfo("es-ES"), DateTimeStyles.None, out var dtEs))
            {
                local = DateTime.SpecifyKind(dtEs.Date, DateTimeKind.Unspecified);
                return true;
            }

            // DateTime ISO with Z or offset -> parse to DateTimeOffset and convert to local wall-clock time
            if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dto))
            {
                // Convert to local wall time (we want the user's local clock)
                var localDt = dto.ToLocalTime().DateTime;
                local = DateTime.SpecifyKind(localDt, DateTimeKind.Unspecified);
                return true;
            }

            // DateTime with time but no zone: parse assuming local
            if (DateTime.TryParse(input, new CultureInfo("es-ES"), DateTimeStyles.AssumeLocal, out var dtLocal))
            {
                local = DateTime.SpecifyKind(dtLocal, DateTimeKind.Unspecified);
                return true;
            }

            // Fallback invariant
            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dtLocal))
            {
                local = DateTime.SpecifyKind(dtLocal, DateTimeKind.Unspecified);
                return true;
            }

            return false;
        }

        // GET api/Ausencias/mias
        [HttpGet("mias")]
        public async Task<ActionResult<IEnumerable<AusenciaDto>>> GetMias()
        {
            var uid = CurrentUserId;
            if (uid == 0) return Unauthorized();

            var list = await _db.Ausencias
                .AsNoTracking()
                .Where(a => a.EmpleadoId == uid)
                .Include(a => a.Empleado)
                .OrderByDescending(a => a.FechaInicio)
                .ToListAsync();

            return Ok(list.Select(Map));
        }

        // GET api/Ausencias/usuario/{userId}
        [HttpGet("usuario/{userId:int}")]
        public async Task<ActionResult<IEnumerable<AusenciaDto>>> GetByUserId(int userId)
        {
            var uid = CurrentUserId;
            if (!IsAdmin && uid == 0) return Unauthorized();
            if (!IsAdmin && userId != uid) return Forbid();

            var list = await _db.Ausencias
                .AsNoTracking()
                .Where(a => a.EmpleadoId == userId)
                .Include(a => a.Empleado)
                .OrderByDescending(a => a.FechaInicio)
                .ToListAsync();

            return Ok(list.Select(Map));
        }

        // GET api/Ausencias/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AusenciaDto>> GetById(int id)
        {
            var uid = CurrentUserId;
            if (uid == 0) return Unauthorized();

            var a = await _db.Ausencias
                .Include(x => x.Empleado)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a is null) return NotFound();
            if (a.EmpleadoId != uid && !IsAdmin) return Forbid();

            return Ok(Map(a));
        }

        // POST api/Ausencias
        [HttpPost]
        public async Task<ActionResult<AusenciaDto>> Create([FromBody] AusenciaCreateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var uid = CurrentUserId;
            if (uid == 0) return Unauthorized();

            var empleado = await _db.Empleados.FindAsync(uid);
            if (empleado is null) return Unauthorized();

            if (!TryParseToLocalDateTime(dto.FechaInicio, out var fechaInicioLocal))
                return BadRequest(new { error = "Formato FechaInicio inválido. Use ISO (yyyy-MM-dd) o dd/MM/yyyy." });

            if (!TryParseToLocalDateTime(dto.FechaFin, out var fechaFinLocal))
                return BadRequest(new { error = "Formato FechaFin inválido. Use ISO (yyyy-MM-dd) o dd/MM/yyyy." });

            // Guardar exactamente horas locales: inicio 00:00, fin 23:59:59 (Kind = Unspecified)
            var inicioLocal = new DateTime(fechaInicioLocal.Year, fechaInicioLocal.Month, fechaInicioLocal.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var finLocal = new DateTime(fechaFinLocal.Year, fechaFinLocal.Month, fechaFinLocal.Day, 23, 59, 59, DateTimeKind.Unspecified);

            if (finLocal < inicioLocal)
                return BadRequest(new { error = "FechaFin debe ser posterior o igual a FechaInicio." });

            var a = new Ausencia
            {
                EmpleadoId = uid,
                FechaInicio = inicioLocal,
                FechaFin = finLocal,
                Motivo = dto.Motivo.Trim(),
                Aceptada = null
            };

            _db.Ausencias.Add(a);
            await _db.SaveChangesAsync();

            await _db.Entry(a).Reference(x => x.Empleado).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = a.Id }, Map(a));
        }

        // PUT api/Ausencias/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] AusenciaUpdateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var uid = CurrentUserId;
            if (uid == 0) return Unauthorized();

            var a = await _db.Ausencias.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null) return NotFound();
            if (a.EmpleadoId != uid && !IsAdmin) return Forbid();

            if (a.Aceptada.HasValue && !IsAdmin) return Forbid();

            if (dto.FechaInicio != null)
            {
                if (!TryParseToLocalDateTime(dto.FechaInicio, out var fiLocal))
                    return BadRequest(new { error = "Formato FechaInicio inválido." });

                a.FechaInicio = new DateTime(fiLocal.Year, fiLocal.Month, fiLocal.Day, 0, 0, 0, DateTimeKind.Unspecified);
            }

            if (dto.FechaFin != null)
            {
                if (!TryParseToLocalDateTime(dto.FechaFin, out var ffLocal))
                    return BadRequest(new { error = "Formato FechaFin inválido." });

                a.FechaFin = new DateTime(ffLocal.Year, ffLocal.Month, ffLocal.Day, 23, 59, 59, DateTimeKind.Unspecified);
            }

            if (dto.Motivo != null) a.Motivo = dto.Motivo.Trim();

            if (a.FechaFin < a.FechaInicio)
                return BadRequest(new { error = "FechaFin debe ser posterior o igual a FechaInicio." });

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PUT api/Ausencias/{id}/aprobar
        [HttpPut("{id:int}/aprobar")]
        public async Task<IActionResult> Aprobar(int id, [FromBody] AprobarRequest body)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (!IsAdmin) return Forbid();

            var a = await _db.Ausencias.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null) return NotFound();

            a.Aceptada = body.Aceptada;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/Ausencias/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var uid = CurrentUserId;
            if (uid == 0) return Unauthorized();

            var a = await _db.Ausencias.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null) return NotFound();
            if (a.EmpleadoId != uid && !IsAdmin) return Forbid();

            _db.Ausencias.Remove(a);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}