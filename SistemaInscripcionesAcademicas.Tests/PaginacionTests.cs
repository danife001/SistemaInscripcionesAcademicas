using SistemaInscripcionesAcademicas.Api.Extensions;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaInscripcionesAcademicas.Api.Controllers;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.DTOs.Comun;
using SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Services;

namespace SistemaInscripcionesAcademicas.Tests;

public class PaginacionTests : IDisposable
{
    private readonly ServiceProvider _proveedor;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _context;
    private readonly EstudianteService _service;

    public PaginacionTests()
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        var baseDatos = Guid.NewGuid().ToString();
        servicios.AddDbContext<ApplicationDbContext>(opciones =>
            opciones.UseInMemoryDatabase(baseDatos));
        servicios.AddIdentityCore<Usuario>().AddEntityFrameworkStores<ApplicationDbContext>();
        servicios.AddPersistenciaAcademica();
        servicios.AddScoped<EstudianteService>();
        servicios.AddScoped<IEstudianteService>(sp => sp.GetRequiredService<EstudianteService>());
        _proveedor = servicios.BuildServiceProvider();
        _scope = _proveedor.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _service = _scope.ServiceProvider.GetRequiredService<EstudianteService>();

        AgregarEstudiante("propio", "Alumno 00");
        for (int i = 1; i <= 12; i++)
            AgregarEstudiante($"otro-{i}", $"Alumno {i:00}");
        AgregarEstudiante("inactivo", "Alumno -1", activo: false);
        _context.SaveChanges();
    }

    private void AgregarEstudiante(string usuarioId, string nombre, bool activo = true)
    {
        _context.Estudiantes.Add(new Estudiante
        {
            Activo = activo,
            Usuario = new Usuario
            {
                Id = usuarioId,
                Nombre = nombre,
                Email = $"{usuarioId}@prueba.com",
                UserName = $"{usuarioId}@prueba.com"
            }
        });
    }

    private RegistrosController CrearController(string? usuarioId = "propio")
    {
        var claims = usuarioId == null
            ? Array.Empty<Claim>()
            : new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId) };
        return new RegistrosController(_service,
            _scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            }
        };
    }

    [Fact]
    public async Task ConsultaAdministrativa_DebeUsarDiezRegistrosPorDefecto()
    {
        var resultado = await _service.ObtenerTodosAsync(new PaginacionDto());

        Assert.Equal(10, resultado.Items.Count);
        Assert.Equal(13, resultado.TotalRegistros);
        Assert.Equal(2, resultado.TotalPaginas);
        Assert.Equal("Alumno 00", resultado.Items[0].Nombre);
        Assert.False(resultado.HayAnterior);
        Assert.True(resultado.HaySiguiente);
        Assert.DoesNotContain(resultado.Items, e => e.Nombre == "Alumno -1");
    }

    [Fact]
    public async Task UltimaPagina_DebeDevolverSoloLosRegistrosRestantes()
    {
        var resultado = await _service.ObtenerTodosAsync(
            new PaginacionDto { Pagina = 3, TamanoPagina = 5 });

        Assert.Equal(3, resultado.Items.Count);
        Assert.Equal(3, resultado.TotalPaginas);
        Assert.Equal("Alumno 10", resultado.Items[0].Nombre);
        Assert.True(resultado.HayAnterior);
        Assert.False(resultado.HaySiguiente);
    }

    [Fact]
    public async Task NombresRepetidos_DebeMantenerOrdenSinDuplicarEntrePaginas()
    {
        foreach (var usuario in _context.Users)
            usuario.Nombre = "Mismo nombre";
        await _context.SaveChangesAsync();

        var ids = new List<int>();
        for (int pagina = 1; pagina <= 3; pagina++)
        {
            var resultado = await _service.ObtenerTodosAsync(
                new PaginacionDto { Pagina = pagina, TamanoPagina = 5 });
            ids.AddRange(resultado.Items.Select(e => e.Id));
        }

        var esperados = await _context.Estudiantes.Where(e => e.Activo)
            .OrderBy(e => e.Id).Select(e => e.Id).ToListAsync();
        Assert.Equal(esperados, ids);
    }

    [Fact]
    public async Task Registros_DebePaginarExcluyendoAlSolicitanteYLosInactivos()
    {
        var respuesta = await CrearController().ObtenerRegistros(
            new PaginacionDto { Pagina = 2, TamanoPagina = 5 }, default);
        var resultado = Assert.IsType<ResultadoPaginado<RegistroEstudianteDto>>(
            Assert.IsType<OkObjectResult>(respuesta.Result).Value);

        Assert.Equal(12, resultado.TotalRegistros);
        Assert.Equal(3, resultado.TotalPaginas);
        Assert.Equal(5, resultado.Items.Count);
        Assert.Equal("Alumno 06", resultado.Items[0].Nombre);
        Assert.Equal("Alumno 10", resultado.Items[^1].Nombre);
        Assert.True(resultado.HayAnterior);
        Assert.True(resultado.HaySiguiente);
    }

    [Fact]
    public async Task SinOtrosEstudiantes_DebeDevolverPaginaVacia()
    {
        foreach (var estudiante in _context.Estudiantes.Where(e => e.UsuarioId != "propio"))
            estudiante.Activo = false;
        await _context.SaveChangesAsync();

        var respuesta = await CrearController().ObtenerRegistros(new PaginacionDto(), default);
        var resultado = Assert.IsType<ResultadoPaginado<RegistroEstudianteDto>>(
            Assert.IsType<OkObjectResult>(respuesta.Result).Value);

        Assert.Empty(resultado.Items);
        Assert.Equal(0, resultado.TotalRegistros);
        Assert.Equal(0, resultado.TotalPaginas);
        Assert.False(resultado.HayAnterior);
        Assert.False(resultado.HaySiguiente);
    }

    [Fact]
    public async Task PaginaMuyGrande_DebeDevolverVacioSinDesbordamiento()
    {
        var resultado = await _service.ObtenerTodosAsync(
            new PaginacionDto { Pagina = int.MaxValue, TamanoPagina = 100 });

        Assert.Empty(resultado.Items);
        Assert.Equal(13, resultado.TotalRegistros);
        Assert.Equal(1, resultado.TotalPaginas);
        Assert.False(resultado.HaySiguiente);
    }

    [Fact]
    public async Task Registros_DebeConservarRestriccionesDeAcceso()
    {
        var anonimo = await CrearController(null).ObtenerRegistros(new PaginacionDto(), default);
        Assert.IsType<UnauthorizedResult>(anonimo.Result);
        var inactivo = await CrearController("inactivo").ObtenerRegistros(new PaginacionDto(), default);
        Assert.IsType<ForbidResult>(inactivo.Result);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void ParametrosInvalidos_DebenFallarValidacion(int pagina, int tamanoPagina)
    {
        var dto = new PaginacionDto { Pagina = pagina, TamanoPagina = tamanoPagina };
        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto),
            new List<ValidationResult>(), validateAllProperties: true));
    }

    public void Dispose()
    {
        _scope.Dispose();
        _proveedor.Dispose();
    }
}
