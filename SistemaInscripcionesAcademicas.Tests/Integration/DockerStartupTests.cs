using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.Models;

namespace SistemaInscripcionesAcademicas.Tests.Integration;

public class DockerStartupTests
{
    [Fact]
    public async Task CatalogoVacio_DebeCrearDiezMateriasYNoDuplicarlasAlReiniciar()
    {
        await using var app = new AplicacionPruebas();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await CatalogoInitializer.InitializeAsync(db);
        var idsOriginales = await db.Materias.OrderBy(m => m.Id).Select(m => m.Id).ToListAsync();
        await CatalogoInitializer.InitializeAsync(db);

        Assert.Equal(5, await db.Profesores.CountAsync());
        Assert.Equal(10, await db.Materias.CountAsync());
        Assert.Equal(idsOriginales, await db.Materias.OrderBy(m => m.Id).Select(m => m.Id).ToListAsync());
        Assert.All(await db.Materias.ToListAsync(), m =>
        {
            Assert.Equal(3, m.Creditos);
            Assert.True(m.Activo);
        });
        var profesores = await db.Profesores.Include(p => p.Materias).ToListAsync();
        Assert.All(profesores, p =>
        {
            Assert.True(p.Activo);
            Assert.Equal(2, p.Materias.Count);
        });
    }

    [Fact]
    public async Task CatalogoExistente_NoDebeReemplazarDatos()
    {
        await using var app = new AplicacionPruebas();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Profesores.Add(new Profesor { Nombre = "Profesor existente" });
        await db.SaveChangesAsync();

        await CatalogoInitializer.InitializeAsync(db);

        Assert.Equal("Profesor existente", (await db.Profesores.SingleAsync()).Nombre);
        Assert.Empty(await db.Materias.ToListAsync());
    }

    [Fact]
    public async Task Health_DebeComprobarConexionSinExponerCredenciales()
    {
        await using var app = new AplicacionPruebas();
        using var cliente = app.CreateClient();
        var respuesta = await cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Healthy", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public void MigracionesSqlServer_DebenEstarSincronizadasConElModelo()
    {
        // Comparación de metadatos: no abre una conexión ni modifica bases de datos.
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=PruebaModelo;Integrated Security=True;")
            .Options;
        using var db = new ApplicationDbContext(opciones);
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
