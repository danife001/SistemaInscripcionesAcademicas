namespace SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;

public class EstudianteDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public bool Activo { get; set; }
}