using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.Exceptions;
using SistemaInscripcionesAcademicas.Api.Repositories.Interfaces;

namespace SistemaInscripcionesAcademicas.Api.Repositories;

public class UnidadTrabajo : IUnidadTrabajo
{
    private readonly ApplicationDbContext _context;

    public UnidadTrabajo(ApplicationDbContext context) => _context = context;

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<ITransaccionDatos> IniciarTransaccionAsync(
        CancellationToken cancellationToken = default) =>
        new TransaccionDatos(await _context.Database.BeginTransactionAsync(cancellationToken));

    public Task<ITransaccionDatos> IniciarTransaccionCatalogoAsync(CancellationToken cancellationToken = default) =>
        IniciarTransaccionProtegidaAsync(null, cancellationToken);

    public Task<ITransaccionDatos> IniciarTransaccionInscripcionesAsync(
        int estudianteId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(estudianteId);
        return IniciarTransaccionProtegidaAsync(estudianteId, cancellationToken);
    }

    private async Task<ITransaccionDatos> IniciarTransaccionProtegidaAsync(
        int? estudianteId, CancellationToken cancellationToken)
    {
        if (_context.Database.IsSqlServer())
        {
            var transaccion = await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            try
            {
                // Orden único: catálogo primero, estudiante después. Los alumnos comparten
                // el bloqueo del catálogo; una edición administrativa necesita exclusividad.
                var modoCatalogo = estudianteId.HasValue ? "Shared" : "Exclusive";
                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    DECLARE @resultado int;
                    EXEC @resultado = sys.sp_getapplock
                        @Resource = N'Inscripciones:Catalogo', @LockMode = {modoCatalogo},
                        @LockOwner = N'Transaction', @LockTimeout = 10000;
                    IF @resultado < 0 THROW 51001, 'No se pudo bloquear el catálogo.', 1;
                    """, cancellationToken);
                if (!estudianteId.HasValue) return new TransaccionDatos(transaccion);
                // El recurso pertenece a esta base y estudiante. SQL Server libera el
                // bloqueo al confirmar o revertir, también entre instancias de la API.
                var recurso = $"Inscripciones:Estudiante:{estudianteId}";
                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    DECLARE @resultado int;
                    EXEC @resultado = sys.sp_getapplock
                        @Resource = {recurso},
                        @LockMode = N'Exclusive',
                        @LockOwner = N'Transaction',
                        @LockTimeout = 10000;
                    IF @resultado < 0
                        THROW 51001, 'No se pudo obtener el bloqueo de inscripciones.', 1;
                    """, cancellationToken);
                return new TransaccionDatos(transaccion);
            }
            catch (SqlException ex) when (ex.Number == 51001)
            {
                await transaccion.DisposeAsync();
                throw new ConflictoInscripcionException(ex, estudianteId.HasValue ? null :
                    "El catálogo está ocupado por otra operación. Intenta nuevamente.");
            }
            catch
            {
                await transaccion.DisposeAsync();
                throw;
            }
        }

        if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQLite reserva la escritura al iniciar esta transacción no diferida.
            // Se utiliza en las pruebas; no simula un bloqueo solo en memoria.
            return new TransaccionDatos(await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken));
        }

        throw new NotSupportedException(
            "Las inscripciones concurrentes requieren SQL Server o SQLite.");
    }

    private sealed class TransaccionDatos(IDbContextTransaction transaccion) : ITransaccionDatos
    {
        public Task ConfirmarAsync(CancellationToken cancellationToken = default) =>
            transaccion.CommitAsync(cancellationToken);

        // EF revierte una transacción que se desecha sin confirmar.
        public ValueTask DisposeAsync() => transaccion.DisposeAsync();
    }
}
