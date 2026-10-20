using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;
using SistemaInscripcionesAcademicas.Api.DTOs.Materias;

namespace SistemaInscripcionesAcademicas.Api.Controllers;

[ApiController]
[Route("api/materias")]
[Authorize]
public class MateriasController(IMateriaService materias) : ControllerBase
{
    // El catálogo disponible sigue siendo necesario para el flujo de inscripción del estudiante.
    [HttpGet]
    [Authorize(Roles = "Estudiante,Administrador")]
    [EndpointSummary("Consultar materias: catálogo completo para administradores y disponibles para estudiantes")]
    public async Task<ActionResult<List<MateriaDto>>> ObtenerTodas() =>
        Ok(User.IsInRole("Administrador") ? await materias.ObtenerTodasAsync() : await materias.ObtenerDisponiblesAsync());

    [Authorize(Roles = "Administrador")]
    [HttpGet("{id:int}")]
    [EndpointSummary("Consultar una materia, incluidas las desactivadas")]
    [ProducesResponseType(typeof(MateriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MateriaDto>> ObtenerPorId(int id) =>
        await materias.ObtenerPorIdAsync(id) is { } materia ? Ok(materia) : NotFound();

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [EndpointSummary("Crear una materia de tres créditos")]
    [ProducesResponseType(typeof(MateriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MateriaDto>> Crear(GuardarMateriaDto dto)
    {
        var materia = await materias.CrearAsync(dto);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = materia.Id }, materia);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:int}")]
    [EndpointSummary("Modificar una materia sin historial de inscripciones")]
    [ProducesResponseType(typeof(MateriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MateriaDto>> Actualizar(int id, GuardarMateriaDto dto)
    {
        var materia = await materias.ActualizarAsync(id, dto);
        return materia == null ? NotFound() : Ok(materia);
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:int}")]
    [EndpointSummary("Desactivar una materia sin inscripciones activas; conserva el historial")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Desactivar(int id)
    {
        return await materias.DesactivarAsync(id) ? NoContent() : NotFound();
    }

}
