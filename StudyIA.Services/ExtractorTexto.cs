using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace StudyIA.Services;

// Saca el texto de los archivos que sube el estudiante: .txt, .md, .csv, .docx y .pdf.
public static class ExtractorTexto
{
    public static readonly string[] Extensiones = { ".txt", ".md", ".csv", ".docx", ".pdf" };

    // Devuelve el texto del archivo o un mensaje de error si no se pudo leer.
    public static async Task<(string? Texto, string? Error)> ExtraerAsync(string nombreArchivo, Stream contenido)
    {
        string extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
        if (!Extensiones.Contains(extension))
        {
            return (null, "Formato no permitido. Usa: " + string.Join(", ", Extensiones) + ".");
        }

        // Se copia a memoria porque los lectores de PDF y DOCX necesitan poder "retroceder" en el archivo.
        using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria);
        memoria.Position = 0;

        try
        {
            string texto = extension switch
            {
                ".docx" => LeerDocx(memoria),
                ".pdf" => EjecutarConPilaGrande(() => LeerPdf(memoria)),
                _ => new StreamReader(memoria, Encoding.UTF8).ReadToEnd()
            };

            texto = texto.Trim();
            if (texto.Length == 0)
            {
                return (null, "No se encontró texto en el archivo (si es un PDF escaneado, no se puede leer).");
            }
            return (texto, null);
        }
        catch (TimeoutException)
        {
            return (null, "El archivo tardó demasiado en leerse. Prueba con un PDF más corto o pega el texto directamente.");
        }
        catch (Exception)
        {
            return (null, "No se pudo leer el archivo. Revisa que no esté dañado ni protegido con contraseña.");
        }
    }

    // Un .docx es un ZIP: el texto está en word/document.xml dentro de las etiquetas <w:t>.
    private static string LeerDocx(Stream archivo)
    {
        using var zip = new ZipArchive(archivo, ZipArchiveMode.Read, leaveOpen: true);
        var entrada = zip.GetEntry("word/document.xml") ?? throw new InvalidDataException("No es un .docx válido.");

        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XDocument xml = XDocument.Load(entrada.Open());

        var parrafos = xml.Descendants(w + "p")
            .Select(p => string.Concat(p.Descendants(w + "t").Select(t => t.Value)))
            .Where(texto => texto.Length > 0);
        return string.Join("\n", parrafos);
    }

    // Límites para que un PDF enorme o raro no consuma toda la memoria ni tarde demasiado.
    private const int MaxPaginasPdf = 100;
    private const int MaxCaracteresLectura = 120_000;
    private static readonly TimeSpan TiempoMaximoLectura = TimeSpan.FromSeconds(30);

    // Se lee el texto página por página con la librería PdfPig y se detiene al llegar a los límites.
    private static string LeerPdf(Stream archivo)
    {
        using var pdf = PdfDocument.Open(archivo);
        var texto = new StringBuilder();
        int total = Math.Min(pdf.NumberOfPages, MaxPaginasPdf);
        for (int i = 1; i <= total && texto.Length < MaxCaracteresLectura; i++)
        {
            texto.AppendLine(pdf.GetPage(i).Text);
        }
        return texto.ToString();
    }

    // Algunos PDF tienen una estructura tan anidada que la lectura necesita mucha "pila" (memoria de llamadas).
    // Se lee en un hilo aparte con una pila grande y con un tiempo máximo, para proteger a toda la aplicación.
    private static string EjecutarConPilaGrande(Func<string> trabajo)
    {
        string resultado = "";
        Exception? error = null;

        var hilo = new Thread(() =>
        {
            try { resultado = trabajo(); }
            catch (Exception ex) { error = ex; }
        }, 128 * 1024 * 1024)
        {
            IsBackground = true   // si se pasa del tiempo, no impide que la aplicación cierre
        };

        hilo.Start();
        if (!hilo.Join(TiempoMaximoLectura))
        {
            throw new TimeoutException("La lectura del archivo superó el tiempo máximo.");
        }
        if (error != null)
        {
            throw new InvalidDataException("No se pudo leer el archivo.", error);
        }
        return resultado;
    }
}
