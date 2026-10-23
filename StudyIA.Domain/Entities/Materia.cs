using System.ComponentModel.DataAnnotations;

namespace StudyIA.Domain.Entities;

// Una materia del estudiante (por ejemplo "Base de Datos").
public class Materia
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = "";

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    // Llave foránea: a qué usuario pertenece esta materia.
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    // Una materia tiene muchos materiales de estudio y muchas autoevaluaciones.
    public List<Material> Materiales { get; set; } = new();
    public List<Autoevaluacion> Autoevaluaciones { get; set; } = new();

    // También puede tener exámenes planificados y sesiones de estudio registradas.
    public List<Examen> Examenes { get; set; } = new();
    public List<SesionEstudio> Sesiones { get; set; } = new();
}
