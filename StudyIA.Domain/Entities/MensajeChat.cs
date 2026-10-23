using System.ComponentModel.DataAnnotations;

namespace StudyIA.Domain.Entities;

// Un mensaje de la conversación con el Asistente StudyIA.
public class MensajeChat
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    // Materia sobre la que se conversa (opcional).
    public int? MateriaId { get; set; }

    // "usuario" o "asistente".
    [Required, MaxLength(15)]
    public string Rol { get; set; } = "usuario";

    [Required]
    public string Texto { get; set; } = "";

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // true si la respuesta la generó la IA en la nube.
    public bool GeneradoConIa { get; set; }
}
