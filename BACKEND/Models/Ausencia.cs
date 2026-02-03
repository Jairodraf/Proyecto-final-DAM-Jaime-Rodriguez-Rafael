using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FichajeApp.Models
{
    public class Ausencia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Empleado que solicita la ausencia
        public int EmpleadoId { get; set; }
        public Empleado Empleado { get; set; } = default!;

        // Fecha de inicio y fin (almacenar en UTC)
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        [MaxLength(500)]
        public string Motivo { get; set; } = default!;

        // null = pendiente, true = aceptada, false = rechazada
        public bool? Aceptada { get; set; }
    }
}