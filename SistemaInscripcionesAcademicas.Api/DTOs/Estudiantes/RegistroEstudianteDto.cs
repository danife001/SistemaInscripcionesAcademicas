namespace SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;

public class RegistroEstudianteDto
{
    public string Nombre { get; set; } = string.Empty;

    public List<string> Materias { get; set; } = new();
}