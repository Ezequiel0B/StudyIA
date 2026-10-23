using System.Text.RegularExpressions;

namespace StudyIA.Services.IA;

// MODO LOCAL: funciona sin internet y sin clave de IA.
// No es IA generativa: usa reglas simples sobre el texto (extracción de oraciones y palabras clave).
// Sirve como respaldo para que la aplicación nunca se quede sin respuesta.
public static class GeneradorLocal
{
    private static readonly HashSet<string> PalabrasVacias = new(StringComparer.OrdinalIgnoreCase)
    {
        "para", "como", "desde", "entre", "sobre", "donde", "cuando", "porque", "tiene", "tienen",
        "puede", "pueden", "cada", "esta", "este", "estos", "estas", "pero", "también", "además",
        "hasta", "sino", "según", "mismo", "misma", "otros", "otras", "cual", "cuales", "cuál",
        "qué", "sus", "los", "las", "una", "unos", "unas", "del", "por", "con", "sin", "muy", "más",
        "son", "ser", "fue", "hay", "han", "les", "nos", "que", "así", "solo", "sólo", "todo", "todos"
    };

    // Divide un texto en oraciones (por punto, signos de pregunta/exclamación o saltos de línea).
    public static List<string> Oraciones(string texto)
    {
        return Regex.Split(texto, @"(?<=[\.\?\!])\s+|\r?\n+")
            .Select(o => o.Trim())
            .Where(o => o.Length >= 20)
            .ToList();
    }

    // Palabras "importantes": más de 3 letras y que no sean palabras comunes.
    public static List<string> Palabras(string texto)
    {
        return Regex.Matches(texto.ToLowerInvariant(), @"[\p{L}\p{N}]+")
            .Select(m => m.Value)
            .Where(p => p.Length > 3 && !PalabrasVacias.Contains(p))
            .ToList();
    }

    // Resumen: se eligen las oraciones que contienen las palabras más repetidas del texto.
    public static string Resumir(string contenido, int maxOraciones = 4)
    {
        var oraciones = Oraciones(contenido);
        if (oraciones.Count == 0)
        {
            return contenido.Length <= 300 ? contenido : contenido.Substring(0, 300) + "...";
        }
        if (oraciones.Count <= maxOraciones)
        {
            return string.Join("\n", oraciones.Select(o => "• " + o));
        }

        // Frecuencia de cada palabra en todo el texto.
        var frecuencia = Palabras(contenido)
            .GroupBy(p => p)
            .ToDictionary(g => g.Key, g => g.Count());

        var elegidas = oraciones
            .Select((texto, indice) => new
            {
                Indice = indice,
                Texto = texto,
                Puntaje = Palabras(texto).Sum(p => frecuencia.ContainsKey(p) ? frecuencia[p] : 0) / (double)(Palabras(texto).Count + 1)
            })
            .OrderByDescending(o => o.Puntaje)
            .Take(maxOraciones)
            .OrderBy(o => o.Indice)   // se conserva el orden original del apunte
            .Select(o => "• " + o.Texto);

        return string.Join("\n", elegidas);
    }

    // Respuesta: busca las oraciones del material que más se parecen a la pregunta.
    public static string Responder(string pregunta, string contexto)
    {
        if (string.IsNullOrWhiteSpace(contexto))
        {
            return "Todavía no tienes material cargado. Sube tus apuntes en la sección Material para que pueda ayudarte.";
        }

        if (EsSaludo(pregunta))
        {
            return "¡Hola! Soy el Asistente StudyIA. Pregúntame sobre tu material de estudio." + Sugerencia(contexto);
        }

        // Se comparan los primeros 5 caracteres de cada palabra para ignorar plurales y conjugaciones.
        var raices = Palabras(pregunta).Select(Raiz).Distinct().ToList();

        var mejores = Oraciones(contexto)
            .Select(o => new
            {
                Texto = o,
                Puntaje = Palabras(o).Select(Raiz).Distinct().Count(r => raices.Contains(r))
            })
            .Where(o => o.Puntaje > 0)
            .OrderByDescending(o => o.Puntaje)
            .Take(2)
            .ToList();

        if (mejores.Count == 0)
        {
            return "No encontré información sobre eso en tu material. Prueba con otras palabras o sube más apuntes." + Sugerencia(contexto);
        }
        return "Según tu material: " + string.Join(" ", mejores.Select(m => m.Texto));
    }

