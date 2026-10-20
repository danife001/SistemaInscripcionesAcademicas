using Microsoft.AspNetCore.Identity;
using SistemaInscripcionesAcademicas.Api.Models;

namespace SistemaInscripcionesAcademicas.Api.Data;

/// <summary>Crea una cuenta administrativa inicial solo cuando se habilita explícitamente.</summary>
public static class AdministradorInitializer
{
    public static async Task InitializeAsync(UserManager<Usuario> usuarios,
        ApplicationDbContext contexto, IConfiguration configuracion)
    {
        if (!configuracion.GetValue<bool>("Administrador:CrearAlIniciar")) return;
        var correo = configuracion["Administrador:Correo"]?.Trim();
        if (string.IsNullOrWhiteSpace(correo))
            throw new InvalidOperationException("Configura Administrador:Correo.");

        var existente = await usuarios.FindByEmailAsync(correo);
        if (existente != null)
        {
            // Nunca eleva una cuenta registrada públicamente ni restablece su contraseña.
            if (!await usuarios.IsInRoleAsync(existente, "Administrador"))
                throw new InvalidOperationException("El correo configurado pertenece a una cuenta no administradora.");
            return;
        }
        var clave = configuracion["Administrador:Password"];
        if (string.IsNullOrWhiteSpace(clave))
            throw new InvalidOperationException("Configura Administrador:Password para crear la cuenta.");

        // Usuario y rol se confirman juntos; un error no deja una cuenta parcial.
        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        var usuario = new Usuario { UserName = correo, Email = correo,
            Nombre = configuracion["Administrador:Nombre"]?.Trim() ?? "Administrador" };
        var creado = await usuarios.CreateAsync(usuario, clave);
        if (!creado.Succeeded)
            throw new InvalidOperationException("No se pudo crear el administrador: " +
                string.Join("; ", creado.Errors.Select(e => e.Description)));
        var rol = await usuarios.AddToRoleAsync(usuario, "Administrador");
        if (!rol.Succeeded)
            throw new InvalidOperationException("No se pudo asignar el rol Administrador.");
        await transaccion.CommitAsync();
    }
}
