using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace SistemaInscripcionesAcademicas.Api.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddDocumentacionApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Sistema de Inscripciones Académicas",
                    Version = "v1",
                    Description = "API de estudiantes, materias e inscripciones. " +
                        "Para probar operaciones protegidas, inicia sesión en POST /api/auth/login, " +
                        "copia accessToken y pégalo en Authorize sin el prefijo Bearer. " +
                        "El login actual solo admite estudiantes activos; las rutas administrativas requieren ese rol."
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Description = "Token de ASP.NET Core Identity. Pega únicamente accessToken; no es un JWT."
                };
                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                var autorizacion = metadata.OfType<IAuthorizeData>().ToList();
                if (metadata.OfType<IAllowAnonymous>().Any() || autorizacion.Count == 0)
                    return Task.CompletedTask;

                operation.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
                    }
                ];
                operation.Responses ??= new OpenApiResponses();
                operation.Responses.TryAdd("401", new OpenApiResponse
                {
                    Description = "Token ausente, inválido o expirado."
                });
                operation.Responses.TryAdd("403", new OpenApiResponse
                {
                    Description = "Rol insuficiente o acceso no permitido al estudiante."
                });

                var roles = autorizacion.Where(a => !string.IsNullOrWhiteSpace(a.Roles))
                    .Select(a => a.Roles).Distinct();
                var descripcionRoles = string.Join("; ", roles);
                if (descripcionRoles.Length > 0)
                    operation.Description = $"{operation.Description}\n\nRoles permitidos: {descripcionRoles}.".Trim();

                return Task.CompletedTask;
            });
        });
        return services;
    }
}
