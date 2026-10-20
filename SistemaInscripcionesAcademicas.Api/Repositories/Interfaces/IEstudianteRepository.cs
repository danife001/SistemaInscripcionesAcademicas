using SistemaInscripcionesAcademicas.Api.DTOs.Comun;
using SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;
using SistemaInscripcionesAcademicas.Api.Models;

namespace SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;

public interface IEstudianteRepository
{
    void Agregar(Estudiante estudiante);
    Task<bool> ExisteActivoAsync(int id);
    Task<Estudiante?> ObtenerActivoPorIdAsync(int id);
    Task<Estudiante?> ObtenerActivoPorUsuarioIdAsync(string usuarioId);
    Task<ResultadoPaginado<EstudianteDto>> ObtenerActivosAsync(
        PaginacionDto paginacion, CancellationToken cancellationToken = default);
    Task<ResultadoPaginado<RegistroEstudianteDto>> ObtenerRegistrosOtrosAsync(
        string usuarioId, PaginacionDto paginacion, CancellationToken cancellationToken = default);
}
