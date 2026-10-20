namespace SistemaInscripcionesAcademicas.Api.Models
{
    public class Inscripcion
    {
        public int Id { get; set; }

        public int EstudianteId { get; set; }

        public Estudiante Estudiante { get; set; } = null!;

        public int MateriaId { get; set; }

        public Materia Materia { get; set; } = null!;

        public DateTime FechaInscripcion { get; set; }
            = DateTime.UtcNow;

        public bool Activa { get; set; } = true;
    }
}
