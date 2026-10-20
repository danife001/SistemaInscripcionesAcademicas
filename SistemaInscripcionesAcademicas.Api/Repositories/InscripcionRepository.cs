using Microsoft.EntityFrameworkCore;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.DTOs.Inscripciones;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;

namespace SistemaInscripcionesAcademicas.Api.Repositories;

public class InscripcionRepository : IInscripcionRepository
{
    private readonly ApplicationDbContext _context;

    public InscripcionRepository(ApplicationDbContext context) => _context = context;

    public void Agregar(Inscripcion inscripcion) => _context.Inscripciones.Add(inscripcion);

    public Task<int> ContarActivasAsync(int estudianteId) =>
        _context.Inscripciones.CountAsync(i => i.EstudianteId == estudianteId && i.Activa);

    public Task<bool> ExisteActivaAsync(int estudianteId, int materiaId) =>
        _context.Inscripciones.AnyAsync(i =>
            i.EstudianteId == estudianteId && i.MateriaId == materiaId && i.Activa);

    public Task<bool> ExisteProfesorActivoAsync(int estudianteId, int profesorId) =>
        _context.Inscripciones.AnyAsync(i =>
            i.EstudianteId == estudianteId && i.Activa && i.Materia.ProfesorId == profesorId);

    public Task<Inscripcion?> ObtenerActivaPropiaAsync(int estudianteId, int inscripcionId) =>
        _context.Inscripciones.FirstOrDefaultAsync(i =>
            i.Id == inscripcionId && i.EstudianteId == estudianteId && i.Activa);

    public Task<List<InscripcionDto>> ObtenerActivasAsync(int estudianteId) =>
        _context.Inscripciones.AsNoTracking()
            .Where(i => i.EstudianteId == estudianteId && i.Activa)
            .OrderBy(i => i.FechaInscripcion).ThenBy(i => i.Id)
            .Select(i => new InscripcionDto
            {
                Id = i.Id,
                MateriaId = i.MateriaId,
                Materia = i.Materia.Nombre,
                Profesor = i.Materia.Profesor.Nombre,
                Creditos = i.Materia.Creditos,
                FechaInscripcion = i.FechaInscripcion,
                Activa = i.Activa
            }).ToListAsync();

    public Task<List<CompaDto>> ObtenerCompanerosAsync(int estudianteId, int materiaId) =>
        _context.Inscripciones.AsNoTracking()
            .Where(i => i.MateriaId == materiaId && i.EstudianteId != estudianteId &&
                i.Activa && i.Estudiante.Activo)
            .OrderBy(i => i.Estudiante.Usuario.Nombre).ThenBy(i => i.EstudianteId)
            .Select(i => new CompaDto { Nombre = i.Estudiante.Usuario.Nombre })
            .ToListAsync();
}
