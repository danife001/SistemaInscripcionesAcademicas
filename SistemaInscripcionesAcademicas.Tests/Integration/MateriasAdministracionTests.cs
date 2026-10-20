using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.DTOs.Materias;

namespace SistemaInscripcionesAcademicas.Tests.Integration;

public class MateriasAdministracionTests
{
    private static object Datos(int profesor, string nombre = "Materia", int creditos = 3) =>
        new { nombre, creditos, profesorId = profesor };

    private static async Task<(int estudiante, int[] profesores)> Preparar(AplicacionPruebas app, HttpClient admin, HttpClient alumno)
    {
        foreach (var correo in new[] { "admin@example.com", "alumno@example.com" })
            Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/estudiantes", new {
                nombre = correo, correo, password = "Prueba123!" })).StatusCode);
        int estudiante;
        int[] ids;
        using (var scope = app.Services.CreateScope()) {
            var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            Assert.True((await usuarios.AddToRoleAsync((await usuarios.FindByEmailAsync("admin@example.com"))!, "Administrador")).Succeeded);
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var profesores = new[] { new Profesor { Nombre = "Profesor A", Activo = true }, new Profesor { Nombre = "Profesor B", Activo = true }, new Profesor { Nombre = "Inactivo", Activo = false } };
            db.Profesores.AddRange(profesores);
            await db.SaveChangesAsync();
            ids = profesores.Select(p => p.Id).ToArray();
            estudiante = await db.Estudiantes.Where(e => e.Usuario.Email == "alumno@example.com").Select(e => e.Id).SingleAsync();
        }
        await Login(admin, "admin@example.com"); await Login(alumno, "alumno@example.com");
        return (estudiante, ids);
    }
    private static async Task Login(HttpClient cliente, string correo)
    {
        var login = await cliente.PostAsJsonAsync("/api/auth/login", new { correo, password = "Prueba123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var datos = await login.Content.ReadFromJsonAsync<JsonElement>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", datos.GetProperty("accessToken").GetString());
    }
    private static async Task<int> Crear(HttpClient admin, int profesor, string nombre = "Materia")
    {
        var respuesta = await admin.PostAsJsonAsync("/api/materias", Datos(profesor, nombre));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var materia = (await respuesta.Content.ReadFromJsonAsync<MateriaDto>())!;
        Assert.EndsWith($"/api/materias/{materia.Id}", respuesta.Headers.Location!.ToString());
        return materia.Id;
    }

    [Fact]
    public async Task Crud_ValidaCatalogoYConservaBajaLogica()
    {
        await using var app = new AplicacionPruebas();
        using var admin = app.CreateClient(); using var alumno = app.CreateClient();
        var (_, profesores) = await Preparar(app, admin, alumno);
        var id = await Crear(admin, profesores[0]);
        await Crear(admin, profesores[0], "Segunda");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/materias", Datos(profesores[0], "Tercera"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/materias", Datos(profesores[1], creditos: 4))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/materias", Datos(profesores[1], "  "))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/materias", Datos(profesores[2]))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/materias", Datos(999999))).StatusCode);
        var cambio = await admin.PutAsJsonAsync($"/api/materias/{id}", Datos(profesores[1], "Actualizada"));
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        var materia = (await cambio.Content.ReadFromJsonAsync<MateriaDto>())!;
        Assert.Equal("Profesor B", materia.Profesor);
        Assert.Equal(profesores[1], materia.ProfesorId);
        Assert.Equal("Actualizada", (await admin.GetFromJsonAsync<MateriaDto>($"/api/materias/{id}"))!.Nombre);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/materias/{id}")).StatusCode);
        Assert.False((await admin.GetFromJsonAsync<MateriaDto>($"/api/materias/{id}"))!.Activo);
        Assert.DoesNotContain((await alumno.GetFromJsonAsync<List<MateriaDto>>("/api/materias"))!, m => m.Id == id);
        Assert.Contains((await admin.GetFromJsonAsync<List<MateriaDto>>("/api/materias"))!, m => m.Id == id && !m.Activo);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/materias/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/materias/{id}", Datos(profesores[1]))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/materias/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync("/api/materias/999999", Datos(profesores[1]))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync("/api/materias/999999")).StatusCode);
    }

    [Fact]
    public async Task Inscripciones_BloqueanCambiosYBajaHastaCancelar()
    {
        await using var app = new AplicacionPruebas();
        using var admin = app.CreateClient(); using var alumno = app.CreateClient();
        var (_, profesores) = await Preparar(app, admin, alumno);
        var id = await Crear(admin, profesores[0]);
        var alta = await alumno.PostAsJsonAsync("/api/inscripciones", new { materiaId = id });
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var inscripcionId = (await alta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/materias/{id}", Datos(profesores[1]))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/materias/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alumno.DeleteAsync($"/api/inscripciones/{inscripcionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/materias/{id}", Datos(profesores[0], "Renombrada"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/materias/{id}")).StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var historial = await db.Inscripciones.Include(i => i.Materia).SingleAsync();
        Assert.False(historial.Activa); Assert.False(historial.Materia.Activo);
        Assert.Equal("Materia", historial.Materia.Nombre); Assert.Equal(profesores[0], historial.Materia.ProfesorId);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Gestion_ExigeAdministrador(string metodo)
    {
        await using var app = new AplicacionPruebas();
        using var admin = app.CreateClient(); using var alumno = app.CreateClient(); using var anonimo = app.CreateClient();
        var (_, profesores) = await Preparar(app, admin, alumno);
        var id = await Crear(admin, profesores[0]);
        foreach (var (cliente, estado) in new[] { (alumno, HttpStatusCode.Forbidden), (anonimo, HttpStatusCode.Unauthorized) }) {
            using var mensaje = new HttpRequestMessage(new HttpMethod(metodo), metodo == "POST" ? "/api/materias" : $"/api/materias/{id}");
            if (metodo is "POST" or "PUT") mensaje.Content = JsonContent.Create(Datos(profesores[1]));
            Assert.Equal(estado, (await cliente.SendAsync(mensaje)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await alumno.GetAsync("/api/materias")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/materias")).StatusCode);
    }

    [SqlServerFact]
    public async Task SqlServer_AltasConcurrentesNoSuperanDosMateriasPorProfesor()
    {
        var conexion = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION")) {
            InitialCatalog = $"InscripcionesConcurrency_{Guid.NewGuid():N}" };
        await using var app = new AplicacionPruebas(conexionSqlServer: conexion.ConnectionString);
        using var admin = app.CreateClient(); using var alumno = app.CreateClient();
        var (_, profesores) = await Preparar(app, admin, alumno);
        var respuestas = await Task.WhenAll(Enumerable.Range(1, 3).Select(n => admin.PostAsJsonAsync("/api/materias", Datos(profesores[0], $"Materia {n}"))));
        Assert.Equal(2, respuestas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        using var scope = app.Services.CreateScope();
        Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Materias.CountAsync(m => m.Activo));
    }
    [SqlServerFact]
    public async Task SqlServer_BajaSimultaneaConInscripcionConservaEstadoValido()
    {
        var conexion = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION")) {
            InitialCatalog = $"InscripcionesConcurrency_{Guid.NewGuid():N}" };
        await using var app = new AplicacionPruebas(conexionSqlServer: conexion.ConnectionString);
        using var admin = app.CreateClient(); using var alumno = app.CreateClient();
        var (_, profesores) = await Preparar(app, admin, alumno);
        var id = await Crear(admin, profesores[0]);
        var alta = alumno.PostAsJsonAsync("/api/inscripciones", new { materiaId = id });
        var baja = admin.DeleteAsync($"/api/materias/{id}");
        await Task.WhenAll(alta, baja);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var materia = await db.Materias.SingleAsync();
        if (materia.Activo) {
            Assert.Equal(HttpStatusCode.Created, alta.Result.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, baja.Result.StatusCode);
            Assert.Equal(1, await db.Inscripciones.CountAsync(i => i.Activa));
        } else {
            Assert.Equal(HttpStatusCode.BadRequest, alta.Result.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, baja.Result.StatusCode);
            Assert.Empty(await db.Inscripciones.ToListAsync());
        }
    }

}
