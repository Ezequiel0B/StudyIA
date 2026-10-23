using System.ComponentModel.DataAnnotations;

namespace StudyIA.Domain.Entities;

// Apuntes o guías que el estudiante sube para una materia.
// La IA usa este texto para responder, resumir y generar preguntas.
public class Material
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Titulo { get; set; } = "";

    // Texto completo del apunte.
    [Required]
    public string Contenido { get; set; } = "";

    // Resumen generado por la IA (puede estar vacío hasta que se pida).
    public string? Resumen { get; set; }

    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

    public int MateriaId { get; set; }
    public Materia? Materia { get; set; }
}
