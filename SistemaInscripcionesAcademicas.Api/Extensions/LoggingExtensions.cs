using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Json;

namespace SistemaInscripcionesAcademicas.Api.Extensions;

public static class LoggingExtensions
{
    public static ILoggingBuilder AddArchivoPersistente(this ILoggingBuilder logging,
        IConfiguration configuracion, string contentRoot)
    {
        if (!configuracion.GetValue("Logging:File:Enabled", true)) return logging;
        // La factoría permite a las pruebas sustituir proveedores sin abrir archivos reales.
        logging.Services.AddSingleton<ILoggerProvider>(_ =>
            new SerilogLoggerProvider(CrearLoggerArchivo(configuracion, contentRoot), dispose: true));
        return logging;
    }

    public static Logger CrearLoggerArchivo(IConfiguration configuracion, string contentRoot)
    {
        var carpeta = configuracion["Logging:File:Directory"] ?? "logs";
        var ruta = Path.GetFullPath(carpeta, contentRoot);
        var maxBytes = configuracion.GetValue<long>("Logging:File:FileSizeLimitBytes", 10 * 1024 * 1024);
        var retenidos = configuracion.GetValue("Logging:File:RetainedFileCountLimit", 14);
        if (maxBytes <= 0 || retenidos <= 0)
            throw new InvalidOperationException("El tamaño y la retención de logs deben ser positivos.");
        Directory.CreateDirectory(ruta);
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(new JsonFormatter(renderMessage: true), Path.Combine(ruta, "api-.jsonl"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: maxBytes, rollOnFileSizeLimit: true,
                retainedFileCountLimit: retenidos, shared: true, buffered: false)
            .CreateLogger();
    }
}
