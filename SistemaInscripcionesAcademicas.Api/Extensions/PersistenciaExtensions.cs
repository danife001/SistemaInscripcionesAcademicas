using SistemaInscripcionesAcademicas.Api.Repositories;
using SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;

namespace SistemaInscripcionesAcademicas.Api.Extensions;

public static class PersistenciaExtensions
{
    public static IServiceCollection AddPersistenciaAcademica(this IServiceCollection services)
    {
        services.AddScoped<IEstudianteRepository, EstudianteRepository>();
        services.AddScoped<IMateriaRepository, MateriaRepository>();
        services.AddScoped<IInscripcionRepository, InscripcionRepository>();
        services.AddScoped<IUnidadTrabajo, UnidadTrabajo>();
        return services;
    }
}
