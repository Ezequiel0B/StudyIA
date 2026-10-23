using System.Text.Json;

namespace StudyIA.Services.IA;

// La IA responde con texto (JSON). Esta clase lo convierte en objetos PreguntaGenerada
// y descarta las preguntas que vengan incompletas o mal formadas.
public static class ParserPreguntas
{
    public static List<PreguntaGenerada> Parsear(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new FormatException("La IA devolvió una respuesta vacía.");
        }

        // A veces la IA envuelve el JSON en ```json ... ```: nos quedamos solo con el arreglo [ ... ].
        int inicio = texto.IndexOf('[');
        int fin = texto.LastIndexOf(']');
        if (inicio < 0 || fin <= inicio)
        {
            throw new FormatException("La respuesta de la IA no contiene un arreglo JSON.");
        }
        string json = texto.Substring(inicio, fin - inicio + 1);

        var resultado = new List<PreguntaGenerada>();

        using JsonDocument documento = JsonDocument.Parse(json);
        foreach (JsonElement item in documento.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string enunciado = LeerTexto(item, "enunciado");
            string correcta = LeerCorrecta(item);
            string[] opciones = LeerOpciones(item);

            // Solo se aceptan preguntas completas: enunciado, 4 opciones y una letra válida.
            if (enunciado == "" || opciones.Length != 4 || correcta == "")
            {
                continue;
            }

            resultado.Add(new PreguntaGenerada
            {
                Tema = LeerTexto(item, "tema") == "" ? "General" : Recortar(LeerTexto(item, "tema"), 100),
                Enunciado = Recortar(enunciado, 600),
                Opciones = opciones.Select(o => Recortar(o, 300)).ToArray(),
                Correcta = correcta,
                Explicacion = Recortar(LeerTexto(item, "explicacion"), 600)
            });
        }

        if (resultado.Count == 0)
        {
            throw new FormatException("La IA no devolvió ninguna pregunta válida.");
        }
        return resultado;
    }

    private static string LeerTexto(JsonElement item, string nombre)
    {
        if (item.TryGetProperty(nombre, out JsonElement valor) && valor.ValueKind == JsonValueKind.String)
        {
            return valor.GetString()?.Trim() ?? "";
        }
        return "";
    }

    // Acepta "A", "b", "C) texto" o un número 0-3.
    private static string LeerCorrecta(JsonElement item)
    {
        if (!item.TryGetProperty("correcta", out JsonElement valor))
        {
            return "";
        }

        if (valor.ValueKind == JsonValueKind.Number && valor.TryGetInt32(out int indice) && indice >= 0 && indice <= 3)
        {
            return ((char)('A' + indice)).ToString();
        }

        if (valor.ValueKind == JsonValueKind.String)
        {
            string letra = (valor.GetString() ?? "").Trim().ToUpperInvariant();
            if (letra.Length > 0 && letra[0] >= 'A' && letra[0] <= 'D')
            {
                return letra[0].ToString();
            }
        }
        return "";
    }

    private static string[] LeerOpciones(JsonElement item)
    {
        if (!item.TryGetProperty("opciones", out JsonElement arreglo) || arreglo.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var lista = new List<string>();
        foreach (JsonElement opcion in arreglo.EnumerateArray())
        {
            if (opcion.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(opcion.GetString()))
            {
                lista.Add(opcion.GetString()!.Trim());
            }
        }
        return lista.ToArray();
    }

    private static string Recortar(string texto, int maximo)
    {
        return texto.Length <= maximo ? texto : texto.Substring(0, maximo);
    }
}
