
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;
using SistemaInscripcionesAcademicas.Api.DTOs.Auth;
using SistemaInscripcionesAcademicas.Api.Models;

namespace SistemaInscripcionesAcademicas.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly SignInManager<Usuario> _signInManager;
    private readonly IEstudianteService _estudianteService;

    public AuthController(
        UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager,
        IEstudianteService estudianteService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _estudianteService = estudianteService;
    }

    [Authorize]
    [HttpGet("me")]
    [EndpointSummary("Consultar la identidad y los roles de la sesión")]
    public async Task<IActionResult> Me()
    {
        var usuario = await _userManager.GetUserAsync(User);
        if (usuario == null) return Unauthorized();
        return Ok(new { usuario.Nombre, Correo = usuario.Email,
            Roles = await _userManager.GetRolesAsync(usuario) });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EndpointSummary("Iniciar sesión como estudiante o administrador")]
    [EndpointDescription("Devuelve accessToken de Identity. Úsalo en Authorize para probar las operaciones protegidas.")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto)
    {
        // 1. Buscar el usuario por correo
        var usuario = await _userManager
            .FindByEmailAsync(dto.Correo);

        if (usuario == null)
            return Unauthorized(new
            {
                mensaje = "Credenciales inválidas."
            });

        var administrador = await _userManager.IsInRoleAsync(usuario, "Administrador");
        var estudiante = await _userManager.IsInRoleAsync(usuario, "Estudiante");
        // El administrador no necesita una ficha académica. Los estudiantes sí deben estar activos.
        if (!administrador && (!estudiante ||
            await _estudianteService.ObtenerPorUsuarioIdAsync(usuario.Id) == null))
            return Unauthorized(new { mensaje = "Credenciales inválidas." });

        // 4. Utilizar autenticación mediante Bearer Token
        _signInManager.AuthenticationScheme =
            IdentityConstants.BearerScheme;

        // 5. Validar las credenciales
        var resultado = await _signInManager
            .PasswordSignInAsync(
                usuario,
                dto.Password,
                isPersistent: false,
                lockoutOnFailure: true);

        if (!resultado.Succeeded)
            return Unauthorized(new
            {
                mensaje = "Credenciales inválidas."
            });

        // Identity ya escribió el token en la respuesta
        return new EmptyResult();
    }
}
