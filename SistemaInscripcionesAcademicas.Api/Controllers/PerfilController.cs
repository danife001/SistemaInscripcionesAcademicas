using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;

namespace SistemaInscripcionesAcademicas.Api.Controllers;

[ApiController]
[Route("api/estudiantes/me")]
[Authorize(Roles = "Estudiante")]
public class PerfilController : ControllerBase
{
    private readonly IEstudianteService _estudianteService;

    public PerfilController(IEstudianteService estudianteService)
    {
        _estudianteService = estudianteService;
    }

    [HttpGet]
    [EndpointSummary("Consultar mi perfil")]
    public async Task<ActionResult<EstudianteDto>> Obtener()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuarioId == null)
            return Unauthorized();

        var estudiante = await _estudianteService.ObtenerPorUsuarioIdAsync(usuarioId);
        if (estudiante == null)
            return Forbid();

        return Ok(estudiante);
    }

    [HttpPut]
    [EndpointSummary("Actualizar mi nombre y correo")]
    [ProducesResponseType(typeof(EstudianteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EstudianteDto>> Actualizar(
        [FromBody] ActualizarEstudianteDto dto)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuarioId == null)
            return Unauthorized();

        var perfil = await _estudianteService.ObtenerPorUsuarioIdAsync(usuarioId);
        if (perfil == null)
            return Forbid();

        // El ID siempre se obtiene del usuario autenticado, nunca del formulario.
        var estudiante = await _estudianteService.ActualizarAsync(perfil.Id, dto);
        if (estudiante == null)
            return Forbid();

        return Ok(estudiante);
    }
}
