using System.Text;
using StudyIA.Services;
using StudyIA.Services.IA;
using Xunit;

namespace StudyIA.Tests;

// Pruebas de las mejoras: saludos y sugerencias del modo local, configuración de la IA y PDF dañado.
public class MejorasFase3Tests
{
    private const string Contexto =
        "## Guía de normalización\n" +
        "La normalización organiza las tablas de una base de datos para reducir la redundancia de datos.\n\n" +
        "## Apuntes de SQL\n" +
        "La sentencia SELECT permite consultar los datos almacenados en una tabla.";

    [Fact]
    public void Responder_Saludo_PresentaAlAsistenteYSugiereTemas()
    {
        string respuesta = GeneradorLocal.Responder("hola", Contexto);

        Assert.Contains("Asistente StudyIA", respuesta);
        Assert.Contains("Guía de normalización", respuesta);
    }

    [Fact]
    public void Responder_SinCoincidencias_SugiereTemasDelMaterial()
    {
        string respuesta = GeneradorLocal.Responder("¿Quién ganó el mundial?", Contexto);

        Assert.Contains("No encontré información", respuesta);
        Assert.Contains("Apuntes de SQL", respuesta);
    }

    [Fact]
    public void IaOptions_AzureSinEndpoint_NoSeConsideraConfigurada()
    {
        var opciones = new IaOptions { Proveedor = "AzureOpenAI", ApiKey = "clave", Modelo = "gpt-4o-mini", Endpoint = "" };
        Assert.False(opciones.Configurada);

        opciones.Endpoint = "https://mi-recurso.openai.azure.com";
        Assert.True(opciones.Configurada);
    }

    [Fact]
    public void IaOptions_OpenAiSinEndpoint_SiEstaConfigurada()
    {
        var opciones = new IaOptions { Proveedor = "OpenAI", ApiKey = "clave", Modelo = "gpt-4o-mini", Endpoint = "" };
        Assert.True(opciones.Configurada);
    }

    [Fact]
    public async Task Extraer_PdfDanado_DevuelveErrorSinLanzarExcepcion()
    {
        using var flujo = new MemoryStream(Encoding.UTF8.GetBytes("esto no es un pdf de verdad"));
        var (texto, error) = await ExtractorTexto.ExtraerAsync("falso.pdf", flujo);

        Assert.Null(texto);
        Assert.NotNull(error);
    }
}
