using Microsoft.AspNetCore.Identity;

namespace SistemaInscripcionesAcademicas.Api.Data;

public static class IdentityInitializer
{
    public static async Task InitializeAsync(
        RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Administrador", "Estudiante" };

        foreach (var rol in roles)
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                var resultado = await roleManager.CreateAsync(
                    new IdentityRole(rol));

                if (!resultado.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"No se pudo crear el rol {rol}");
                }
            }
        }
    }
}