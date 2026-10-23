using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudyIA.Domain.Entities;

namespace StudyIA.Services.IA;

// Característica de Inteligencia Artificial de StudyIA.
// - Si hay una clave configurada: llama a Azure OpenAI (o a un servicio compatible con OpenAI).
// - Si no hay clave, o si la llamada falla: usa el modo local (GeneradorLocal) para no dejar al usuario sin respuesta.
public class IaService : IIaService
{
    private const int MaxCaracteresContexto = 6000;

    private readonly HttpClient _http;
    private readonly IaOptions _opciones;
    private readonly ILogger<IaService> _logger;

    public IaService(HttpClient http, IOptions<IaOptions> opciones, ILogger<IaService> logger)
    {
        _http = http;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public bool EstaConfigurada => _opciones.Configurada;

    public string NombreMotor
    {
        get
        {
            if (!EstaConfigurada) return "Modo local (sin clave de IA)";
            return _opciones.EsAzure ? "Azure OpenAI" : "IA en la nube (" + _opciones.Modelo + ")";
        }
    }

    // ---------------------------------------------------------------------------------
    // 1) Asistente (chatbot)
    // ---------------------------------------------------------------------------------
    public async Task<RespuestaIa> ChatAsync(string pregunta, string contexto, IEnumerable<MensajeChat> historial)
    {
        if (EstaConfigurada)
        {
            try
            {
                var mensajes = new List<(string Rol, string Contenido)>
                {
                    ("system",
                        "Eres el Asistente StudyIA, un tutor para estudiantes universitarios. Responde siempre en español, " +
                        "de forma clara y breve.\n" +
                        "REGLAS:\n" +
                        "1) Responde SOLO con la información del MATERIAL DEL ESTUDIANTE.\n" +
                        "2) Si el material no contiene la respuesta, di exactamente: 'No encontré eso en tus apuntes.' " +
                        "y sugiere subir material sobre el tema. No inventes datos, cifras, nombres ni fuentes.\n" +
                        "3) Solo si el estudiante pide una explicación general, dala en máximo 3 líneas y empieza con " +
                        "'Explicación general (no está en tus apuntes):'.\n\n" +
                        "MATERIAL DEL ESTUDIANTE:\n" + Recortar(contexto, MaxCaracteresContexto))
                };

                // Se envían los últimos mensajes para que el asistente recuerde la conversación.
                foreach (var m in historial.OrderBy(h => h.Fecha).TakeLast(8))
                {
                    mensajes.Add((m.Rol == "usuario" ? "user" : "assistant", m.Texto));
                }
                mensajes.Add(("user", pregunta));

                string texto = await LlamarModeloAsync(mensajes, 0.2);
                return new RespuestaIa(texto.Trim(), true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "La IA en la nube falló en ChatAsync. Se usa el modo local.");
            }
        }

        return new RespuestaIa(GeneradorLocal.Responder(pregunta, contexto), false);
    }

    // ---------------------------------------------------------------------------------
    // 2) Resumen automático
    // ---------------------------------------------------------------------------------
    public async Task<RespuestaIa> ResumirAsync(string titulo, string contenido)
    {
        if (EstaConfigurada)
        {
            try
            {
                var mensajes = new List<(string Rol, string Contenido)>
                {
                    ("system", "Eres un asistente académico. Resume apuntes en español en 5 viñetas cortas que empiecen con '• '. " +
                        "Usa SOLO lo que dice el apunte: no agregues información nueva."),
                    ("user", "Título: " + titulo + "\n\nApunte:\n" + Recortar(contenido, MaxCaracteresContexto))
                };
                string texto = await LlamarModeloAsync(mensajes, 0.2);
                return new RespuestaIa(texto.Trim(), true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "La IA en la nube falló en ResumirAsync. Se usa el modo local.");
            }
        }

        return new RespuestaIa(GeneradorLocal.Resumir(contenido), false);
    }

    // ---------------------------------------------------------------------------------
    // 3) Autoevaluaciones adaptativas
    // ---------------------------------------------------------------------------------
    public async Task<ResultadoPreguntas> GenerarPreguntasAsync(string materia, string contexto, string nivel, int cantidad, IEnumerable<string> temasDebiles)
    {
        if (EstaConfigurada)
        {
            try
            {
                string debiles = string.Join(", ", temasDebiles);
                string instruccionDebiles = debiles == ""
                    ? ""
                    : "\nEl estudiante tiene bajo dominio en estos temas, dales prioridad: " + debiles + ".";

                var mensajes = new List<(string Rol, string Contenido)>
                {
                    ("system",
                        "Eres un generador de exámenes. Respondes SOLO con un arreglo JSON válido, sin texto adicional ni comillas invertidas. " +
                        "Cada pregunta debe poder responderse con el material dado: la opción correcta debe estar en el material y " +
                        "las otras 3 deben ser falsas pero creíbles. No repitas preguntas, reparte la letra correcta entre A, B, C y D, " +
                        "y no inventes información que no esté en el material."),
                    ("user",
                        "Materia: " + materia + "\nNivel de dificultad: " + nivel + "\n" +
                        "Genera " + cantidad + " preguntas de opción múltiple (exactamente 4 opciones) basadas ÚNICAMENTE en este material:\n" +
                        Recortar(contexto, MaxCaracteresContexto) + instruccionDebiles + "\n\n" +
                        "Formato exacto: " + FormatoJson)
                };

                // Si la IA responde algo que no se puede leer, se intenta una vez más antes de usar el modo local.
                List<PreguntaGenerada>? preguntas = null;
                for (int intento = 1; intento <= 2 && preguntas == null; intento++)
                {
                    try
                    {
                        string texto = await LlamarModeloAsync(mensajes, 0.3);
                        preguntas = ParserPreguntas.Parsear(texto);
                    }
                    catch (FormatException ex) when (intento < 2)
                    {
                        _logger.LogWarning(ex, "La IA devolvió un formato inválido. Se reintenta.");
                    }
                }
                return new ResultadoPreguntas(preguntas!.Take(cantidad).ToList(), true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "La IA en la nube falló en GenerarPreguntasAsync. Se usa el modo local.");
            }
        }

        return new ResultadoPreguntas(GeneradorLocal.GenerarPreguntas(contexto, materia, cantidad), false);
    }

    private const string FormatoJson =
        "[{\"tema\":\"nombre del tema\",\"enunciado\":\"texto de la pregunta\"," +
        "\"opciones\":[\"opción A\",\"opción B\",\"opción C\",\"opción D\"],\"correcta\":\"A\",\"explicacion\":\"por qué es correcta\"}]";

    // ---------------------------------------------------------------------------------
    // Llamada HTTP al modelo (formato "chat completions", igual en Azure OpenAI y OpenAI)
    // ---------------------------------------------------------------------------------
    private async Task<string> LlamarModeloAsync(List<(string Rol, string Contenido)> mensajes, double temperatura)
    {
        var cuerpo = new
        {
            model = _opciones.Modelo,
            messages = mensajes.Select(m => new { role = m.Rol, content = m.Contenido }).ToList(),
            temperature = temperatura
        };

        string url;
        using var peticion = new HttpRequestMessage();
        peticion.Method = HttpMethod.Post;

        if (_opciones.EsAzure)
        {
            // Azure OpenAI: la URL incluye el nombre del deployment y la clave va en el encabezado "api-key".
            url = _opciones.Endpoint.TrimEnd('/') + "/openai/deployments/" + _opciones.Modelo +
                  "/chat/completions?api-version=" + _opciones.ApiVersion;
            peticion.Headers.Add("api-key", _opciones.ApiKey);
        }
        else
        {
            // OpenAI o servicio compatible: la clave va como "Bearer".
            string baseUrl = string.IsNullOrWhiteSpace(_opciones.Endpoint) ? "https://api.openai.com/v1" : _opciones.Endpoint;
            url = baseUrl.TrimEnd('/') + "/chat/completions";
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opciones.ApiKey);
        }

        peticion.RequestUri = new Uri(url);
        peticion.Content = JsonContent.Create(cuerpo);

        using HttpResponseMessage respuesta = await _http.SendAsync(peticion);
        string json = await respuesta.Content.ReadAsStringAsync();

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("La IA respondió " + (int)respuesta.StatusCode + ": " + Recortar(json, 300));
        }

        using JsonDocument documento = JsonDocument.Parse(json);
        string? contenido = documento.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(contenido))
        {
            throw new InvalidOperationException("La IA devolvió una respuesta vacía.");
        }
        return contenido;
    }

    private static string Recortar(string texto, int maximo)
    {
        return texto.Length <= maximo ? texto : texto.Substring(0, maximo);
    }
}
