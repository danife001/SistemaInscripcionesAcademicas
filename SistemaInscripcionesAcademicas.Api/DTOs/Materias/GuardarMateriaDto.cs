using System.ComponentModel.DataAnnotations;

namespace SistemaInscripcionesAcademicas.Api.DTOs.Materias;

public class GuardarMateriaDto
{
    [Required, StringLength(200)]
    public string Nombre { get; set; } = string.Empty;
    [Range(3, 3, ErrorMessage = "Cada materia debe tener exactamente 3 créditos.")]
    public int Creditos { get; set; } = 3;
    [Range(1, int.MaxValue)]
    public int ProfesorId { get; set; }
}
