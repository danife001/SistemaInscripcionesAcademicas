using SistemaInscripcionesAcademicas.Api.DTOs.Inscripciones;

namespace SistemaInscripcionesAcademicas.Api.Services.Interfaces;

public interface IInscripcionService
{
    Task<InscripcionDto> InscribirAsync(
        int estudianteId,
        CrearInscripcionDto dto);

    Task<List<InscripcionDto>> ObtenerMisInscripcionesAsync(
        int estudianteId);

    Task<bool> CancelarAsync(int estudianteId, int inscripcionId);

    Task<List<CompaDto>?> ObtenerCompasAsync(
    int estudianteId,
    int materiaId);
}