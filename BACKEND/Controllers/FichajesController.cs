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
    [Route("api/[controller]")] // api/Fichajes
    [Authorize]
    public class FichajesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public FichajesController(ApplicationDbContext db) => _db = db;

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : 0;

        private bool IsAdmin =>
            User.IsInRole("Admin")
            || User.HasClaim(c => c.Type == "role" && c.Value == "Admin")
            || User.HasClaim(c => c.Type == "roles" && c.Value == "Admin");

        private static FichajeDto Map(Fichaje f) => new FichajeDto
        {
            Id = f.Id,
            FechaHora = f.FechaHora,
            Tipo = f.Tipo,
            Observacion = f.Observacion,
            EmpleadoId = f.EmpleadoId,
            EmpleadoNombre = f.Empleado != null ? $"{f.Empleado.Nombre} {f.Empleado.Apellidos}" : null
        };

        // GET api/Fichajes
        // Por seguridad: devolver solo los del usuario actual por defecto.
        // Si se necesita obtener "todos" hay que pasar ?all=true y ser Admin.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FichajeDto>>> GetAll([FromQuery] int? limit = null, [FromQuery] bool all = false)
        {
            var uid = CurrentUserId;
            var isAdmin = IsAdmin;

            if (!isAdmin && uid == 0) return Unauthorized();
            if (all && !isAdmin) return Forbid();

            IQueryable<Fichaje> query = _db.Fichajes
                .AsNoTracking()
                .Include(f => f.Empleado)
                .OrderByDescending(f => f.FechaHora);

            if (!all)
            {
                query = query.Where(f => f.EmpleadoId == uid);
            }

            if (limit is > 0)
                query = query.Take(limit.Value);

            var list = await query.ToListAsync();
            list.Reverse();
            return Ok(list.Select(Map));
        }

        // Nuevo: GET api/Fichajes/usuario/{userId}
        // Solo Admin puede solicitar fichajes de otros usuarios.
        [HttpGet("usuario/{userId:int}")]
        public async Task<ActionResult<IEnumerable<FichajeDto>>> GetByUserId(int userId, [FromQuery] int? limit = null)
        {
            var uid = CurrentUserId;
            var isAdmin = IsAdmin;

            if (!isAdmin && uid == 0) return Unauthorized();

            // Si no es admin, solo puede pedir sus propios fichajes
            if (!isAdmin && userId != uid) return Forbid();

            IQueryable<Fichaje> query = _db.Fichajes
                .AsNoTracking()
                .Where(f => f.EmpleadoId == userId)
                .Include(f => f.Empleado)
                .OrderByDescending(f => f.FechaHora);

            if (limit is > 0)
                query = query.Take(limit.Value);

            var list = await query.ToListAsync();
            list.Reverse();
            return Ok(list.Select(Map));
        }

        // GET api/Fichajes/mios?limit=50
        [HttpGet("mios")]
        public async Task<ActionResult<IEnumerable<FichajeDto>>> GetMios([FromQuery] int? limit = null)
        {
            var uid = CurrentUserId;
            if (uid == 0) return Unauthorized();

            IQueryable<Fichaje> query = _db.Fichajes
                .AsNoTracking()
                .Where(f => f.EmpleadoId == uid)
                .Include(f => f.Empleado)
                .OrderByDescending(f => f.FechaHora);

            if (limit is > 0)
                query = query.Take(limit.Value);

            var list = await query.ToListAsync();
            list.Reverse();
            return Ok(list.Select(Map));
        }

        // GET api/Fichajes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<FichajeDto>> GetById(int id)
        {
            var uid = CurrentUserId;
            if (uid == 0) return Unauthorized();

            var f = await _db.Fichajes
                .Include(x => x.Empleado)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (f is null) return NotFound();
            if (f.EmpleadoId != uid && !IsAdmin) return Forbid();

            return Ok(Map(f));
        }

        // POST api/Fichajes
        [HttpPost]
        public async Task<ActionResult<FichajeDto>> Create([FromBody] FichajeCreateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var uid = CurrentUserId;
            if (uid == 0) return Unauthorized();

            // Asegurar que el empleado existe
            var empleado = await _db.Empleados.FindAsync(uid);
            if (empleado is null) return Unauthorized();

            // Normalizar y validar Tipo
            var tipo = dto.Tipo?.Trim();
            if (string.IsNullOrEmpty(tipo)) tipo = "Entrada";
            if (tipo != "Entrada" && tipo != "Salida") return BadRequest(new { error = "Tipo debe ser 'Entrada' o 'Salida'." });

            var f = new Fichaje
            {
                EmpleadoId = uid,
                FechaHora = dto.FechaHora.HasValue
                    ? DateTime.SpecifyKind(dto.FechaHora.Value, DateTimeKind.Utc)
                    : DateTime.UtcNow,
                Tipo = tipo,
                Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion!.Trim()
            };

            _db.Fichajes.Add(f);
            await _db.SaveChangesAsync();

            // Cargar navegación para mostrar nombre en el DTO
            await _db.Entry(f).Reference(x => x.Empleado).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = f.Id }, Map(f));
        }

        // PUT api/Fichajes/{id}/observacion
        [HttpPut("{id}/observacion")]
        public async Task<IActionResult> UpdateObservacion(int id, [FromBody] ObservacionRequest body)
        {
            var uid = CurrentUserId;
            if (uid == 0) return Unauthorized();

            var f = await _db.Fichajes.FirstOrDefaultAsync(x => x.Id == id);
            if (f is null) return NotFound();
            if (f.EmpleadoId != uid) return Forbid();

            f.Observacion = string.IsNullOrWhiteSpace(body.Observacion) ? null : body.Observacion!.Trim();
            await _db.SaveChangesAsync();
            return NoContent();
        }

        public class ObservacionRequest
        {
            public string? Observacion { get; set; }
        }
    }
}
