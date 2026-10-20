using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaInscripcionesAcademicas.Api.DTOs.Materias;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;

namespace SistemaInscripcionesAcademicas.Api.Controllers;

[ApiController]
[Route("api/profesores")]
[Authorize(Roles = "Administrador")]
public class ProfesoresController(IMateriaService materias) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Consultar profesores activos para asignarlos a materias")]
    public async Task<ActionResult<List<ProfesorDisponibleDto>>> ObtenerActivos() =>
        Ok(await materias.ObtenerProfesoresActivosAsync());
}
