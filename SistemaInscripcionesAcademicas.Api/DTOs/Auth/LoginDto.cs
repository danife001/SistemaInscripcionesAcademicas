using System.ComponentModel.DataAnnotations;

namespace SistemaInscripcionesAcademicas.Api.DTOs.Auth;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Correo { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}