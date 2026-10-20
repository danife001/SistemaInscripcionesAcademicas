namespace SistemaInscripcionesAcademicas.Api.Models
{
    public class Profesor
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public ICollection<Materia> Materias { get; set; }
            = new List<Materia>();
    }
}
