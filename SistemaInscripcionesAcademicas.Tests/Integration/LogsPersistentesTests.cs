using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaInscripcionesAcademicas.Api.Extensions;

namespace SistemaInscripcionesAcademicas.Tests.Integration;

public class LogsPersistentesTests : IDisposable
{
    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "logs-inscripciones-" + Guid.NewGuid().ToString("N"));
    private IConfiguration Configuracion(long bytes = 10485760, int archivos = 14) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Logging:File:Directory"] = carpeta,
            ["Logging:File:FileSizeLimitBytes"] = bytes.ToString(),
            ["Logging:File:RetainedFileCountLimit"] = archivos.ToString()
        }).Build();

    [Fact]
    public void Reinicio_ConservaEventosPreviosYFormatoJson()
    {
        using (var logger = SistemaInscripcionesAcademicas.Api.Extensions.LoggingExtensions.CrearLoggerArchivo(Configuracion(), Path.GetTempPath()))
            logger.Information("Primera ejecución {TraceId}", "traza-uno");
        using (var logger = SistemaInscripcionesAcademicas.Api.Extensions.LoggingExtensions.CrearLoggerArchivo(Configuracion(), Path.GetTempPath()))
            logger.Error(new InvalidOperationException("Detalle de prueba"), "Segunda ejecución {TraceId}", "traza-dos");
        var lineas = Directory.GetFiles(carpeta).SelectMany(LeerCompartido).ToArray();
        Assert.Equal(2, lineas.Length);
        var primero = JsonDocument.Parse(lineas[0]).RootElement;
        Assert.Equal("traza-uno", primero.GetProperty("Properties").GetProperty("TraceId").GetString());
        var segundo = JsonDocument.Parse(lineas[1]).RootElement;
        Assert.Equal("Error", segundo.GetProperty("Level").GetString());
        Assert.Contains("Detalle de prueba", segundo.GetProperty("Exception").GetString());
    }

    [Fact]
    public void RotacionPorTamano_RespetaRetencionYConservaEventoReciente()
    {
        using (var logger = SistemaInscripcionesAcademicas.Api.Extensions.LoggingExtensions.CrearLoggerArchivo(Configuracion(bytes: 200, archivos: 2), Path.GetTempPath()))
            for (var i = 0; i < 12; i++) logger.Information("Evento {Numero}: {Texto}", i, new string('x', 200));
        var archivos = Directory.GetFiles(carpeta);
        Assert.Equal(2, archivos.Length);
        var eventos = archivos.SelectMany(LeerCompartido).Select(l => JsonDocument.Parse(l).RootElement).ToArray();
        Assert.Contains(eventos, e => e.GetProperty("Properties").GetProperty("Numero").GetInt32() == 11);
        Assert.DoesNotContain(eventos, e => e.GetProperty("Properties").GetProperty("Numero").GetInt32() == 0);
    }

    [Fact]
    public async Task ErrorHttp_SePersisteConTraceIdDelCliente()
    {
        await using var app = new AplicacionPruebas(services => {
            services.AddControllers().AddApplicationPart(typeof(ErroresPruebaController).Assembly);
            services.AddLogging(logging => logging.AddArchivoPersistente(Configuracion(), Path.GetTempPath()));
        }, ambiente: "Production");
        using var cliente = app.CreateClient();
        var respuesta = await cliente.GetAsync("/pruebas/errores/interno");
        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;
        var traceId = json.GetProperty("traceId").GetString();
        var eventos = Directory.GetFiles(carpeta).SelectMany(LeerCompartido)
            .Select(l => JsonDocument.Parse(l).RootElement).ToArray();
        Assert.Contains(eventos, e => e.GetProperty("Level").GetString() == "Error"
            && e.TryGetProperty("Properties", out var props)
            && props.TryGetProperty("TraceId", out var traza) && traza.GetString() == traceId);
    }

    [Fact]
    public async Task EscriturasParalelas_NoPierdenEventos()
    {
        using (var logger = SistemaInscripcionesAcademicas.Api.Extensions.LoggingExtensions.CrearLoggerArchivo(Configuracion(), Path.GetTempPath()))
            await Task.WhenAll(Enumerable.Range(0, 50).Select(i => Task.Run(() => logger.Information("Evento {Numero}", i))));
        var numeros = Directory.GetFiles(carpeta).SelectMany(LeerCompartido)
            .Select(l => JsonDocument.Parse(l).RootElement.GetProperty("Properties").GetProperty("Numero").GetInt32()).ToArray();
        Assert.Equal(50, numeros.Distinct().Count());
    }

    private static IEnumerable<string> LeerCompartido(string archivo)
    {
        // Windows exige compartir escritura al leer mientras el proveedor mantiene abierto el log.
        using var stream = new FileStream(archivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } linea) yield return linea;
    }

    public void Dispose()
    {
        // La carpeta es exclusiva de esta prueba, generada bajo el directorio temporal.
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, recursive: true);
    }
}
