namespace SistemaInscripcionesAcademicas.Api.DTOs.Comun;

public class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
    public int TotalRegistros { get; init; }
    public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / TamanoPagina);
    public bool HayAnterior => TotalRegistros > 0 && Pagina > 1;
    public bool HaySiguiente => Pagina < TotalPaginas;
}
