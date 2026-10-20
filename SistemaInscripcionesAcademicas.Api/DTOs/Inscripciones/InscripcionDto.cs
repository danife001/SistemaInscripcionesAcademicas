
namespace SistemaInscripcionesAcademicas.Api.DTOs.Inscripciones;

public class InscripcionDto
{
    public int Id { get; set; }

    public int MateriaId { get; set; }

    public string Materia { get; set; } = string.Empty;

    public string Profesor { get; set; } = string.Empty;

    public int Creditos { get; set; }

    public DateTime FechaInscripcion { get; set; }

    public bool Activa { get; set; }
}