    // Saludos y mensajes de cortesía: no tiene sentido buscarlos en el material.
    private static bool EsSaludo(string pregunta)
    {
        string p = pregunta.Trim().ToLowerInvariant().Trim('¡', '!', '¿', '?', '.', ',', ' ');
        string[] saludos =
        {
            "hola", "buenas", "buen dia", "buen día", "buenos dias", "buenos días", "buenas tardes",
            "buenas noches", "hey", "ayuda", "gracias", "que tal", "qué tal", "hola que tal", "hola qué tal"
        };
        return saludos.Contains(p) || p.StartsWith("hola ");
    }

    // Los títulos de los materiales vienen en el contexto como líneas "## Título".
    private static string Sugerencia(string contexto)
    {
        var titulos = contexto.Split('\n')
            .Select(linea => linea.Trim())
            .Where(linea => linea.StartsWith("## "))
            .Select(linea => linea.Substring(3).Trim())
            .Where(titulo => titulo.Length > 0)
            .Distinct()
            .Take(4)
            .ToList();
        return titulos.Count == 0 ? "" : " Por ejemplo, puedes preguntar sobre: " + string.Join(", ", titulos) + ".";
    }

    // Preguntas tipo "completa la oración": se oculta una palabra clave y se ofrecen 4 opciones.
    public static List<PreguntaGenerada> GenerarPreguntas(string contexto, string tema, int cantidad)
    {
        var resultado = new List<PreguntaGenerada>();
        var oraciones = Oraciones(contexto).Where(o => o.Length >= 40 && o.Length <= 350).ToList();
        var todasLasPalabras = Palabras(contexto).Where(p => p.Length >= 6).Distinct().ToList();
        var palabrasUsadas = new HashSet<string>();
        string[] relleno = { "ninguna de las anteriores", "todas las anteriores", "no se menciona en el material" };

        foreach (string oracion in oraciones)
        {
            if (resultado.Count >= cantidad)
            {
                break;
            }

            // La palabra clave es la más larga de la oración que no se haya usado antes.
            string? clave = Palabras(oracion)
                .Where(p => p.Length >= 6)
                .Distinct()
                .OrderByDescending(p => p.Length)
                .FirstOrDefault(p => palabrasUsadas.Add(p));
            if (clave == null)
            {
                continue;
            }

            var azar = new Random(oracion.Length * 31 + resultado.Count);

            // Opciones incorrectas: otras palabras del mismo material.
            var opciones = todasLasPalabras
                .Where(p => !p.Equals(clave, StringComparison.OrdinalIgnoreCase))
                .OrderBy(_ => azar.Next())
                .Take(3)
                .ToList();
            foreach (string extra in relleno)
            {
                if (opciones.Count >= 3) break;
                opciones.Add(extra);
            }

            opciones.Add(clave);
            opciones = opciones.OrderBy(_ => azar.Next()).ToList();
            int posicion = opciones.IndexOf(clave);

            string enunciado = Regex.Replace(oracion, Regex.Escape(clave), "_____", RegexOptions.IgnoreCase);

            resultado.Add(new PreguntaGenerada
            {
                Tema = tema,
                Enunciado = "Completa según tu material: " + enunciado,
                Opciones = opciones.ToArray(),
                Correcta = ((char)('A' + posicion)).ToString(),
                Explicacion = "La oración completa del material dice: " + oracion
            });
        }
        return resultado;
    }

    private static string Raiz(string palabra)
    {
        return palabra.Length <= 5 ? palabra : palabra.Substring(0, 5);
    }
}
