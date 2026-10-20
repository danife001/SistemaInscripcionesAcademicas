using SistemaInscripcionesAcademicas.Api.Exceptions;
using SistemaInscripcionesAcademicas.Api.DTOs.Inscripciones;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;

namespace SistemaInscripcionesAcademicas.Api.Services;

public class InscripcionService : IInscripcionService
{
    private readonly IEstudianteRepository _estudiantes;
    private readonly IMateriaRepository _materias;
    private readonly IInscripcionRepository _inscripciones;
    private readonly IUnidadTrabajo _unidadTrabajo;

    public InscripcionService(IEstudianteRepository estudiantes, IMateriaRepository materias,
        IInscripcionRepository inscripciones, IUnidadTrabajo unidadTrabajo)
    {
        _estudiantes = estudiantes;
        _materias = materias;
        _inscripciones = inscripciones;
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<InscripcionDto> InscribirAsync(int estudianteId, CrearInscripcionDto dto)
    {
        await using var transaccion =
            await _unidadTrabajo.IniciarTransaccionInscripcionesAsync(estudianteId);

        if (!await _estudiantes.ExisteActivoAsync(estudianteId))
            throw new ReglaNegocioException("El estudiante no existe o está inactivo.");

        var materia = await _materias.ObtenerDisponiblePorIdAsync(dto.MateriaId);
        if (materia == null)
            throw new ReglaNegocioException("La materia no está disponible.");

        if (await _inscripciones.ContarActivasAsync(estudianteId) >= 3)
            throw new ReglaNegocioException("No puede inscribir más de tres materias.");

        if (await _inscripciones.ExisteActivaAsync(estudianteId, dto.MateriaId))
            throw new ReglaNegocioException("Ya está inscrito en esta materia.");

        if (await _inscripciones.ExisteProfesorActivoAsync(estudianteId, materia.ProfesorId))
            throw new ReglaNegocioException("No puede inscribir dos materias del mismo profesor.");

        var inscripcion = new Inscripcion
        {
            EstudianteId = estudianteId, MateriaId = materia.Id,
            FechaInscripcion = DateTime.UtcNow, Activa = true
        };
        _inscripciones.Agregar(inscripcion);
        await _unidadTrabajo.GuardarCambiosAsync();
        await transaccion.ConfirmarAsync();

        return new InscripcionDto
        {
            Id = inscripcion.Id, MateriaId = materia.Id, Materia = materia.Nombre,
            Profesor = materia.Profesor.Nombre, Creditos = materia.Creditos,
            FechaInscripcion = inscripcion.FechaInscripcion, Activa = inscripcion.Activa
        };
    }

    public Task<List<InscripcionDto>> ObtenerMisInscripcionesAsync(int estudianteId) =>
        _inscripciones.ObtenerActivasAsync(estudianteId);

    public async Task<bool> CancelarAsync(int estudianteId, int inscripcionId)
    {
        await using var transaccion =
            await _unidadTrabajo.IniciarTransaccionInscripcionesAsync(estudianteId);

        var inscripcion = await _inscripciones.ObtenerActivaPropiaAsync(estudianteId, inscripcionId);
        if (inscripcion == null) return false;
        inscripcion.Activa = false;
        await _unidadTrabajo.GuardarCambiosAsync();
        await transaccion.ConfirmarAsync();
        return true;
    }

    public async Task<List<CompaDto>?> ObtenerCompasAsync(int estudianteId, int materiaId)
    {
        if (!await _inscripciones.ExisteActivaAsync(estudianteId, materiaId))
            return null;
        return await _inscripciones.ObtenerCompanerosAsync(estudianteId, materiaId);
    }
}
