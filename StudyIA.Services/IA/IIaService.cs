using StudyIA.Domain.Entities;

namespace StudyIA.Services.IA;

// Respuesta de la IA. EsReal = true si vino del servicio en la nube, false si fue el modo local.
public record RespuestaIa(string Texto, bool EsReal);

// Una pregunta generada (todavía no guardada en la base de datos).
public class PreguntaGenerada
{
    public string Tema { get; set; } = "General";
    public string Enunciado { get; set; } = "";
    public string[] Opciones { get; set; } = new string[4];
    public string Correcta { get; set; } = "A";
    public string Explicacion { get; set; } = "";
}

public record ResultadoPreguntas(List<PreguntaGenerada> Preguntas, bool EsReal);

// Contrato de la característica de Inteligencia Artificial de StudyIA.
public interface IIaService
{
    // true si hay una clave de IA configurada (modo nube).
    bool EstaConfigurada { get; }

    // Texto para mostrar en pantalla: qué motor está respondiendo.
    string NombreMotor { get; }

    // Asistente: responde una pregunta usando el material del estudiante como contexto.
    Task<RespuestaIa> ChatAsync(string pregunta, string contexto, IEnumerable<MensajeChat> historial);

    // Resumen automático de un apunte.
    Task<RespuestaIa> ResumirAsync(string titulo, string contenido);

    // Genera preguntas de opción múltiple a partir del material.
    Task<ResultadoPreguntas> GenerarPreguntasAsync(string materia, string contexto, string nivel, int cantidad, IEnumerable<string> temasDebiles);
}
