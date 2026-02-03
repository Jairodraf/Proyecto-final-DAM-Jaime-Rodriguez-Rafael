using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FichajeApp.Models
{
    public class Fichaje
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Relación con Empleado
        public int EmpleadoId { get; set; }
        public Empleado Empleado { get; set; } = default!;

        public DateTime FechaHora { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Tipo { get; set; } = "Entrada"; // "Entrada" | "Salida"

        [MaxLength(300)]
        public string? Observacion { get; set; }
    }
}
