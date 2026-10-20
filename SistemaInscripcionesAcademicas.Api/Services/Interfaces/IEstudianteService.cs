using SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;

using SistemaInscripcionesAcademicas.Api.DTOs.Comun;

namespace SistemaInscripcionesAcademicas.Api.Services.Interfaces;

public interface IEstudianteService
{
    Task<EstudianteDto> RegistrarAsync(
        CrearEstudianteDto dto);

    Task<ResultadoPaginado<EstudianteDto>> ObtenerTodosAsync(
        PaginacionDto paginacion, CancellationToken cancellationToken = default);

    Task<EstudianteDto?> ObtenerPorIdAsync(int id);

    Task<EstudianteDto?> ObtenerPorUsuarioIdAsync(string usuarioId);

    Task<ResultadoPaginado<RegistroEstudianteDto>> ObtenerRegistrosOtrosAsync(
        string usuarioId, PaginacionDto paginacion, CancellationToken cancellationToken = default);

    Task<EstudianteDto?> ActualizarAsync(int id, ActualizarEstudianteDto dto);

    Task<bool> DesactivarAsync(int id);
}
