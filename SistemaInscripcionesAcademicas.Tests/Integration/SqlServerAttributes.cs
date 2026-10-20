namespace SistemaInscripcionesAcademicas.Tests.Integration;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION")))
            Skip = "Define SQLSERVER_TEST_CONNECTION para ejecutar contra SQL Server real.";
    }
}

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION")))
            Skip = "Define SQLSERVER_TEST_CONNECTION para ejecutar contra SQL Server real.";
    }
}
