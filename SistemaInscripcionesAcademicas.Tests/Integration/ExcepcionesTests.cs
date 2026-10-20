using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaInscripcionesAcademicas.Api.Exceptions;

namespace SistemaInscripcionesAcademicas.Tests.Integration;

public class ExcepcionesTests
{
    [Theory]
    [InlineData("Development", "regla", 400)]
    [InlineData("Production", "regla", 400)]
    [InlineData("Development", "conflicto", 409)]
    [InlineData("Production", "bloqueo", 409)]
    [InlineData("Development", "interno", 500)]
    [InlineData("Production", "interno", 500)]
    [InlineData("Production", "invalidoperation", 500)]
    public async Task Handler_ConservaContratoYOcultaDetalles(string ambiente, string error, int estado)
    {
        var registro = new RegistroErrores();
        await using var app = new AplicacionPruebas(services => {
            // Este controlador solo existe en el servidor aislado de pruebas.
            services.AddControllers().AddApplicationPart(typeof(ErroresPruebaController).Assembly);
            services.AddSingleton<ILogger<ManejadorExcepciones>>(registro);
        }, ambiente: ambiente);
        using var cliente = app.CreateClient();
        var respuesta = await cliente.GetAsync($"/pruebas/errores/{error}");
        Assert.Equal((HttpStatusCode)estado, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType!.MediaType);
        var texto = await respuesta.Content.ReadAsStringAsync();
        var datos = JsonDocument.Parse(texto).RootElement;
        Assert.Equal(estado, datos.GetProperty("status").GetInt32());
        Assert.Equal(datos.GetProperty("detail").GetString(), datos.GetProperty("mensaje").GetString());
        var traceId = datos.GetProperty("traceId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(traceId));
        Assert.Contains(registro.Entradas, e => e.Texto.Contains(traceId!));
        if (estado == 500) {
            Assert.DoesNotContain("SECRETO_INTERNO", texto);
            Assert.DoesNotContain("stackTrace", texto);
            Assert.Contains(registro.Entradas, e => e.Nivel == LogLevel.Error && e.Error != null);
        } else {
            Assert.Contains(error == "bloqueo" ? "otra operación" : "Regla pública", datos.GetProperty("mensaje").GetString());
        }
    }

    [Fact]
    public async Task ValidacionDto_Conserva400YErroresPorCampo()
    {
        await using var app = new AplicacionPruebas();
        using var cliente = app.CreateClient();
        var respuesta = await cliente.PostAsJsonAsync("/api/estudiantes", new { nombre = "", correo = "incorrecto", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var datos = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(datos.TryGetProperty("errors", out _));
    }

    private sealed class RegistroErrores : ILogger<ManejadorExcepciones>
    {
        public ConcurrentQueue<(LogLevel Nivel, string Texto, Exception? Error)> Entradas { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Entradas.Enqueue((logLevel, formatter(state, exception), exception));
    }
}

[ApiController]
[AllowAnonymous]
[Route("pruebas/errores")]
public class ErroresPruebaController : ControllerBase
{
    [HttpGet("{tipo}")]
    public IActionResult Fallar(string tipo) => throw (tipo switch {
        "regla" => new ReglaNegocioException("Regla pública"),
        "conflicto" => new ReglaNegocioException("Regla pública", conflicto: true),
        "bloqueo" => new ConflictoInscripcionException(),
        "invalidoperation" => new InvalidOperationException("SECRETO_INTERNO"),
        _ => new Exception("SECRETO_INTERNO")
    });
}
