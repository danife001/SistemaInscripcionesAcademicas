using SistemaInscripcionesAcademicas.Api.Exceptions;
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
using SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Services;

namespace SistemaInscripcionesAcademicas.Tests;

public class EstudianteActualizacionTests
{
    private static ServiceProvider CrearServicios()
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddDbContext<ApplicationDbContext>(opciones =>
            opciones.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        servicios.AddIdentityCore<Usuario>(opciones =>
            opciones.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<ApplicationDbContext>();
        servicios.AddPersistenciaAcademica();
        servicios.AddScoped<EstudianteService>();
        servicios.AddScoped<IEstudianteService>(sp => sp.GetRequiredService<EstudianteService>());
        return servicios.BuildServiceProvider();
    }

    private static async Task<Estudiante> CrearEstudianteAsync(
        IServiceProvider servicios, string correo, bool activo = true)
    {
        var manager = servicios.GetRequiredService<UserManager<Usuario>>();
        var usuario = new Usuario
        {
            Nombre = "Nombre original",
            Email = correo,
            UserName = correo,
            EmailConfirmed = true
        };
        var resultado = await manager.CreateAsync(usuario, "Prueba123!");
        Assert.True(resultado.Succeeded);
        var contexto = servicios.GetRequiredService<ApplicationDbContext>();
        var estudiante = new Estudiante { Usuario = usuario, Activo = activo };
        contexto.Estudiantes.Add(estudiante);
        await contexto.SaveChangesAsync();
        return estudiante;
    }

    [Fact]
    public async Task Actualizar_DebeGuardarDatosYConservarPassword()
    {
        await using var proveedor = CrearServicios();
        using var scope = proveedor.CreateScope();
        var servicios = scope.ServiceProvider;
        var estudiante = await CrearEstudianteAsync(servicios, "original@prueba.com");
        var service = servicios.GetRequiredService<EstudianteService>();

        var resultado = await service.ActualizarAsync(estudiante.Id,
            new ActualizarEstudianteDto { Nombre = "  Ana Pérez  ", Correo = "nuevo@prueba.com" });

        Assert.NotNull(resultado);
        Assert.Equal("Ana Pérez", resultado.Nombre);
        Assert.Equal("nuevo@prueba.com", resultado.Correo);

        servicios.GetRequiredService<ApplicationDbContext>().ChangeTracker.Clear();
        var manager = servicios.GetRequiredService<UserManager<Usuario>>();
        var usuario = await manager.FindByEmailAsync("nuevo@prueba.com");
        Assert.NotNull(usuario);
        Assert.Equal("Ana Pérez", usuario.Nombre);
        Assert.Equal("nuevo@prueba.com", usuario.UserName);
        Assert.Equal("NUEVO@PRUEBA.COM", usuario.NormalizedEmail);
        Assert.False(usuario.EmailConfirmed);
        Assert.Null(await manager.FindByEmailAsync("original@prueba.com"));
        Assert.True(await manager.CheckPasswordAsync(usuario, "Prueba123!"));
    }

    [Fact]
    public async Task Actualizar_CorreoDuplicado_DebeRechazarSinCambiarDatos()
    {
        await using var proveedor = CrearServicios();
        using var scope = proveedor.CreateScope();
        var servicios = scope.ServiceProvider;
        var estudiante = await CrearEstudianteAsync(servicios, "original@prueba.com");
        await CrearEstudianteAsync(servicios, "ocupado@prueba.com");
        var service = servicios.GetRequiredService<EstudianteService>();

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.ActualizarAsync(estudiante.Id,
                new ActualizarEstudianteDto { Nombre = "Cambio", Correo = "OCUPADO@prueba.com" }));

        Assert.Equal("El correo ya está registrado.", error.Message);
        servicios.GetRequiredService<ApplicationDbContext>().ChangeTracker.Clear();
        var perfil = await service.ObtenerPorIdAsync(estudiante.Id);
        Assert.NotNull(perfil);
        Assert.Equal("Nombre original", perfil.Nombre);
        Assert.Equal("original@prueba.com", perfil.Correo);
    }

    [Fact]
    public async Task Actualizar_EstudianteInactivoOInexistente_DebeRetornarNull()
    {
        await using var proveedor = CrearServicios();
        using var scope = proveedor.CreateScope();
        var servicios = scope.ServiceProvider;
        var estudiante = await CrearEstudianteAsync(servicios, "inactivo@prueba.com", false);
        var service = servicios.GetRequiredService<EstudianteService>();
        var dto = new ActualizarEstudianteDto { Nombre = "Cambio", Correo = "nuevo@prueba.com" };

        Assert.Null(await service.ActualizarAsync(estudiante.Id, dto));
        Assert.Null(await service.ActualizarAsync(int.MaxValue, dto));
        Assert.Null(await service.ObtenerPorUsuarioIdAsync(estudiante.UsuarioId));
    }

    [Fact]
    public async Task ActualizarPerfil_DebeModificarSoloAlUsuarioAutenticado()
    {
        await using var proveedor = CrearServicios();
        using var scope = proveedor.CreateScope();
        var servicios = scope.ServiceProvider;
        var propio = await CrearEstudianteAsync(servicios, "propio@prueba.com");
        var ajeno = await CrearEstudianteAsync(servicios, "ajeno@prueba.com");
        var service = servicios.GetRequiredService<EstudianteService>();
        var controller = new PerfilController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, propio.UsuarioId) }, "Test"))
                }
            }
        };

        var respuesta = await controller.Actualizar(new ActualizarEstudianteDto
        {
            Nombre = "Mi nuevo nombre", Correo = "nuevo@prueba.com"
        });

        var ok = Assert.IsType<OkObjectResult>(respuesta.Result);
        Assert.Equal(propio.Id, Assert.IsType<EstudianteDto>(ok.Value).Id);
        var perfilAjeno = await service.ObtenerPorIdAsync(ajeno.Id);
        Assert.NotNull(perfilAjeno);
        Assert.Equal("Nombre original", perfilAjeno.Nombre);
        Assert.Equal("ajeno@prueba.com", perfilAjeno.Correo);
    }

    [Fact]
    public async Task ActualizarPerfil_SinIdentidad_DebeRechazarSolicitud()
    {
        await using var proveedor = CrearServicios();
        using var scope = proveedor.CreateScope();
        var controller = new PerfilController(scope.ServiceProvider.GetRequiredService<EstudianteService>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var resultado = await controller.Actualizar(new ActualizarEstudianteDto
        {
            Nombre = "Cambio", Correo = "nuevo@prueba.com"
        });

        Assert.IsType<UnauthorizedResult>(resultado.Result);
    }

    [Theory]
    [InlineData("", "valido@prueba.com")]
    [InlineData("   ", "valido@prueba.com")]
    [InlineData("Ana", "correo-invalido")]
    public void Actualizar_DatosInvalidos_DebeFallarValidacion(string nombre, string correo)
    {
        var dto = new ActualizarEstudianteDto { Nombre = nombre, Correo = correo };
        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto),
            new List<ValidationResult>(), validateAllProperties: true));
    }
}
