using System.ComponentModel.DataAnnotations;

namespace StudyIA.Domain.Entities;

// Un examen que el estudiante quiere preparar (planificación de exámenes).
public class Examen
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Titulo { get; set; } = "";

    public DateTime Fecha { get; set; }

    [MaxLength(300)]
    public string? Notas { get; set; }

    public int MateriaId { get; set; }
    public Materia? Materia { get; set; }
}
