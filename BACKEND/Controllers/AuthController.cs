// Controllers/AuthController.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FichajeApp.Data;
using FichajeApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FichajeApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // api/Auth
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _cfg;

        public AuthController(ApplicationDbContext db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;
        }

        public class LoginRequest
        {
            public string Email { get; set; } = default!;
            public string Contrasena { get; set; } = default!;
        }

        public class LoginResponse
        {
            // Forzamos camelCase aunque no tengas la opción global activada
            [JsonPropertyName("token")]
            public string Token { get; set; } = default!;

            [JsonPropertyName("rol")]
            public string Rol { get; set; } = default!; // "Admin" | "User"

            [JsonPropertyName("empleadoId")]
            public int EmpleadoId { get; set; }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest dto)
        {
            var email = (dto.Email ?? "").Trim().ToLowerInvariant();
            var empleado = await _db.Empleados
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Email == email);

            if (empleado is null) return Unauthorized();

            var ok = BCrypt.Net.BCrypt.Verify(dto.Contrasena ?? "", empleado.Contrasena);
            if (!ok) return Unauthorized();

            var token = GenerateJwt(empleado);

            return Ok(new LoginResponse
            {
                Token = token,
                Rol = empleado.Rol.ToString(),   // ⬅️ enum -> string
                EmpleadoId = empleado.Id
            });
        }

        // --- helpers ---
        private string GenerateJwt(Empleado empleado)
        {
            var key = _cfg["Jwt:Key"] ?? throw new Exception("Falta Jwt:Key en configuración.");
            var issuer = _cfg["Jwt:Issuer"];
            var audience = _cfg["Jwt:Audience"];
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, empleado.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, empleado.Id.ToString()),
                new Claim(ClaimTypes.Email, empleado.Email),
                // Muy importante: claim de rol como TEXTO
                new Claim(ClaimTypes.Role, empleado.Rol.ToString())
            };

            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
