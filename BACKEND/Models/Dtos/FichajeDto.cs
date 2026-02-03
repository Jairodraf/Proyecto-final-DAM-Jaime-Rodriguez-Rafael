using System;
using System.ComponentModel.DataAnnotations;

namespace FichajeApp.DTOs
{
    public class FichajeDto
    {
        public int Id { get; set; }
        public string? Tipo { get; set; }  // "Entrada" | "Salida"
        public DateTime FechaHora { get; set; }
        public string? Observacion { get; set; }
        public int EmpleadoId { get; set; }
        public string? EmpleadoNombre { get; set; } // Nombre completo del empleado
    }

    public class FichajeCreateDto
    {
        // Opcional: si no se envía, el controller pondrá DateTime.UtcNow
        public DateTime? FechaHora { get; set; }

        [Required]
        [RegularExpression("^(Entrada|Salida)$", ErrorMessage = "Tipo debe ser Entrada o Salida.")]
        public string Tipo { get; set; } = default!;

        [MaxLength(300)]
        public string? Observacion { get; set; }
    }

    public class FichajeUpdateDto
    {
        [RegularExpression("^(Entrada|Salida)$", ErrorMessage = "Tipo debe ser Entrada o Salida.")]
        [MaxLength(20)]
        public string? Tipo { get; set; }

        public DateTime? FechaHora { get; set; }

        [MaxLength(300)]
        public string? Observacion { get; set; }
    }

    public class ObservacionRequest
    {
        [MaxLength(300)]
        public string? Observacion { get; set; }
    }
}
