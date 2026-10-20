using Microsoft.EntityFrameworkCore;
using SistemaInscripcionesAcademicas.Api.Models;

namespace SistemaInscripcionesAcademicas.Api.Data;

public static class CatalogoInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        // Solo inicializa un catálogo vacío; conserva los datos existentes.
        if (await context.Profesores.AnyAsync(cancellationToken) ||
            await context.Materias.AnyAsync(cancellationToken))
            return;

        var catalogo = new[]
        {
            ("Carlos Rodríguez", "Matemáticas", "Física"),
            ("Ana Martínez", "Programación", "Bases de Datos"),
            ("Pedro Gómez", "Inglés", "Comunicación"),
            ("Laura Fernández", "Estadística", "Cálculo"),
            ("Andrés López", "Redes", "Sistemas Operativos")
        };

        foreach (var (nombre, primera, segunda) in catalogo)
        {
            context.Profesores.Add(new Profesor
            {
                Nombre = nombre,
                Materias = new List<Materia>
                {
                    new() { Nombre = primera, Creditos = 3 },
                    new() { Nombre = segunda, Creditos = 3 }
                }
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
