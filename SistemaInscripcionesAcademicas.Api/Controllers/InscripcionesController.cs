
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaInscripcionesAcademicas.Api.DTOs.Inscripciones;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;

namespace SistemaInscripcionesAcademicas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Estudiante")]
public class InscripcionesController : ControllerBase
{
    private readonly IInscripcionService _inscripcionService;
    private readonly IEstudianteService _estudianteService;
    private readonly UserManager<Usuario> _userManager;

    public InscripcionesController(
        IInscripcionService inscripcionService,
        IEstudianteService estudianteService,
        UserManager<Usuario> userManager)
    {
        _inscripcionService = inscripcionService;
        _estudianteService = estudianteService;
        _userManager = userManager;
    }

    [HttpPost]
    [EndpointSummary("Inscribir una materia")]
    [EndpointDescription("Máximo tres materias activas; no se permite duplicar materia ni profesor.")]
    [ProducesResponseType(typeof(InscripcionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InscripcionDto>> Inscribir(
        [FromBody] CrearInscripcionDto dto)
    {
        var usuarioId = _userManager.GetUserId(User);

        if (usuarioId == null)
            return Unauthorized();

        var estudiante = await _estudianteService.ObtenerPorUsuarioIdAsync(usuarioId);
        var estudianteId = estudiante?.Id;

        if (estudianteId == null)
            return Forbid();

        var resultado = await _inscripcionService
            .InscribirAsync(estudianteId.Value, dto);

        return StatusCode(201, resultado);
    }

    [HttpGet]
    [EndpointSummary("Consultar mis inscripciones activas")]
    public async Task<ActionResult<List<InscripcionDto>>> ObtenerMisInscripciones()
    {
        // Obtener ID del usuario autenticado
        var usuarioId = _userManager.GetUserId(User);

        if (usuarioId == null)
            return Unauthorized();

        // Buscar el estudiante relacionado y verificar su estado
        var estudiante = await _estudianteService.ObtenerPorUsuarioIdAsync(usuarioId);
        var estudianteId = estudiante?.Id;

        if (estudianteId == null)
            return Forbid();

        // Obtener únicamente sus inscripciones
        var inscripciones = await _inscripcionService
            .ObtenerMisInscripcionesAsync(estudianteId.Value);

        return Ok(inscripciones);
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Cancelar una inscripción propia")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancelar(int id)
    {
        var usuarioId = _userManager.GetUserId(User);

        if (usuarioId == null)
            return Unauthorized();

        var estudiante = await _estudianteService.ObtenerPorUsuarioIdAsync(usuarioId);
        var estudianteId = estudiante?.Id;

        if (estudianteId == null)
            return Forbid();

        var resultado = await _inscripcionService.CancelarAsync(estudianteId.Value, id);
        if (!resultado)
        {
            return NotFound(new
            {
                mensaje = "Inscripción no encontrada o inactiva."
            });
        }

        return NoContent();
    }

    [HttpGet("materias/{materiaId:int}/compas")]
    [EndpointSummary("Consultar los nombres de mis compañeros de clase")]
    [EndpointDescription("Solo permite consultar una materia en la que el estudiante está inscrito.")]
    public async Task<ActionResult<List<CompaDto>>> ObtenerCompas(
    int materiaId)
    {
        var usuarioId = _userManager.GetUserId(User);

        if (usuarioId == null)
            return Unauthorized();

        var estudiante = await _estudianteService.ObtenerPorUsuarioIdAsync(usuarioId);
        var estudianteId = estudiante?.Id;

        if (estudianteId == null)
            return Forbid();

        var compas = await _inscripcionService
            .ObtenerCompasAsync(estudianteId.Value, materiaId);

        if (compas == null)
            return Forbid();

        return Ok(compas);
    }
}


