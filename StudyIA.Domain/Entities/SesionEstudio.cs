using System.ComponentModel.DataAnnotations;

namespace StudyIA.Domain.Entities;

// Un rato de estudio que el estudiante registra (seguimiento del estudio).
public class SesionEstudio
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // Cuántos minutos estudió.
    public int Minutos { get; set; }

    [MaxLength(200)]
    public string? Tema { get; set; }

    public int MateriaId { get; set; }
    public Materia? Materia { get; set; }
}
