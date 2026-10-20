namespace SistemaInscripcionesAcademicas.Api.DTOs.Auth;

// Contrato de la respuesta generada por el manejador Bearer de Identity.
public class TokenDto
{
    public string TokenType { get; set; } = "Bearer";
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}
