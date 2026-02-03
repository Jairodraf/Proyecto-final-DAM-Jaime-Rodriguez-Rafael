using System.ComponentModel.DataAnnotations;

namespace FichajeApp.Models
{
    public class Empleado
    {
        [Key] public int Id { get; set; }

        [Required, MaxLength(100)] public string Nombre { get; set; } = default!;
        [Required, MaxLength(150)] public string Apellidos { get; set; } = default!;
        [Required, MaxLength(15)] public string DNI { get; set; } = default!;
        [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = default!;
        [Required, MaxLength(256)] public string Contrasena { get; set; } = default!;
        [MaxLength(20)] public string? Telefono { get; set; }

        [Required] public Rol Rol { get; set; } = Rol.User;
    }
}
