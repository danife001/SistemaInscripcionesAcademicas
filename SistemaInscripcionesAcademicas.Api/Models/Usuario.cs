using Microsoft.AspNetCore.Identity;

namespace SistemaInscripcionesAcademicas.Api.Models;

public class Usuario : IdentityUser
{
    public string Nombre { get; set; } = string.Empty;

    public Estudiante? Estudiante { get; set; }
}