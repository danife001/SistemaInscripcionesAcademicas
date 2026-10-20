using SistemaInscripcionesAcademicas.Api.DTOs.Materias;
using SistemaInscripcionesAcademicas.Api.Models;

namespace SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;

public interface IMateriaRepository
{
    Task<List<ProfesorDisponibleDto>> ObtenerProfesoresActivosAsync();
    Task<List<MateriaDto>> ObtenerTodasAsync();
    Task<Materia?> ObtenerPorIdAsync(int id);
    Task<bool> ProfesorActivoAsync(int id);
    Task<int> ContarActivasProfesorAsync(int profesorId, int excluirId);
    Task<bool> TieneInscripcionesAsync(int id, bool soloActivas);
    void Agregar(Materia materia);
    Task<Materia?> ObtenerDisponiblePorIdAsync(int id);
    Task<List<MateriaDto>> ObtenerDisponiblesAsync();
}
