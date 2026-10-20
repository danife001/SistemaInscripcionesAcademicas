
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaInscripcionesAcademicas.Api.Models;

namespace SistemaInscripcionesAcademicas.Api.Data;

public class ApplicationDbContext : IdentityDbContext<Usuario>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Profesor> Profesores => Set<Profesor>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<Inscripcion> Inscripciones => Set<Inscripcion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Usuario - Estudiante: uno a uno
        modelBuilder.Entity<Estudiante>()
            .HasOne(e => e.Usuario)
            .WithOne(u => u.Estudiante)
            .HasForeignKey<Estudiante>(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Profesor - Materia: uno a muchos
        modelBuilder.Entity<Materia>()
            .HasOne(m => m.Profesor)
            .WithMany(p => p.Materias)
            .HasForeignKey(m => m.ProfesorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Estudiante - Inscripcion: uno a muchos
        modelBuilder.Entity<Inscripcion>()
            .HasOne(i => i.Estudiante)
            .WithMany(e => e.Inscripciones)
            .HasForeignKey(i => i.EstudianteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Materia - Inscripcion: uno a muchos
        modelBuilder.Entity<Inscripcion>()
            .HasOne(i => i.Materia)
            .WithMany(m => m.Inscripciones)
            .HasForeignKey(i => i.MateriaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Evita inscripciones activas duplicadas
        modelBuilder.Entity<Inscripcion>()
            .HasIndex(i => new { i.EstudianteId, i.MateriaId })
            .IsUnique()
            .HasFilter("[Activa] = 1");
    }
}
