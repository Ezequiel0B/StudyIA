using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudyIA.Domain.Entities;
using StudyIA.Services.IA;
using Xunit;

namespace StudyIA.Tests;

// Pruebas de la integración con IA. No se llama a internet: se usa un "servidor falso"
// que guarda la petición recibida y devuelve la respuesta que le indiquemos.
public class IaServiceTests
{
    private class ServidorFalso : HttpMessageHandler
    {
        public HttpRequestMessage? Peticion { get; private set; }
        public string? Cuerpo { get; private set; }
        public HttpStatusCode Codigo { get; set; } = HttpStatusCode.OK;
        public string Respuesta { get; set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Peticion = request;
            Cuerpo = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Codigo) { Content = new StringContent(Respuesta, Encoding.UTF8, "application/json") };
        }
    }

    // Respuesta con el formato real de "chat completions".
    private static string RespuestaChat(string contenido)
    {
        return JsonSerializer.Serialize(new { choices = new[] { new { message = new { content = contenido } } } });
    }

    private static IaService CrearServicio(ServidorFalso servidor, IaOptions opciones)
    {
        var http = new HttpClient(servidor);
        return new IaService(http, Options.Create(opciones), NullLogger<IaService>.Instance);
    }

    private const string Material = "La normalización organiza las tablas de una base de datos para reducir la redundancia de datos.";

    [Fact]
    public async Task SinClave_UsaElModoLocalYNoLlamaAInternet()
    {
        var servidor = new ServidorFalso();
        var servicio = CrearServicio(servidor, new IaOptions { ApiKey = "" });

        var respuesta = await servicio.ChatAsync("¿Qué es la normalización?", Material, new List<MensajeChat>());

        Assert.False(respuesta.EsReal);
        Assert.False(servicio.EstaConfigurada);
        Assert.True(servidor.Peticion == null);
    }

    [Fact]
    public async Task ConAzureOpenAI_ArmaBienLaPeticionYDevuelveLaRespuestaDeLaIa()
    {
        var servidor = new ServidorFalso { Respuesta = RespuestaChat("Respuesta generada por la IA") };
        var opciones = new IaOptions
        {
            Proveedor = "AzureOpenAI",
            Endpoint = "https://mi-recurso.openai.azure.com/",
            ApiKey = "clave-secreta",
            Modelo = "gpt-demo",
            ApiVersion = "2024-06-01"
        };
        var servicio = CrearServicio(servidor, opciones);

        var respuesta = await servicio.ChatAsync("¿Qué es la normalización?", Material, new List<MensajeChat>());

        Assert.True(respuesta.EsReal);
        Assert.Equal("Respuesta generada por la IA", respuesta.Texto);

        string url = servidor.Peticion!.RequestUri!.ToString();
        Assert.Contains("https://mi-recurso.openai.azure.com/openai/deployments/gpt-demo/chat/completions", url);
        Assert.Contains("api-version=2024-06-01", url);
        Assert.True(servidor.Peticion.Headers.Contains("api-key"));
        Assert.Contains("¿Qué es la normalización?", servidor.Cuerpo!.Replace("\\u00BF", "¿").Replace("\\u00E9", "é").Replace("\\u00F3", "ó"));
    }

    [Fact]
    public async Task ConOpenAI_UsaAutenticacionBearer()
    {
        var servidor = new ServidorFalso { Respuesta = RespuestaChat("ok") };
        var opciones = new IaOptions { Proveedor = "OpenAI", ApiKey = "sk-prueba", Modelo = "gpt-4o-mini" };
        var servicio = CrearServicio(servidor, opciones);

        await servicio.ResumirAsync("Título", Material);

        Assert.Equal("Bearer", servidor.Peticion!.Headers.Authorization!.Scheme);
        Assert.Equal("sk-prueba", servidor.Peticion.Headers.Authorization!.Parameter);
        Assert.Equal("https://api.openai.com/v1/chat/completions", servidor.Peticion.RequestUri!.ToString());
    }

    [Fact]
    public async Task SiLaIaFalla_SeUsaElModoLocalEnVezDeRomperLaAplicacion()
    {
        var servidor = new ServidorFalso { Codigo = HttpStatusCode.InternalServerError, Respuesta = "{\"error\":\"fallo\"}" };
        var opciones = new IaOptions { Proveedor = "OpenAI", ApiKey = "sk-prueba", Modelo = "gpt-4o-mini" };
        var servicio = CrearServicio(servidor, opciones);

        var respuesta = await servicio.ChatAsync("¿Qué es la normalización?", Material, new List<MensajeChat>());

        Assert.False(respuesta.EsReal);
        Assert.Contains("Según tu material", respuesta.Texto);
    }

    [Fact]
    public async Task GenerarPreguntas_ConRespuestaJsonDeLaIa_DevuelvePreguntasReales()
    {
        string json = "[{\"tema\":\"Normalización\",\"enunciado\":\"¿Qué reduce la normalización?\",\"opciones\":[\"La redundancia\",\"Las tablas\",\"Los índices\",\"Los usuarios\"],\"correcta\":\"A\",\"explicacion\":\"Reduce la redundancia.\"}]";
        var servidor = new ServidorFalso { Respuesta = RespuestaChat(json) };
        var opciones = new IaOptions { Proveedor = "OpenAI", ApiKey = "sk-prueba", Modelo = "gpt-4o-mini" };
        var servicio = CrearServicio(servidor, opciones);

        var resultado = await servicio.GenerarPreguntasAsync("Base de Datos", Material, "Intermedio", 5, new List<string> { "Normalización" });

        Assert.True(resultado.EsReal);
        var pregunta = Assert.Single(resultado.Preguntas);
        Assert.Equal("Normalización", pregunta.Tema);
        Assert.Equal("A", pregunta.Correcta);
    }

    [Fact]
    public async Task GenerarPreguntas_SiLaIaDevuelveBasura_CaeAlModoLocal()
    {
        var servidor = new ServidorFalso { Respuesta = RespuestaChat("Lo siento, no puedo generar eso.") };
        var opciones = new IaOptions { Proveedor = "OpenAI", ApiKey = "sk-prueba", Modelo = "gpt-4o-mini" };
        var servicio = CrearServicio(servidor, opciones);

        var texto = "La normalización es el proceso de organizar las tablas de una base de datos para reducir la redundancia. " +
                    "La tercera forma normal elimina las dependencias transitivas entre los atributos de la tabla.";
        var resultado = await servicio.GenerarPreguntasAsync("Base de Datos", texto, "Basico", 3, new List<string>());

        Assert.False(resultado.EsReal);
        Assert.True(resultado.Preguntas.Count > 0);
    }
}
