
using System.ComponentModel.DataAnnotations;

namespace SistemaInscripcionesAcademicas.Api.DTOs.Inscripciones;

public class CrearInscripcionDto
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Debe seleccionar una materia válida.")]
    public int MateriaId { get; set; }
}
