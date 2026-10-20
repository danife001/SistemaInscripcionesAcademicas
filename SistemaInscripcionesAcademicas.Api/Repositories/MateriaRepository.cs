using Microsoft.EntityFrameworkCore;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.DTOs.Materias;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;

namespace SistemaInscripcionesAcademicas.Api.Repositories;

public class MateriaRepository : IMateriaRepository
{
    private readonly ApplicationDbContext _context;

    public MateriaRepository(ApplicationDbContext context) => _context = context;

    public void Agregar(Materia materia) => _context.Materias.Add(materia);
    public Task<Materia?> ObtenerPorIdAsync(int id) =>
        _context.Materias.Include(m => m.Profesor).FirstOrDefaultAsync(m => m.Id == id);
    public Task<bool> ProfesorActivoAsync(int id) => _context.Profesores.AnyAsync(p => p.Id == id && p.Activo);
    public Task<int> ContarActivasProfesorAsync(int profesorId, int excluirId) =>
        _context.Materias.CountAsync(m => m.Activo && m.ProfesorId == profesorId && m.Id != excluirId);
    public Task<bool> TieneInscripcionesAsync(int id, bool soloActivas) =>
        _context.Inscripciones.AnyAsync(i => i.MateriaId == id && (!soloActivas || i.Activa));
    public Task<List<MateriaDto>> ObtenerTodasAsync() =>
        _context.Materias.AsNoTracking().OrderBy(m => m.Nombre).ThenBy(m => m.Id)
            .Select(m => new MateriaDto { Id = m.Id, Nombre = m.Nombre, Creditos = m.Creditos,
                ProfesorId = m.ProfesorId, Profesor = m.Profesor.Nombre, Activo = m.Activo }).ToListAsync();

    public Task<Materia?> ObtenerDisponiblePorIdAsync(int id) =>
        _context.Materias.AsNoTracking().Include(m => m.Profesor)
            .FirstOrDefaultAsync(m => m.Id == id && m.Activo && m.Profesor.Activo);

    public Task<List<MateriaDto>> ObtenerDisponiblesAsync() =>
        _context.Materias.AsNoTracking()
            .Where(m => m.Activo && m.Profesor.Activo)
            .OrderBy(m => m.Nombre).ThenBy(m => m.Id)
            .Select(m => new MateriaDto
            {
                Id = m.Id, Nombre = m.Nombre, Creditos = m.Creditos, Profesor = m.Profesor.Nombre, ProfesorId = m.ProfesorId, Activo = m.Activo
            }).ToListAsync();
}
