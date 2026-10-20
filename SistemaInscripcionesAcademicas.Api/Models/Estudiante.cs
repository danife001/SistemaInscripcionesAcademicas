namespace SistemaInscripcionesAcademicas.Api.Models
{
    public class Estudiante
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public string UsuarioId { get; set; } = string.Empty;

        public Usuario Usuario { get; set; } = null!;

        public ICollection<Inscripcion> Inscripciones { get; set; }
            = new List<Inscripcion>();
    }
}
