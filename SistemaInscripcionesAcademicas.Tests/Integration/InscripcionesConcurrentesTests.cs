using Microsoft.Data.SqlClient;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.DTOs.Inscripciones;
using SistemaInscripcionesAcademicas.Api.Exceptions;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Repositories;
using SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;

namespace SistemaInscripcionesAcademicas.Tests.Integration;

public class InscripcionesConcurrentesTests
{
    [Theory]
    [InlineData("limite", 3, "más de tres materias")]
    [InlineData("profesor", 1, "mismo profesor")]
    [InlineData("duplicada", 1, "Ya está inscrito")]
    public Task SolicitudesSolapadas_DebenConservarReglas(
        string escenario, int totalEsperado, string mensajeEsperado) =>
        VerificarSolicitudesAsync(escenario, totalEsperado, mensajeEsperado);

    [SqlServerTheory]
    [InlineData("limite", 3, "más de tres materias")]
    [InlineData("profesor", 1, "mismo profesor")]
    [InlineData("duplicada", 1, "Ya está inscrito")]
    public Task SqlServer_DosInstancias_DebenConservarReglas(
        string escenario, int totalEsperado, string mensajeEsperado) =>
        VerificarSolicitudesAsync(escenario, totalEsperado, mensajeEsperado, CrearConexionSqlServer());

    private static async Task VerificarSolicitudesAsync(
        string escenario, int totalEsperado, string mensajeEsperado, string? conexionSqlServer = null)
    {
        var control = new ControlConcurrencia();
        await using var app = CrearAplicacion(control, conexionSqlServer);
        await using var otraApp = conexionSqlServer == null ? null
            : CrearAplicacion(control, conexionSqlServer, eliminarBaseSqlServer: false);
        using var cliente = app.CreateClient();
        var (estudianteId, materias) = await PrepararAsync(app, cliente);
        using var otroCliente = (otraApp ?? app).CreateClient();
        await LoginAsync(otroCliente);
        if (escenario == "limite")
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Inscripciones.AddRange(
                new Inscripcion { EstudianteId = estudianteId, MateriaId = materias[0] },
                new Inscripcion { EstudianteId = estudianteId, MateriaId = materias[2] });
            await db.SaveChangesAsync();
        }

        int primeraMateria = escenario == "limite" ? materias[4] : materias[0];
        int segundaMateria = escenario == "limite" ? materias[6]
            : escenario == "profesor" ? materias[1] : materias[0];

        var primera = cliente.PostAsJsonAsync("/api/inscripciones", new { materiaId = primeraMateria });
        await control.PrimeraLectura.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Task<HttpResponseMessage>? segunda = null;
        try
        {
            segunda = otroCliente.PostAsJsonAsync("/api/inscripciones", new { materiaId = segundaMateria });
            await control.SegundaOperacion.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.False(segunda.IsCompleted);
        }
        finally
        {
            control.Continuar.TrySetResult();
        }

        var respuestas = await Task.WhenAll(primera, segunda!).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(HttpStatusCode.Created, respuestas[0].StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, respuestas[1].StatusCode);
        Assert.Contains(mensajeEsperado, await respuestas[1].Content.ReadAsStringAsync());

