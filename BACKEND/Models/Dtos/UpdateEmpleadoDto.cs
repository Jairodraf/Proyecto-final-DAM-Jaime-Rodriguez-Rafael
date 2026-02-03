using System.ComponentModel.DataAnnotations;

public class UpdateEmpleadoDto
{
    [MaxLength(100)]
    public string Nombre { get; set; }
    [MaxLength(150)]
    public string Apellidos { get; set; }
    [MaxLength(15)]
    public string DNI { get; set; }
    [EmailAddress, MaxLength(150)]
    public string Email { get; set; }
    [MinLength(6)]
    public string Contrasena { get; set; }
    [MaxLength(20)]
    public string Telefono { get; set; }
    [MaxLength(50)]
    public string Rol { get; set; }
}