using SistemaInscripcionesAcademicas.Api.DTOs.Materias;

namespace SistemaInscripcionesAcademicas.Api.Services.Interfaces;

public interface IMateriaService
{
    Task<List<MateriaDto>> ObtenerTodasAsync();
    Task<MateriaDto?> ObtenerPorIdAsync(int id);
    Task<MateriaDto> CrearAsync(GuardarMateriaDto dto);
    Task<MateriaDto?> ActualizarAsync(int id, GuardarMateriaDto dto);
    Task<bool> DesactivarAsync(int id);
    Task<List<MateriaDto>> ObtenerDisponiblesAsync();
}