        using var verificacion = app.Services.CreateScope();
        var contexto = verificacion.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var activas = await contexto.Inscripciones.Include(i => i.Materia)
            .Where(i => i.EstudianteId == estudianteId && i.Activa).ToListAsync();
        Assert.Equal(totalEsperado, activas.Count);
        Assert.Equal(activas.Count, activas.Select(i => i.MateriaId).Distinct().Count());
        Assert.Equal(activas.Count, activas.Select(i => i.Materia.ProfesorId).Distinct().Count());
    }

    [Fact]
    public Task CancelacionSolapadaConInscripcion_DebeLiberarCupoAntesDeValidar() =>
        VerificarCancelacionAsync();

    [SqlServerFact]
    public Task SqlServer_DosInstancias_DebenCoordinarCancelacionEInscripcion() =>
        VerificarCancelacionAsync(CrearConexionSqlServer());

    private static async Task VerificarCancelacionAsync(string? conexionSqlServer = null)
    {
        var control = new ControlConcurrencia { PausarConfirmacion = true };
        await using var app = CrearAplicacion(control, conexionSqlServer);
        await using var otraApp = conexionSqlServer == null ? null
            : CrearAplicacion(control, conexionSqlServer, eliminarBaseSqlServer: false);
        using var cliente = app.CreateClient();
        var (estudianteId, materias) = await PrepararAsync(app, cliente);
        using var otroCliente = (otraApp ?? app).CreateClient();
        await LoginAsync(otroCliente);
        int cancelarId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cancelar = new Inscripcion { EstudianteId = estudianteId, MateriaId = materias[0] };
            db.Inscripciones.AddRange(cancelar,
                new Inscripcion { EstudianteId = estudianteId, MateriaId = materias[2] },
                new Inscripcion { EstudianteId = estudianteId, MateriaId = materias[4] });
            await db.SaveChangesAsync();
            cancelarId = cancelar.Id;
        }

        var cancelacion = cliente.DeleteAsync($"/api/inscripciones/{cancelarId}");
        await control.PrimeraLectura.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Task<HttpResponseMessage>? inscripcion = null;
        try
        {
            inscripcion = otroCliente.PostAsJsonAsync("/api/inscripciones", new { materiaId = materias[6] });
            await control.SegundaOperacion.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.False(inscripcion.IsCompleted);
        }
        finally
        {
            control.Continuar.TrySetResult();
        }

        var resultados = await Task.WhenAll(cancelacion, inscripcion!).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(HttpStatusCode.NoContent, resultados[0].StatusCode);
        Assert.Equal(HttpStatusCode.Created, resultados[1].StatusCode);
        using var verificacion = app.Services.CreateScope();
        var contexto = verificacion.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(3, await contexto.Inscripciones.CountAsync(i => i.Activa));
        Assert.False((await contexto.Inscripciones.FindAsync(cancelarId))!.Activa);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task BloqueoNoDisponible_DebeResponder409SinGuardar(string metodo)
    {
        var control = new ControlConcurrencia { SimularConflicto = true };
        await using var app = CrearAplicacion(control);
        using var cliente = app.CreateClient();
        var (_, materias) = await PrepararAsync(app, cliente);
        var respuesta = metodo == "POST"
            ? await cliente.PostAsJsonAsync("/api/inscripciones", new { materiaId = materias[0] })
            : await cliente.DeleteAsync("/api/inscripciones/1");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains("otra operación", await respuesta.Content.ReadAsStringAsync());
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Inscripciones.ToListAsync());
    }

    private static AplicacionPruebas CrearAplicacion(ControlConcurrencia control,
        string? conexionSqlServer = null, bool eliminarBaseSqlServer = true) => new(services =>
    {
        services.AddScoped<UnidadTrabajo>();
        services.AddScoped<InscripcionRepository>();
        services.AddScoped<IUnidadTrabajo>(sp => new UnidadCoordinada(
            sp.GetRequiredService<UnidadTrabajo>(), control));
        services.AddScoped<IInscripcionRepository>(sp => new RepositorioCoordinado(
            sp.GetRequiredService<InscripcionRepository>(), control));
    }, conexionesIndependientes: true, conexionSqlServer: conexionSqlServer,
        eliminarBaseSqlServer: eliminarBaseSqlServer);

    private static async Task<(int EstudianteId, int[] Materias)> PrepararAsync(
        AplicacionPruebas app, HttpClient cliente)
    {
        var registro = await cliente.PostAsJsonAsync("/api/estudiantes", new
        {
            nombre = "Prueba concurrente", correo = "concurrencia@example.com", password = "Prueba123!"
        });
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        var datos = await registro.Content.ReadFromJsonAsync<JsonElement>();
        await LoginAsync(cliente);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await CatalogoInitializer.InitializeAsync(db);
        return (datos.GetProperty("id").GetInt32(), await db.Materias.OrderBy(m => m.ProfesorId)
            .ThenBy(m => m.Id).Select(m => m.Id).ToArrayAsync());
    }

    private static async Task LoginAsync(HttpClient cliente)
    {
        var login = await cliente.PostAsJsonAsync("/api/auth/login", new
        {
            correo = "concurrencia@example.com", password = "Prueba123!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", tokens.GetProperty("accessToken").GetString());
    }

    private static string CrearConexionSqlServer()
    {
        var conexion = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION"))
        {
            InitialCatalog = $"InscripcionesConcurrency_{Guid.NewGuid():N}"
        };
        return conexion.ConnectionString;
    }

    private sealed class ControlConcurrencia
    {
        public TaskCompletionSource PrimeraLectura { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SegundaOperacion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Operaciones;
        public int Lecturas;
        public bool PausarConfirmacion { get; init; }
        public bool SimularConflicto { get; init; }

        public async Task PausarAsync()
        {
            PrimeraLectura.TrySetResult();
            await Continuar.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }
    }

    private sealed class UnidadCoordinada(IUnidadTrabajo real, ControlConcurrencia control) : IUnidadTrabajo
    {
        public Task<ITransaccionDatos> IniciarTransaccionCatalogoAsync(CancellationToken ct = default) =>
            real.IniciarTransaccionCatalogoAsync(ct);
        public Task<int> GuardarCambiosAsync(CancellationToken ct = default) => real.GuardarCambiosAsync(ct);
        public Task<ITransaccionDatos> IniciarTransaccionAsync(CancellationToken ct = default) =>
            real.IniciarTransaccionAsync(ct);

        public async Task<ITransaccionDatos> IniciarTransaccionInscripcionesAsync(int id, CancellationToken ct = default)
        {
            if (control.SimularConflicto) throw new ConflictoInscripcionException();
            var numero = Interlocked.Increment(ref control.Operaciones);
            if (numero == 2) control.SegundaOperacion.TrySetResult();
            var transaccion = await real.IniciarTransaccionInscripcionesAsync(id, ct);
            return numero == 1 && control.PausarConfirmacion
                ? new TransaccionCoordinada(transaccion, control)
                : transaccion;
        }
    }

    private sealed class TransaccionCoordinada(ITransaccionDatos real, ControlConcurrencia control) : ITransaccionDatos
    {
        public async Task ConfirmarAsync(CancellationToken ct = default)
        {
            await control.PausarAsync();
            await real.ConfirmarAsync(ct);
        }
        public ValueTask DisposeAsync() => real.DisposeAsync();
    }

    private sealed class RepositorioCoordinado(IInscripcionRepository real, ControlConcurrencia control)
        : IInscripcionRepository
    {
        public async Task<int> ContarActivasAsync(int id)
        {
            var resultado = await real.ContarActivasAsync(id);
            if (!control.PausarConfirmacion && Interlocked.Increment(ref control.Lecturas) == 1)
                await control.PausarAsync();
            return resultado;
        }
        public void Agregar(Inscripcion i) => real.Agregar(i);
        public Task<bool> ExisteActivaAsync(int e, int m) => real.ExisteActivaAsync(e, m);
        public Task<bool> ExisteProfesorActivoAsync(int e, int p) => real.ExisteProfesorActivoAsync(e, p);
        public Task<Inscripcion?> ObtenerActivaPropiaAsync(int e, int i) => real.ObtenerActivaPropiaAsync(e, i);
        public Task<List<InscripcionDto>> ObtenerActivasAsync(int e) => real.ObtenerActivasAsync(e);
        public Task<List<CompaDto>> ObtenerCompanerosAsync(int e, int m) => real.ObtenerCompanerosAsync(e, m);
    }
}
