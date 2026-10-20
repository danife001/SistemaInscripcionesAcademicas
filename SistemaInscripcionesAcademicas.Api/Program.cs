using SistemaInscripcionesAcademicas.Api.Exceptions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Services;
using SistemaInscripcionesAcademicas.Api.Services.Interfaces;
using SistemaInscripcionesAcademicas.Api.Extensions;
using Microsoft.AspNetCore.DataProtection;


var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddArchivoPersistente(builder.Configuration, builder.Environment.ContentRootPath);

// Controladores
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddProblemDetails();

// OpenAPI
builder.Services.AddDocumentacionApi();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

var rutaClaves = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(rutaClaves))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("SistemaInscripcionesAcademicas")
        .PersistKeysToFileSystem(Directory.CreateDirectory(rutaClaves));
}

// Conexión con SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Configuración de ASP.NET Core Identity
builder.Services.AddIdentityApiEndpoints<Usuario>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Autorización
builder.Services.AddAuthorization();
builder.Services.AddPersistenciaAcademica();
builder.Services.AddScoped<IMateriaService, MateriaService>();
builder.Services.AddScoped<IEstudianteService, EstudianteService>();
builder.Services.AddScoped<
    IInscripcionService,
    InscripcionService>();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var contexto = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (builder.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
        await contexto.Database.MigrateAsync();

    if (builder.Configuration.GetValue<bool>("Database:SeedCatalogOnStartup"))
        await CatalogoInitializer.InitializeAsync(contexto);

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    await IdentityInitializer.InitializeAsync(roleManager);
    await AdministradorInitializer.InitializeAsync(
        scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>(),
        contexto, builder.Configuration);
}
// Antes del resto del pipeline: el mismo contrato de errores en todos los entornos.
app.UseExceptionHandler();

// OpenAPI en desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "API de Inscripciones Académicas";
        options.SwaggerEndpoint("/openapi/v1.json", "Inscripciones Académicas v1");
        options.EnableTryItOutByDefault();
        options.DisplayRequestDuration();
    });
}

if (builder.Configuration.GetValue("HttpsRedirection:Enabled", true))
    app.UseHttpsRedirection();

// Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Endpoints de los controladores
app.MapControllers();
app.MapHealthChecks("/health");

// Endpoints de Identity
//app.MapGroup("/api/auth").MapIdentityApi<Usuario>();

app.Logger.LogInformation("API de inscripciones preparada para iniciar.");
app.Run();

// Permite iniciar la aplicación real desde las pruebas HTTP.
public partial class Program { }
