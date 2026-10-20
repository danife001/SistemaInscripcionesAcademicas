using SistemaInscripcionesAcademicas.Api.Exceptions;
using Microsoft.AspNetCore.Identity;
using SistemaInscripcionesAcademicas.Api.DTOs.Comun;
using SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;

namespace SistemaInscripcionesAcademicas.Api.Services;

public class EstudianteService : IEstudianteService
{
    private readonly IEstudianteRepository _estudiantes;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly UserManager<Usuario> _userManager;

    public EstudianteService(IEstudianteRepository estudiantes,
        IUnidadTrabajo unidadTrabajo, UserManager<Usuario> userManager)
    {
        _estudiantes = estudiantes;
        _unidadTrabajo = unidadTrabajo;
        _userManager = userManager;
    }

    public async Task<EstudianteDto> RegistrarAsync(CrearEstudianteDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Correo) != null)
            throw new ReglaNegocioException("El correo ya está registrado.");

        var usuario = new Usuario
        {
            Nombre = dto.Nombre, Email = dto.Correo, UserName = dto.Correo
        };

        await using var transaccion = await _unidadTrabajo.IniciarTransaccionAsync();
        var resultado = await _userManager.CreateAsync(usuario, dto.Password);
        if (!resultado.Succeeded)
            throw new ReglaNegocioException(string.Join(
                "; ", resultado.Errors.Select(e => e.Description)));

        var resultadoRol = await _userManager.AddToRoleAsync(usuario, "Estudiante");
        if (!resultadoRol.Succeeded)
            throw new ReglaNegocioException("No fue posible asignar el rol de estudiante: " +
                string.Join("; ", resultadoRol.Errors.Select(e => e.Description)));

        var estudiante = new Estudiante
        {
            UsuarioId = usuario.Id, Usuario = usuario, Activo = true
        };
        _estudiantes.Agregar(estudiante);
        await _unidadTrabajo.GuardarCambiosAsync();
        await transaccion.ConfirmarAsync();
        return CrearDto(estudiante);
    }

    public Task<ResultadoPaginado<EstudianteDto>> ObtenerTodosAsync(
        PaginacionDto paginacion, CancellationToken cancellationToken = default) =>
        _estudiantes.ObtenerActivosAsync(paginacion, cancellationToken);

    public async Task<EstudianteDto?> ObtenerPorIdAsync(int id)
    {
        var estudiante = await _estudiantes.ObtenerActivoPorIdAsync(id);
        return estudiante == null ? null : CrearDto(estudiante);
    }

    public async Task<EstudianteDto?> ObtenerPorUsuarioIdAsync(string usuarioId)
    {
        var estudiante = await _estudiantes.ObtenerActivoPorUsuarioIdAsync(usuarioId);
        return estudiante == null ? null : CrearDto(estudiante);
    }

    public Task<ResultadoPaginado<RegistroEstudianteDto>> ObtenerRegistrosOtrosAsync(
        string usuarioId, PaginacionDto paginacion, CancellationToken cancellationToken = default) =>
        _estudiantes.ObtenerRegistrosOtrosAsync(usuarioId, paginacion, cancellationToken);

    public async Task<EstudianteDto?> ActualizarAsync(int id, ActualizarEstudianteDto dto)
    {
        var estudiante = await _estudiantes.ObtenerActivoPorIdAsync(id);
        if (estudiante == null) return null;

        var nombre = dto.Nombre.Trim();
        var correo = dto.Correo.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaNegocioException("El nombre es obligatorio.");

        var usuario = estudiante.Usuario;
        var existente = await _userManager.FindByEmailAsync(correo);
        var existentePorNombre = await _userManager.FindByNameAsync(correo);
        if ((existente != null && existente.Id != usuario.Id) ||
            (existentePorNombre != null && existentePorNombre.Id != usuario.Id))
            throw new ReglaNegocioException("El correo ya está registrado.");

        usuario.Nombre = nombre;
        if (!string.Equals(usuario.Email, correo, StringComparison.OrdinalIgnoreCase))
            usuario.EmailConfirmed = false;
        usuario.Email = correo;
        usuario.UserName = correo;

        // Identity conserva la validación, normalización y gestión de credenciales.
        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
            throw new ReglaNegocioException(string.Join(
                "; ", resultado.Errors.Select(e => e.Description)));
        return CrearDto(estudiante);
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var estudiante = await _estudiantes.ObtenerActivoPorIdAsync(id);
        if (estudiante == null) return false;
        estudiante.Activo = false;
        await _unidadTrabajo.GuardarCambiosAsync();
        return true;
    }

    private static EstudianteDto CrearDto(Estudiante estudiante) => new()
    {
        Id = estudiante.Id,
        Nombre = estudiante.Usuario.Nombre,
        Correo = estudiante.Usuario.Email ?? string.Empty,
        Activo = estudiante.Activo
    };
}
