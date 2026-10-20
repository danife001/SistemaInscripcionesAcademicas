
using Xunit;

namespace SistemaInscripcionesAcademicas.Tests;

public class UnitTest1
{
    [Fact]
    public void SumarDosNumeros_DebeRetornarElResultadoCorrecto()
    {
        // Arrange: preparar los datos
        int numero1 = 2;
        int numero2 = 3;

        // Act: ejecutar la operación
        int resultado = numero1 + numero2;

        // Assert: verificar el resultado
        Assert.Equal(5, resultado);
    }
}
