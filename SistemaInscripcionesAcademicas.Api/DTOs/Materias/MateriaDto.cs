namespace SistemaInscripcionesAcademicas.Api.DTOs.Materias;

public class MateriaDto
{
    public int Id { get; set; }
    public int ProfesorId { get; set; }
    public bool Activo { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public int Creditos { get; set; }

    public string Profesor { get; set; } = string.Empty;
}