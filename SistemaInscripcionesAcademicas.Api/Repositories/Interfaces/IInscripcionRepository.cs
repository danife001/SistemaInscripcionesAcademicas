using SistemaInscripcionesAcademicas.Api.DTOs.Inscripciones;
using SistemaInscripcionesAcademicas.Api.Models;

namespace SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;

public interface IInscripcionRepository
{
    void Agregar(Inscripcion inscripcion);
    Task<int> ContarActivasAsync(int estudianteId);
    Task<bool> ExisteActivaAsync(int estudianteId, int materiaId);
    Task<bool> ExisteProfesorActivoAsync(int estudianteId, int profesorId);
    Task<Inscripcion?> ObtenerActivaPropiaAsync(int estudianteId, int inscripcionId);
    Task<List<InscripcionDto>> ObtenerActivasAsync(int estudianteId);
    Task<List<CompaDto>> ObtenerCompanerosAsync(int estudianteId, int materiaId);
}
