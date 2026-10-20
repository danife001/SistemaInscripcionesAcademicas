using Microsoft.EntityFrameworkCore;
using SistemaInscripcionesAcademicas.Api.DTOs.Comun;

namespace SistemaInscripcionesAcademicas.Api.Extensions;

public static class PaginacionExtensions
{
    // La consulta debe tener un orden estable antes de paginar.
    public static async Task<ResultadoPaginado<T>> PaginarAsync<T>(
        this IQueryable<T> consulta,
        PaginacionDto paginacion,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(paginacion.Pagina, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(paginacion.TamanoPagina, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(paginacion.TamanoPagina, 100);

        var total = await consulta.CountAsync(cancellationToken);
        var desplazamiento = ((long)paginacion.Pagina - 1) * paginacion.TamanoPagina;
        var items = desplazamiento >= total
            ? new List<T>()
            : await consulta.Skip((int)desplazamiento)
                .Take(paginacion.TamanoPagina)
                .ToListAsync(cancellationToken);

        return new ResultadoPaginado<T>
        {
            Items = items,
            Pagina = paginacion.Pagina,
            TamanoPagina = paginacion.TamanoPagina,
            TotalRegistros = total
        };
    }
}
