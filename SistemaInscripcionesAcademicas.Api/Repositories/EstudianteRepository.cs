using Microsoft.EntityFrameworkCore;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.DTOs.Comun;
using SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;
using SistemaInscripcionesAcademicas.Api.Extensions;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;

namespace SistemaInscripcionesAcademicas.Api.Repositories;

public class EstudianteRepository : IEstudianteRepository
{
    private readonly ApplicationDbContext _context;

    public EstudianteRepository(ApplicationDbContext context) => _context = context;

    public void Agregar(Estudiante estudiante) => _context.Estudiantes.Add(estudiante);

    public Task<bool> ExisteActivoAsync(int id) =>
        _context.Estudiantes.AnyAsync(e => e.Id == id && e.Activo);

    public Task<Estudiante?> ObtenerActivoPorIdAsync(int id) =>
        _context.Estudiantes.Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.Id == id && e.Activo);

    public Task<Estudiante?> ObtenerActivoPorUsuarioIdAsync(string usuarioId) =>
        _context.Estudiantes.Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.UsuarioId == usuarioId && e.Activo);

    public Task<ResultadoPaginado<EstudianteDto>> ObtenerActivosAsync(
        PaginacionDto paginacion, CancellationToken cancellationToken = default) =>
        _context.Estudiantes.AsNoTracking()
            .Where(e => e.Activo)
            .OrderBy(e => e.Usuario.Nombre).ThenBy(e => e.Id)
            .Select(e => new EstudianteDto
            {
                Id = e.Id,
                Nombre = e.Usuario.Nombre,
                Correo = e.Usuario.Email ?? string.Empty,
                Activo = e.Activo
            })
            .PaginarAsync(paginacion, cancellationToken);

    public Task<ResultadoPaginado<RegistroEstudianteDto>> ObtenerRegistrosOtrosAsync(
        string usuarioId, PaginacionDto paginacion, CancellationToken cancellationToken = default) =>
        _context.Estudiantes.AsNoTracking()
            .Where(e => e.Activo && e.UsuarioId != usuarioId)
            .OrderBy(e => e.Usuario.Nombre).ThenBy(e => e.Id)
            .Select(e => new RegistroEstudianteDto
            {
                Nombre = e.Usuario.Nombre,
                Materias = e.Inscripciones.Where(i => i.Activa)
                    .OrderBy(i => i.Materia.Nombre).ThenBy(i => i.MateriaId)
                    .Select(i => i.Materia.Nombre).ToList()
            })
            .PaginarAsync(paginacion, cancellationToken);
}
