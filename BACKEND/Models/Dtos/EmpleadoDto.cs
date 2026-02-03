using System.ComponentModel.DataAnnotations;

namespace FichajeApp.DTOs
{
    public class EmpleadoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = default!;
        public string Apellidos { get; set; } = default!;
        public string DNI { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Telefono { get; set; }
        public string Rol { get; set; } = default!;
    }

    public class CreateEmpleadoDto
    {
        [Required, MaxLength(100)] public string Nombre { get; set; } = default!;
        [Required, MaxLength(150)] public string Apellidos { get; set; } = default!;
        [Required, MaxLength(15)] public string DNI { get; set; } = default!;
        [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = default!;
        [Required, MinLength(6), MaxLength(256)] public string Contrasena { get; set; } = default!;
        [MaxLength(20)] public string? Telefono { get; set; }
        [Required, MaxLength(50)] public string Rol { get; set; } = "User";
    }

    public class UpdateEmpleadoDto
    {
        [MaxLength(100)] public string? Nombre { get; set; }
        [MaxLength(150)] public string? Apellidos { get; set; }
        [MaxLength(15)] public string? DNI { get; set; }
        [EmailAddress, MaxLength(150)] public string? Email { get; set; }
        [MinLength(6), MaxLength(256)] public string? Contrasena { get; set; }
        [MaxLength(20)] public string? Telefono { get; set; }
        [MaxLength(50)] public string? Rol { get; set; }
    }
}
