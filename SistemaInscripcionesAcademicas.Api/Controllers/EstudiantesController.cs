
using Microsoft.AspNetCore.Mvc;
using SistemaInscripcionesAcademicas.Api.DTOs.Estudiantes;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using SistemaInscripcionesAcademicas.Api.DTOs.Comun;

namespace SistemaInscripcionesAcademicas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class EstudiantesController : ControllerBase
{
    private readonly IEstudianteService _estudianteService;

    public EstudiantesController(
        IEstudianteService estudianteService)
    {
        _estudianteService = estudianteService;
    }

    [AllowAnonymous]
    [HttpPost]
    [EndpointSummary("Registrar un estudiante")]
    [ProducesResponseType(typeof(EstudianteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EstudianteDto>> Registrar(
        [FromBody] CrearEstudianteDto dto)
    {
        var estudiante =
            await _estudianteService.RegistrarAsync(dto);

        return StatusCode(201, estudiante);
    }


    [HttpGet]
    [EndpointSummary("Consultar estudiantes activos con paginación")]
    public async Task<ActionResult<ResultadoPaginado<EstudianteDto>>> ObtenerTodos(
        [FromQuery] PaginacionDto paginacion, CancellationToken cancellationToken)
    {
        var estudiantes = await _estudianteService.ObtenerTodosAsync(
            paginacion, cancellationToken);

        return Ok(estudiantes);
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Consultar un estudiante por ID")]
    [ProducesResponseType(typeof(EstudianteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstudianteDto>> ObtenerPorId(int id)
    {
        var estudiante = await _estudianteService.ObtenerPorIdAsync(id);

        if (estudiante == null)
        {
            return NotFound(new
            {
                mensaje = "Estudiante no encontrado."
            });
        }

        return Ok(estudiante);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Actualizar un estudiante como administrador")]
    [ProducesResponseType(typeof(EstudianteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstudianteDto>> Actualizar(
        int id, [FromBody] ActualizarEstudianteDto dto)
    {
        var estudiante = await _estudianteService.ActualizarAsync(id, dto);
        if (estudiante == null)
            return NotFound(new { mensaje = "Estudiante no encontrado o inactivo." });

        return Ok(estudiante);
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Desactivar un estudiante")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var resultado = await _estudianteService
            .DesactivarAsync(id);

        if (!resultado)
        {
            return NotFound(new
            {
                mensaje = "Estudiante no encontrado o inactivo."
            });
        }

        return NoContent();
    }
}
