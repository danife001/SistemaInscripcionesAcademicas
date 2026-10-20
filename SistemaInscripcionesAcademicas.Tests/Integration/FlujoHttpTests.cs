using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.DTOs.Comun;
using SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;
using SistemaInscripcionesAcademicas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace SistemaInscripcionesAcademicas.Tests.Integration;

public class FlujoHttpTests
{
    [Fact]
    public async Task RegistroInvalido_DebeResponder400SinCrearUsuarioNiAsignarRol()
    {
        var control = new ControlAsignacionRol();
        await using var app = CrearAplicacionConControl(control);
        using var cliente = app.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/estudiantes", new
        {
            nombre = "Prueba", correo = "invalido@example.com", password = "abc"
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, control.Intentos);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Users.ToListAsync());
        Assert.Empty(await db.Estudiantes.ToListAsync());
    }

    [Fact]
    public async Task FalloAlAsignarRol_DebeResponder400YRevertirTodaLaTransaccion()
    {
        var control = new ControlAsignacionRol { Rechazar = true };
        await using var app = CrearAplicacionConControl(control);
        using var cliente = app.CreateClient();

        var respuesta = await RegistrarAsync(cliente, "rol@example.com");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("No fue posible asignar el rol", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(1, control.Intentos);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Users.ToListAsync());
        Assert.Empty(await db.Estudiantes.ToListAsync());
        Assert.Empty(await db.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task Actualizacion_DebePersistirYPermitirLoginConNuevoCorreo()
    {
        await using var app = new AplicacionPruebas();
        using var cliente = app.CreateClient();
        Assert.Equal(HttpStatusCode.Created, (await RegistrarAsync(cliente, "antes@example.com")).StatusCode);
        await LoginAsync(cliente, "antes@example.com");

        var actualizacion = await cliente.PutAsJsonAsync("/api/estudiantes/me", new
        {
            nombre = "Nombre actualizado", correo = "despues@example.com"
        });
        Assert.Equal(HttpStatusCode.OK, actualizacion.StatusCode);
        var perfil = await cliente.GetFromJsonAsync<EstudianteDto>("/api/estudiantes/me");
        Assert.NotNull(perfil);
        Assert.Equal("Nombre actualizado", perfil.Nombre);
        Assert.Equal("despues@example.com", perfil.Correo);

        using var nuevoCliente = app.CreateClient();
        await LoginAsync(nuevoCliente, "despues@example.com");
        var anterior = await nuevoCliente.PostAsJsonAsync("/api/auth/login", new
        {
            correo = "antes@example.com", password = "Prueba123!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anterior.StatusCode);

        using var scope = app.Services.CreateScope();
        var usuario = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.SingleAsync();
        Assert.Equal("Nombre actualizado", usuario.Nombre);
        Assert.Equal("DESPUES@EXAMPLE.COM", usuario.NormalizedEmail);
    }

    [Fact]
    public async Task Paginacion_DebeValidarParametrosOrdenarYRespetarPermisosHttp()
    {
        await using var app = new AplicacionPruebas();
        using var cliente = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await cliente.GetAsync("/api/registros/estudiantes")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await RegistrarAsync(cliente, "propio@example.com")).StatusCode);
        await LoginAsync(cliente, "propio@example.com");

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (int i = 1; i <= 12; i++)
                db.Estudiantes.Add(new Estudiante
                {
                    Usuario = new Usuario
                    {
                        Nombre = $"Alumno {i:00}", Email = $"otro{i}@example.com",
                        UserName = $"otro{i}@example.com"
                    }
                });
            await db.SaveChangesAsync();
        }

        var pagina = await cliente.GetFromJsonAsync<ResultadoPaginado<RegistroEstudianteDto>>(
            "/api/registros/estudiantes?pagina=2&tamanoPagina=5");
        Assert.NotNull(pagina);
        Assert.Equal(12, pagina.TotalRegistros);
        Assert.Equal(3, pagina.TotalPaginas);
        Assert.Equal(5, pagina.Items.Count);
        Assert.Equal("Alumno 06", pagina.Items[0].Nombre);
        Assert.Equal("Alumno 10", pagina.Items[^1].Nombre);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await cliente.GetAsync("/api/registros/estudiantes?pagina=0&tamanoPagina=101")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await cliente.GetAsync("/api/estudiantes")).StatusCode);
    }

    [Fact]
    public async Task Repositorios_DebenConservarInscripcionesCancelacionYPrivacidad()
    {
        await using var app = new AplicacionPruebas();
        using var primero = app.CreateClient();
        using var segundo = app.CreateClient();
        Assert.Equal(HttpStatusCode.Created, (await RegistrarAsync(primero, "primero@example.com")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await RegistrarAsync(segundo, "segundo@example.com")).StatusCode);
        await LoginAsync(primero, "primero@example.com");
        await LoginAsync(segundo, "segundo@example.com");

        int materiaId, otraMateriaId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var profesor = new Profesor { Nombre = "Profesor activo" };
            var materia = new Materia { Nombre = "Primera", Profesor = profesor };
            var otra = new Materia { Nombre = "Segunda", Profesor = profesor };
            db.Materias.AddRange(materia, otra,
                new Materia { Nombre = "Oculta", Profesor = profesor, Activo = false },
                new Materia { Nombre = "Profesor inactivo", Profesor = new Profesor
                {
                    Nombre = "Inactivo", Activo = false
                } });
            await db.SaveChangesAsync();
            materiaId = materia.Id;
            otraMateriaId = otra.Id;
        }

        var disponibles = await primero.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/materias");
        Assert.Equal(2, disponibles.GetArrayLength());
        var inscripcionPrimero = await primero.PostAsJsonAsync("/api/inscripciones", new { materiaId });
        var inscripcionSegundo = await segundo.PostAsJsonAsync("/api/inscripciones", new { materiaId });
        Assert.Equal(HttpStatusCode.Created, inscripcionPrimero.StatusCode);
        Assert.Equal(HttpStatusCode.Created, inscripcionSegundo.StatusCode);
        var datosPrimero = await inscripcionPrimero.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var datosSegundo = await inscripcionSegundo.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await primero.PostAsJsonAsync("/api/inscripciones", new { materiaId })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await primero.PostAsJsonAsync("/api/inscripciones", new { materiaId = otraMateriaId })).StatusCode);

        var companeros = await primero.GetFromJsonAsync<System.Text.Json.JsonElement>(
            $"/api/inscripciones/materias/{materiaId}/compas");
        Assert.Equal(1, companeros.GetArrayLength());
        Assert.Equal("Estudiante de prueba", companeros[0].GetProperty("nombre").GetString());
        Assert.Single(companeros[0].EnumerateObject());
        Assert.Equal(HttpStatusCode.NotFound,
            (await primero.DeleteAsync($"/api/inscripciones/{datosSegundo.GetProperty("id").GetInt32()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await primero.DeleteAsync($"/api/inscripciones/{datosPrimero.GetProperty("id").GetInt32()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await primero.GetAsync($"/api/inscripciones/materias/{materiaId}/compas")).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await primero.PostAsJsonAsync("/api/inscripciones", new { materiaId })).StatusCode);

        using var verificacion = app.Services.CreateScope();
        var contexto = verificacion.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(3, await contexto.Inscripciones.CountAsync());
        Assert.Equal(2, await contexto.Inscripciones.CountAsync(i => i.Activa));
    }

    private static Task<HttpResponseMessage> RegistrarAsync(HttpClient cliente, string correo) =>
        cliente.PostAsJsonAsync("/api/estudiantes", new
        {
            nombre = "Estudiante de prueba", correo, password = "Prueba123!"
        });

    private static async Task LoginAsync(HttpClient cliente, string correo)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new
        {
            correo, password = "Prueba123!"
        });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var datos = await respuesta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", datos.GetProperty("accessToken").GetString());
    }

    private static AplicacionPruebas CrearAplicacionConControl(ControlAsignacionRol control) =>
        new(services =>
        {
            services.AddSingleton(control);
            services.AddScoped<UserManager<Usuario>, UserManagerControlado>();
        });

    public class ControlAsignacionRol
    {
        public bool Rechazar { get; init; }
        public int Intentos { get; set; }
    }

    public class UserManagerControlado : UserManager<Usuario>
    {
        private readonly ControlAsignacionRol _control;

        public UserManagerControlado(IUserStore<Usuario> store, IOptions<IdentityOptions> options,
            IPasswordHasher<Usuario> hasher, IEnumerable<IUserValidator<Usuario>> userValidators,
            IEnumerable<IPasswordValidator<Usuario>> passwordValidators, ILookupNormalizer normalizer,
            IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<Usuario>> logger,
            ControlAsignacionRol control)
            : base(store, options, hasher, userValidators, passwordValidators, normalizer, errors, services, logger)
        {
            _control = control;
        }

        public override Task<IdentityResult> AddToRoleAsync(Usuario user, string role)
        {
            _control.Intentos++;
            return _control.Rechazar
                ? Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "PruebaRol", Description = "Asignación rechazada en la prueba."
                }))
                : base.AddToRoleAsync(user, role);
        }
    }
}
