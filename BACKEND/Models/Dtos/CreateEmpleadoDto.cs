using System.ComponentModel.DataAnnotations;

public class CreateEmpleadoDto
{
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = default!;

    [Required]
    [MaxLength(150)]
    public string Apellidos { get; set; } = default!;

    [Required]
    [MaxLength(15)]
    public string DNI { get; set; } = default!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Contrasena { get; set; } = default!;

    [MaxLength(20)]
    public string Telefono { get; set; }

    [Required]
    public string Rol { get; set; } = "Empleado";
}
