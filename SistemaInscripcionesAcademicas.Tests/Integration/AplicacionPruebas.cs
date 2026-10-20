using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SistemaInscripcionesAcademicas.Api.Data;

namespace SistemaInscripcionesAcademicas.Tests.Integration;

public class AplicacionPruebas : WebApplicationFactory<Program>
{
    private readonly SqliteConnection? _conexion;
    private readonly string? _conexionSqlServer;
    private readonly bool _eliminarBaseSqlServer;
    private bool _baseSqlServerEliminada;
    private readonly string? _archivoSqlite;
    private readonly Action<IServiceCollection>? _configurar;
    private readonly string _ambiente;

    public AplicacionPruebas(Action<IServiceCollection>? configurar = null, string ambiente = "Testing",
        bool conexionesIndependientes = false, string? conexionSqlServer = null,
        bool eliminarBaseSqlServer = true)
    {
        _configurar = configurar;
        _ambiente = ambiente;
        if (conexionSqlServer != null)
        {
            var conexion = new SqlConnectionStringBuilder(conexionSqlServer);
            if (!Regex.IsMatch(conexion.InitialCatalog, "^InscripcionesConcurrency_[a-f0-9]{32}$"))
                throw new ArgumentException("La base SQL de pruebas debe ser temporal y tener un identificador único.");
            _conexionSqlServer = conexion.ConnectionString;
            _eliminarBaseSqlServer = eliminarBaseSqlServer;
            return;
        }
        if (conexionesIndependientes)
            _archivoSqlite = Path.Combine(Path.GetTempPath(), $"inscripciones-test-{Guid.NewGuid():N}.db");
        _conexion = new SqliteConnection(_archivoSqlite == null
            ? "Data Source=:memory:"
            : $"Data Source={_archivoSqlite};Default Timeout=10;Pooling=False");
        _conexion.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_ambiente);
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                if (_conexionSqlServer != null) options.UseSqlServer(_conexionSqlServer);
                else if (_archivoSqlite == null) options.UseSqlite(_conexion!);
                else options.UseSqlite(_conexion!.ConnectionString);
            });

            var opciones = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (_conexionSqlServer != null) opciones.UseSqlServer(_conexionSqlServer);
            else opciones.UseSqlite(_conexion!);
            using var contexto = new ApplicationDbContext(opciones.Options);
            contexto.Database.EnsureCreated();
            _configurar?.Invoke(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _conexion?.Dispose();
            BorrarArchivoTemporal();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_conexion != null) await _conexion.DisposeAsync();
        BorrarArchivoTemporal();
    }

    private void BorrarArchivoTemporal()
    {
        if (_conexionSqlServer != null && _eliminarBaseSqlServer && !_baseSqlServerEliminada)
        {
            var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(_conexionSqlServer).Options;
            using var contexto = new ApplicationDbContext(opciones);
            contexto.Database.EnsureDeleted();
            _baseSqlServerEliminada = true;
        }
        if (_archivoSqlite == null) return;
        foreach (var sufijo in new[] { "", "-wal", "-shm" })
            if (File.Exists(_archivoSqlite + sufijo)) File.Delete(_archivoSqlite + sufijo);
    }
}
