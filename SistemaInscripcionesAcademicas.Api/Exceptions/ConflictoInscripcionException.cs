namespace SistemaInscripcionesAcademicas.Api.Exceptions;

public class ConflictoInscripcionException : Exception
{
    public ConflictoInscripcionException(Exception? innerException = null, string? mensaje = null)
        : base(mensaje ?? "Hay otra operación de inscripción en curso. Intenta nuevamente.", innerException) { }
}
