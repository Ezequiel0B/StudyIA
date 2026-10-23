using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyIA.Domain.Entities;

// Un examen de práctica generado para una materia.
public class Autoevaluacion
{
    public int Id { get; set; }

    public int MateriaId { get; set; }
    public Materia? Materia { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // Basico, Intermedio o Avanzado.
    [MaxLength(20)]
    public string Nivel { get; set; } = "Intermedio";

    // Indica si el estudiante ya respondió y se calificó.
    public bool Finalizada { get; set; }

    public int Correctas { get; set; }
    public int Total { get; set; }

    // true si las preguntas las generó la IA en la nube, false si fue el modo local.
    public bool GeneradaConIa { get; set; }

    public List<Pregunta> Preguntas { get; set; } = new();

    // Esta propiedad se calcula, no se guarda en la base de datos.
    [NotMapped]
    public int Porcentaje => Total == 0 ? 0 : (int)Math.Round(Correctas * 100.0 / Total);
}
