using System.ComponentModel.DataAnnotations;

namespace FichajeApp.DTOs
{
    public class LoginDto
    {
        [Required, EmailAddress] public string Email { get; set; } = default!;
        [Required] public string Contrasena { get; set; } = default!;
    }

    public class TokenDto
    {
        public string Token { get; set; } = default!;
        public string Rol { get; set; } = default!;
        public int EmpleadoId { get; set; }
    }
}
