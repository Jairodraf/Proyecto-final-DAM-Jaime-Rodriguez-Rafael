using System.ComponentModel.DataAnnotations;

namespace FichajeApp.Models.Dtos
{



    public class UpdateFichajeDto
    {
        // Todos opcionales para permitir updates parciales
        [MaxLength(100)]
        public string Name { get; set; }

        public DateTime? FechaHora { get; set; }

        // Usa null = no tocar; "" = borrar; "texto" = establecer
        [MaxLength(250)]
        public string Incidencia { get; set; }
    }
}