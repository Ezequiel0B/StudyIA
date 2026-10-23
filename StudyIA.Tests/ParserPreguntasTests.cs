using StudyIA.Services.IA;
using Xunit;

namespace StudyIA.Tests;

// Pruebas del parser que convierte la respuesta de la IA (JSON) en preguntas.
public class ParserPreguntasTests
{
    private const string JsonValido = """
        [
          {"tema":"Normalización","enunciado":"¿Qué elimina la 3FN?","opciones":["Dependencias transitivas","Claves","Tablas","Índices"],"correcta":"A","explicacion":"La 3FN elimina dependencias transitivas."},
          {"tema":"SQL","enunciado":"¿Qué filtra WHERE?","opciones":["Columnas","Registros","Tablas","Usuarios"],"correcta":"b","explicacion":"WHERE filtra registros."}
        ]
        """;

    [Fact]
    public void Parsear_ConJsonValido_DevuelveLasPreguntas()
    {
        var preguntas = ParserPreguntas.Parsear(JsonValido);

        Assert.Equal(2, preguntas.Count);
        Assert.Equal("Normalización", preguntas[0].Tema);
        Assert.Equal(4, preguntas[0].Opciones.Length);
        Assert.Equal("A", preguntas[0].Correcta);
        Assert.Equal("B", preguntas[1].Correcta);   // "b" se normaliza a mayúscula
    }

    [Fact]
    public void Parsear_AceptaElJsonEnvueltoEnBloqueDeCodigo()
    {
        string respuesta = "```json\n" + JsonValido + "\n```";
        Assert.Equal(2, ParserPreguntas.Parsear(respuesta).Count);
    }

    [Fact]
    public void Parsear_AceptaLaRespuestaCorrectaComoNumero()
    {
        string json = """[{"enunciado":"¿Pregunta?","opciones":["a","b","c","d"],"correcta":2}]""";
        var preguntas = ParserPreguntas.Parsear(json);
        Assert.Equal("C", preguntas[0].Correcta);
        Assert.Equal("General", preguntas[0].Tema);
    }

    [Fact]
    public void Parsear_DescartaLasPreguntasIncompletas()
    {
        string json = """
            [
              {"enunciado":"Sin opciones suficientes","opciones":["a","b"],"correcta":"A"},
              {"enunciado":"Letra inválida","opciones":["a","b","c","d"],"correcta":"Z"},
              {"enunciado":"Esta sí es válida","opciones":["a","b","c","d"],"correcta":"D"}
            ]
            """;
        var preguntas = ParserPreguntas.Parsear(json);

        Assert.Single(preguntas);
        Assert.Equal("Esta sí es válida", preguntas[0].Enunciado);
    }

    [Fact]
    public void Parsear_SinNingunaPreguntaValida_LanzaFormatException()
    {
        Assert.Throws<FormatException>(() => ParserPreguntas.Parsear("[]"));
    }

    [Fact]
    public void Parsear_ConTextoQueNoEsJson_LanzaFormatException()
    {
        Assert.Throws<FormatException>(() => ParserPreguntas.Parsear("Lo siento, no puedo ayudarte."));
    }
}
