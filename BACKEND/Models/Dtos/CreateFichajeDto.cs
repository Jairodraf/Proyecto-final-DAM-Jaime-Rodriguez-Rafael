// Este archivo se mantiene solo para evitar confusiones con el antiguo tipo
// y para compatibilidad si hay código que usa FichajeApp.Models.Dtos.CreateFichajeDto.
// Recomendado: eliminar este archivo si no hay referencias antiguas.
using FichajeApp.DTOs;

namespace FichajeApp.Models.Dtos
{
    // Clase ligera que reusa el DTO correcto para evitar errores de binding/compilación
    public class CreateFichajeDto : FichajeCreateDto { }
}