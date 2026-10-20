namespace SistemaInscripcionesAcademicas.Api.Models
{
   
    public class Materia
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public int Creditos { get; set; } = 3;

        public bool Activo { get; set; } = true;

        public int ProfesorId { get; set; }

        public Profesor Profesor { get; set; } = null!;

        public ICollection<Inscripcion> Inscripciones { get; set; }
            = new List<Inscripcion>();
    }
}
