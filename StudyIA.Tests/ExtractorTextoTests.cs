using System.IO.Compression;
using System.Text;
using StudyIA.Services;
using Xunit;

namespace StudyIA.Tests;

// Pruebas de la lectura de archivos subidos (.txt, .docx y formatos no permitidos).
public class ExtractorTextoTests
{
    [Fact]
    public async Task Extraer_ArchivoTxt_DevuelveElTexto()
    {
        using var flujo = new MemoryStream(Encoding.UTF8.GetBytes("Apuntes de normalización"));
        var (texto, error) = await ExtractorTexto.ExtraerAsync("apuntes.txt", flujo);

        Assert.Null(error);
        Assert.Equal("Apuntes de normalización", texto);
    }

    [Fact]
    public async Task Extraer_FormatoNoPermitido_DevuelveError()
    {
        using var flujo = new MemoryStream(new byte[] { 1, 2, 3 });
        var (texto, error) = await ExtractorTexto.ExtraerAsync("programa.exe", flujo);

        Assert.Null(texto);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Extraer_ArchivoVacio_DevuelveError()
    {
        using var flujo = new MemoryStream(Array.Empty<byte>());
        var (texto, error) = await ExtractorTexto.ExtraerAsync("vacio.txt", flujo);

        Assert.Null(texto);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Extraer_ArchivoDocx_DevuelveLosParrafos()
    {
        // Se arma un .docx mínimo en memoria: un ZIP con word/document.xml.
        const string xml =
            "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" +
            "<w:p><w:r><w:t>Primer párrafo</w:t></w:r></w:p>" +
            "<w:p><w:r><w:t>Segundo </w:t></w:r><w:r><w:t>párrafo</w:t></w:r></w:p>" +
            "</w:body></w:document>";

        using var docx = new MemoryStream();
        using (var zip = new ZipArchive(docx, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entrada = zip.CreateEntry("word/document.xml");
            using var escritor = new StreamWriter(entrada.Open(), Encoding.UTF8);
            escritor.Write(xml);
        }
        docx.Position = 0;

        var (texto, error) = await ExtractorTexto.ExtraerAsync("apunte.docx", docx);

        Assert.Null(error);
        Assert.Equal("Primer párrafo\nSegundo párrafo", texto);
    }
}
