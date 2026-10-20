namespace SistemaInscripcionesAcademicas.Api.Exceptions;

/// <summary>Error esperado cuyo mensaje es seguro para mostrar al cliente.</summary>
public sealed class ReglaNegocioException(string mensaje, bool conflicto = false) : Exception(mensaje)
{
    public bool Conflicto { get; } = conflicto;
}
