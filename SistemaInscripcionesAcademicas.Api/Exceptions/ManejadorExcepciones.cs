using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SistemaInscripcionesAcademicas.Api.Exceptions;

/// <summary>Traduce excepciones de solicitudes HTTP sin exponer detalles internos.</summary>
public sealed class ManejadorExcepciones(ILogger<ManejadorExcepciones> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion,
        CancellationToken cancellationToken)
    {
        var (estado, titulo, mensaje) = excepcion switch
        {
            ReglaNegocioException regla => (regla.Conflicto ? 409 : 400,
                regla.Conflicto ? "Conflicto de negocio" : "Solicitud inválida", regla.Message),
            ConflictoInscripcionException conflicto => (409, "Operación en conflicto", conflicto.Message),
            _ => (500, "Error interno", "Ocurrió un error inesperado. Intenta nuevamente.")
        };
        var traceId = Activity.Current?.TraceId.ToString() ?? contexto.TraceIdentifier;
        // Solo errores explícitamente clasificados exponen su mensaje; fallos técnicos son 500.
        if (estado == 500)
            logger.LogError(excepcion, "Error no controlado. TraceId: {TraceId}", traceId);
        else
            logger.LogWarning("Solicitud rechazada con estado {Estado}. TraceId: {TraceId}", estado, traceId);

        var problema = new ProblemDetails {
            Status = estado, Title = titulo, Detail = mensaje, Instance = contexto.Request.Path
        };
        problema.Extensions["mensaje"] = mensaje; // Compatibilidad con el cliente React.
        problema.Extensions["traceId"] = traceId;
        contexto.Response.StatusCode = estado;
        await contexto.Response.WriteAsJsonAsync(problema, options: null,
            contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
