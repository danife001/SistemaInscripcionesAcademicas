using SistemaInscripcionesAcademicas.Api.Exceptions;
using SistemaInscripcionesAcademicas.Api.DTOs.Materias;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;

namespace SistemaInscripcionesAcademicas.Api.Services;

public class MateriaService(IMateriaRepository materias, IUnidadTrabajo unidad) : IMateriaService
{
    public Task<List<MateriaDto>> ObtenerDisponiblesAsync() => materias.ObtenerDisponiblesAsync();
    public Task<List<MateriaDto>> ObtenerTodasAsync() => materias.ObtenerTodasAsync();
    public async Task<MateriaDto?> ObtenerPorIdAsync(int id) =>
        await materias.ObtenerPorIdAsync(id) is { } materia ? CrearDto(materia) : null;

    private async Task ValidarAsync(GuardarMateriaDto dto, int excluirId = 0)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Nombre.Trim().Length > 200)
            throw new ReglaNegocioException("El nombre es obligatorio y admite hasta 200 caracteres.", conflicto: true);
        if (dto.Creditos != 3) throw new ReglaNegocioException("Cada materia debe tener exactamente 3 créditos.", conflicto: true);
        if (!await materias.ProfesorActivoAsync(dto.ProfesorId))
            throw new ReglaNegocioException("El profesor no existe o está inactivo.", conflicto: true);
        if (await materias.ContarActivasProfesorAsync(dto.ProfesorId, excluirId) >= 2)
            throw new ReglaNegocioException("El profesor ya tiene dos materias activas.", conflicto: true);
    }

    public async Task<MateriaDto> CrearAsync(GuardarMateriaDto dto)
    {
        await using var transaccion = await unidad.IniciarTransaccionCatalogoAsync();
        await ValidarAsync(dto);
        var materia = new Materia { Nombre = dto.Nombre.Trim(), Creditos = 3, ProfesorId = dto.ProfesorId };
        materias.Agregar(materia);
        await unidad.GuardarCambiosAsync();
        var resultado = CrearDto((await materias.ObtenerPorIdAsync(materia.Id))!);
        await transaccion.ConfirmarAsync();
        return resultado;
    }

    public async Task<MateriaDto?> ActualizarAsync(int id, GuardarMateriaDto dto)
    {
        await using var transaccion = await unidad.IniciarTransaccionCatalogoAsync();
        var materia = await materias.ObtenerPorIdAsync(id);
        if (materia == null) return null;
        if (!materia.Activo) throw new ReglaNegocioException("No se puede modificar una materia desactivada.", conflicto: true);
        await ValidarAsync(dto, id);
        // El historial referencia la materia, no una copia: cambiar nombre o profesor lo alteraría.
        if ((materia.Nombre != dto.Nombre.Trim() || materia.ProfesorId != dto.ProfesorId || materia.Creditos != dto.Creditos)
            && await materias.TieneInscripcionesAsync(id, false))
            throw new ReglaNegocioException("No se puede modificar una materia con historial de inscripciones.", conflicto: true);
        materia.Nombre = dto.Nombre.Trim();
        materia.ProfesorId = dto.ProfesorId;
        await unidad.GuardarCambiosAsync();
        var resultado = CrearDto((await materias.ObtenerPorIdAsync(id))!);
        await transaccion.ConfirmarAsync();
        return resultado;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        await using var transaccion = await unidad.IniciarTransaccionCatalogoAsync();
        var materia = await materias.ObtenerPorIdAsync(id);
        if (materia == null || !materia.Activo) return false;
        if (await materias.TieneInscripcionesAsync(id, true))
            throw new ReglaNegocioException("No se puede desactivar una materia con inscripciones activas.", conflicto: true);
        materia.Activo = false;
        await unidad.GuardarCambiosAsync();
        await transaccion.ConfirmarAsync();
        return true;
    }

    private static MateriaDto CrearDto(Materia materia) => new() {
        Id = materia.Id, Nombre = materia.Nombre, Creditos = materia.Creditos,
        ProfesorId = materia.ProfesorId, Profesor = materia.Profesor.Nombre, Activo = materia.Activo
    };
}
