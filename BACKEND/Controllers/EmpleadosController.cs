using System.Linq;
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
    public class EmpleadosController(ApplicationDbContext db) : ControllerBase
    {
        private (int UserId, bool IsAdmin) Current =>
            (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : 0,
             User.IsInRole("Admin"));

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<EmpleadoDto>>> GetAll()
        {
            var list = await db.Empleados.AsNoTracking()
                .OrderBy(e => e.Apellidos).ThenBy(e => e.Nombre)
                .Select(e => new EmpleadoDto
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    Apellidos = e.Apellidos,
                    DNI = e.DNI,
                    Email = e.Email,
                    Telefono = e.Telefono,
                    Rol = e.Rol.ToString()
                }).ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<EmpleadoDto>> GetById(int id)
        {
            var (uid, isAdmin) = Current;
            if (!isAdmin && uid != id) return Forbid();

            var e = await db.Empleados.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return NotFound();

            return Ok(new EmpleadoDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Apellidos = e.Apellidos,
                DNI = e.DNI,
                Email = e.Email,
                Telefono = e.Telefono,
                Rol = e.Rol.ToString()
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<EmpleadoDto>> Create([FromBody] CreateEmpleadoDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var email = dto.Email.Trim().ToLowerInvariant();
            var dni = dto.DNI.Trim();

            if (await db.Empleados.AnyAsync(x => x.Email == email || x.DNI == dni))
                return Conflict("Ya existe un empleado con ese DNI o Email.");

            // dentro de Create
            var rolOk = Enum.TryParse<Rol>(dto.Rol?.Trim(), ignoreCase: true, out var rolEnum);
            if (!rolOk) return BadRequest("Rol inválido. Use 'Admin' o 'User'.");

            var e = new Empleado
            {
                Nombre = dto.Nombre.Trim(),
                Apellidos = dto.Apellidos.Trim(),
                DNI = dni,
                Email = email,
                Contrasena = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena),
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
                Rol = rolEnum
            };


            db.Empleados.Add(e);
            await db.SaveChangesAsync();

            var res = new EmpleadoDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Apellidos = e.Apellidos,
                DNI = e.DNI,
                Email = e.Email,
                Telefono = e.Telefono,
                Rol = e.Rol.ToString(),
            };

            return CreatedAtAction(nameof(GetById), new { id = e.Id }, res);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEmpleadoDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var e = await db.Empleados.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var email = dto.Email.Trim().ToLowerInvariant();
                if (email != e.Email)
                {
                    if (await db.Empleados.AnyAsync(x => x.Email == email && x.Id != id))
                        return Conflict("Email ya en uso.");
                    e.Email = email;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.DNI))
            {
                var dni = dto.DNI.Trim();
                if (dni != e.DNI)
                {
                    if (await db.Empleados.AnyAsync(x => x.DNI == dni && x.Id != id))
                        return Conflict("DNI ya en uso.");
                    e.DNI = dni;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Nombre)) e.Nombre = dto.Nombre.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Apellidos)) e.Apellidos = dto.Apellidos.Trim();
            if (dto.Telefono != null) e.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Rol))
            {
                if (Enum.TryParse<Rol>(dto.Rol.Trim(), true, out Rol parsedRol))
                {
                    e.Rol = parsedRol;
                }
            }


            if (!string.IsNullOrWhiteSpace(dto.Contrasena)) e.Contrasena = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena);

            if (!string.IsNullOrWhiteSpace(dto.Rol))
            {
                if (Enum.TryParse<Rol>(dto.Rol.Trim(), true, out var rol))
                    e.Rol = rol;
                else
                    return BadRequest("Rol inválido. Use 'Admin' o 'User'.");
            }


            await db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await db.Empleados.FindAsync(id);
            if (e is null) return NotFound();

            db.Empleados.Remove(e);
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
