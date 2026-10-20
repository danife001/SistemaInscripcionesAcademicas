
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;
using SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.DTOs.Comun;

namespace SistemaInscripcionesAcademicas.Api.Controllers;

[ApiController]
[Route("api/registros")]
[Authorize(Roles = "Estudiante")]
public class RegistrosController : ControllerBase
{
    private readonly IEstudianteService _estudianteService;
    private readonly UserManager<Usuario> _userManager;

    public RegistrosController(
        IEstudianteService estudianteService,
        UserManager<Usuario> userManager)
    {
        _estudianteService = estudianteService;
        _userManager = userManager;
    }

    [HttpGet("estudiantes")]
    [EndpointSummary("Consultar los registros de otros estudiantes con paginación")]
    [ProducesResponseType(typeof(ResultadoPaginado<RegistroEstudianteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultadoPaginado<RegistroEstudianteDto>>>
        ObtenerRegistros(
            [FromQuery] PaginacionDto paginacion, CancellationToken cancellationToken)
    {
        var usuarioId = _userManager.GetUserId(User);

        if (usuarioId == null)
            return Unauthorized();

        var estudianteActivo = await _estudianteService.ObtenerPorUsuarioIdAsync(usuarioId);

        if (estudianteActivo == null)
            return Forbid();

        var registros = await _estudianteService.ObtenerRegistrosOtrosAsync(
            usuarioId, paginacion, cancellationToken);

        return Ok(registros);
    }
}
