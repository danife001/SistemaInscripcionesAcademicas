using System.Net;
using System.Text.Json;

namespace SistemaInscripcionesAcademicas.Tests.Integration;

public class SwaggerTests
{
    [Fact]
    public async Task Desarrollo_DebePublicarInterfazYDocumentoConPermisosCorrectos()
    {
        await using var app = new AplicacionPruebas(ambiente: "Development");
        using var cliente = app.CreateClient();

        var interfaz = await cliente.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, interfaz.StatusCode);
        Assert.Contains("swagger-ui", await interfaz.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/swagger/swagger-ui-bundle.js")).StatusCode);

        var respuesta = await cliente.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var root = documento.RootElement;
        var esquema = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", esquema.GetProperty("type").GetString());
        Assert.Equal("bearer", esquema.GetProperty("scheme").GetString());
        Assert.False(esquema.TryGetProperty("bearerFormat", out _));

        var rutas = root.GetProperty("paths");
        Assert.True(rutas.GetProperty("/api/estudiantes/me").GetProperty("put").GetProperty("security")[0]
            .TryGetProperty("Bearer", out _));
        Assert.False(rutas.GetProperty("/api/auth/login").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(rutas.GetProperty("/api/Estudiantes").GetProperty("post").TryGetProperty("security", out _));
        Assert.True(rutas.GetProperty("/api/Estudiantes").GetProperty("post").GetProperty("responses")
            .TryGetProperty("201", out _));
        var login = rutas.GetProperty("/api/auth/login").GetProperty("post");
        Assert.True(rutas.GetProperty("/api/registros/estudiantes").GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("content")
            .TryGetProperty("application/json", out _));
        Assert.True(login.GetProperty("responses").GetProperty("200").GetProperty("content")
            .TryGetProperty("application/json", out _));
    }

    [Fact]
    public async Task Produccion_NoDebePublicarSwaggerNiOpenApi()
    {
        await using var app = new AplicacionPruebas(ambiente: "Production");
        using var cliente = app.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/swagger/index.html")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/openapi/v1.json")).StatusCode);
    }
}
