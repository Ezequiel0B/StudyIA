using System.ComponentModel.DataAnnotations;

namespace StudyIA.Domain.Entities;

// Representa a un estudiante que usa la plataforma.
public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = "";

    // Con este nombre de usuario se inicia sesión. Es único (no se repite).
    [Required, MaxLength(30)]
    public string NombreUsuario { get; set; } = "";

    // Nunca se guarda la contraseña real, solo su "hash" (ver ContrasenaHasher).
    [Required]
    public string ContrasenaHash { get; set; } = "";

    [MaxLength(20)]
    public string Rol { get; set; } = "Estudiante";

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    // Seguridad: se cuentan los intentos fallidos y la cuenta se bloquea un rato si son muchos.
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }

    // Un usuario tiene muchas materias.
    public List<Materia> Materias { get; set; } = new();
}
