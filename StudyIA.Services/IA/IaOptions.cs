namespace StudyIA.Services.IA;

// Configuración de la IA. Se lee de appsettings.json (sección "IA") o de variables de entorno
// (por ejemplo IA__ApiKey). La clave NUNCA se debe subir al repositorio.
public class IaOptions
{
    public const string Seccion = "IA";

    // "AzureOpenAI" o "OpenAI" (también sirve para cualquier servicio compatible con el formato de OpenAI).
    public string Proveedor { get; set; } = "AzureOpenAI";

    // Azure OpenAI: https://NOMBRE-DEL-RECURSO.openai.azure.com
    // OpenAI / compatibles: déjalo vacío para usar https://api.openai.com/v1
    public string Endpoint { get; set; } = "";

    public string ApiKey { get; set; } = "";

    // Azure OpenAI: nombre del "deployment". OpenAI: nombre del modelo (por ejemplo gpt-4o-mini).
    public string Modelo { get; set; } = "gpt-4o-mini";

    // Solo se usa con Azure OpenAI.
    public string ApiVersion { get; set; } = "2024-06-01";

    public bool EsAzure => Proveedor.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase);

    // Hay IA en la nube configurada solo si existe una clave y un modelo.
    public bool Configurada =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(Modelo)
        && (!EsAzure || !string.IsNullOrWhiteSpace(Endpoint));   // Azure siempre necesita su endpoint
}
