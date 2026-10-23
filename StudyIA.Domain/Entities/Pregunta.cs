using System.ComponentModel.DataAnnotations;

namespace StudyIA.Domain.Entities;

// Una pregunta de opción múltiple dentro de una autoevaluación.
public class Pregunta
{
    public int Id { get; set; }

    public int AutoevaluacionId { get; set; }
    public Autoevaluacion? Autoevaluacion { get; set; }

    // Tema al que pertenece la pregunta (sirve para detectar áreas débiles).
    [MaxLength(100)]
    public string Tema { get; set; } = "General";

    [Required, MaxLength(600)]
    public string Enunciado { get; set; } = "";

    [Required, MaxLength(300)]
    public string OpcionA { get; set; } = "";
    [Required, MaxLength(300)]
    public string OpcionB { get; set; } = "";
    [Required, MaxLength(300)]
    public string OpcionC { get; set; } = "";
    [Required, MaxLength(300)]
    public string OpcionD { get; set; } = "";

    // Letra de la respuesta correcta: "A", "B", "C" o "D".
    [Required, MaxLength(1)]
    public string RespuestaCorrecta { get; set; } = "A";

    [MaxLength(600)]
    public string? Explicacion { get; set; }

    // Lo que contestó el estudiante (null si no contestó).
    [MaxLength(1)]
    public string? RespuestaUsuario { get; set; }
}
