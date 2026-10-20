using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.Models;

namespace SistemaInscripcionesAcademicas.Tests.Integration;

public class AdministradorTests
{
    private static IConfiguration Configuracion(string correo = "admin@example.com", string clave = "Prueba123!") =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Administrador:CrearAlIniciar"] = "true", ["Administrador:Correo"] = correo,
            ["Administrador:Password"] = clave, ["Administrador:Nombre"] = "Administración"
        }).Build();

    private static Task Inicializar(IServiceProvider servicios, IConfiguration configuracion) =>
        AdministradorInitializer.InitializeAsync(servicios.GetRequiredService<UserManager<Usuario>>(),
            servicios.GetRequiredService<ApplicationDbContext>(), configuracion);

    private static async Task Login(HttpClient cliente, string correo)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new { correo, password = "Prueba123!" });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }

    [Fact]
    public async Task ArranqueConfigurado_CreaAdministradorYPermiteGestionCompleta()
    {
        await using var original = new AplicacionPruebas();
        using var app = original.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddConfiguration(Configuracion())));
        using var cliente = app.CreateClient();
        await Login(cliente, "admin@example.com");
        var sesion = await cliente.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Contains("Administrador", sesion.GetProperty("roles").EnumerateArray().Select(x => x.GetString()));
        var creado = await cliente.PostAsJsonAsync("/api/estudiantes", new { nombre = "Alumno", correo = "alumno@example.com", password = "Prueba123!" });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var id = (await creado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/estudiantes?pagina=1&tamanoPagina=1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PutAsJsonAsync($"/api/estudiantes/{id}", new { nombre = "Actualizado", correo = "actualizado@example.com" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/estudiantes/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/estudiantes/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PostAsJsonAsync("/api/auth/login", new { correo = "actualizado@example.com", password = "Prueba123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PostAsJsonAsync("/api/auth/login", new { correo = "admin@example.com", password = "Incorrecta123!" })).StatusCode);
    }

    [Fact]
    public async Task Inicializacion_RepetibleNoCambiaClaveNiCreaEstudiante()
    {
        await using var app = new AplicacionPruebas();
        using var cliente = app.CreateClient();
        using (var scope = app.Services.CreateScope()) {
            await Inicializar(scope.ServiceProvider, Configuracion());
            await Inicializar(scope.ServiceProvider, Configuracion(clave: "Otra123!"));
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Single(await db.Users.ToListAsync());
            Assert.Empty(await db.Estudiantes.ToListAsync());
        }
        await Login(cliente, "admin@example.com");
    }

    [Fact]
    public async Task RegistroPublico_NoPermiteElegirRolNiElevarCuentaExistente()
    {
        await using var app = new AplicacionPruebas();
        using var cliente = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/estudiantes")).StatusCode);
        var creado = await cliente.PostAsJsonAsync("/api/estudiantes", new {
            nombre = "Alumno", correo = "alumno@example.com", password = "Prueba123!", rol = "Administrador" });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var id = (await creado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await Login(cliente, "alumno@example.com");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/estudiantes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PutAsJsonAsync($"/api/estudiantes/{id}", new { nombre = "Cambio", correo = "otro@example.com" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.DeleteAsync($"/api/estudiantes/{id}")).StatusCode);
        using var scope = app.Services.CreateScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Inicializar(scope.ServiceProvider, Configuracion("alumno@example.com")));
        var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Assert.False(await usuarios.IsInRoleAsync((await usuarios.FindByEmailAsync("alumno@example.com"))!, "Administrador"));
    }

    [Fact]
    public async Task Inicializacion_DeshabilitadaNoCreaUsuarioYClaveInvalidaRevierte()
    {
        await using var app = new AplicacionPruebas();
        using var cliente = app.CreateClient();
        using (var scope = app.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Empty(await db.Users.ToListAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => Inicializar(scope.ServiceProvider, Configuracion(clave: "abc")));
        }
        using var verificacion = app.Services.CreateScope();
        Assert.Empty(await verificacion.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.ToListAsync());
    }
}
