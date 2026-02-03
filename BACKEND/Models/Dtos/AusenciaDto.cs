using System;
using System.ComponentModel.DataAnnotations;

namespace FichajeApp.DTOs
{
    public class AusenciaDto
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public string? EmpleadoNombre { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Motivo { get; set; } = default!;
        public bool? Aceptada { get; set; }
    }

    public class AusenciaCreateDto
    {
        [Required]
        public string FechaInicio { get; set; } = default!; // aceptar texto desde el front

        [Required]
        public string FechaFin { get; set; } = default!; // aceptar texto desde el front

        [Required]
        [MaxLength(500)]
        public string Motivo { get; set; } = default!;
    }

    public class AusenciaUpdateDto
    {
        public string? FechaInicio { get; set; } // opcional, texto
        public string? FechaFin { get; set; }    // opcional, texto

        [MaxLength(500)]
        public string? Motivo { get; set; }
    }

    public class AprobarRequest
    {
        public bool Aceptada { get; set; }
    }
}