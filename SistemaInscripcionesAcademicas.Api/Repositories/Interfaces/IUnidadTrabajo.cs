namespace SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;

public interface IUnidadTrabajo
{
    Task<ITransaccionDatos> IniciarTransaccionCatalogoAsync(CancellationToken cancellationToken = default);
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
    Task<ITransaccionDatos> IniciarTransaccionAsync(CancellationToken cancellationToken = default);
    Task<ITransaccionDatos> IniciarTransaccionInscripcionesAsync(
        int estudianteId, CancellationToken cancellationToken = default);
}

public interface ITransaccionDatos : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken cancellationToken = default);
}
