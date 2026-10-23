using StudyIA.Services.IA;
using Xunit;

namespace StudyIA.Tests;

// Pruebas del modo local (respaldo cuando no hay IA en la nube).
public class GeneradorLocalTests
{
    private const string Material =
        "La normalización es el proceso de organizar las tablas de una base de datos para reducir la redundancia. " +
        "La primera forma normal exige que cada columna contenga valores atómicos. " +
        "La tercera forma normal elimina las dependencias transitivas entre los atributos. " +
        "Una clave foránea referencia la clave primaria de otra tabla y mantiene la integridad referencial. " +
        "La sentencia SELECT permite consultar los datos almacenados en una tabla. " +
        "La cláusula WHERE filtra los registros que cumplen una condición específica.";

    [Fact]
    public void Resumir_DevuelveMenosOracionesQueElTextoOriginal()
    {
        string resumen = GeneradorLocal.Resumir(Material, 3);
        int lineas = resumen.Split('\n').Length;

        Assert.Equal(3, lineas);
        Assert.StartsWith("•", resumen);
    }

    [Fact]
    public void Responder_EncuentraLaOracionRelacionadaConLaPregunta()
    {
        string respuesta = GeneradorLocal.Responder("¿Qué es una clave foránea?", Material);
        Assert.Contains("clave foránea", respuesta);
    }

    [Fact]
    public void Responder_SinMaterial_PideSubirApuntes()
    {
        string respuesta = GeneradorLocal.Responder("¿Qué es SQL?", "");
        Assert.Contains("Sube tus apuntes", respuesta);
    }

    [Fact]
    public void GenerarPreguntas_CreaPreguntasCompletasYConLaRespuestaCorrectaEntreLasOpciones()
    {
        var preguntas = GeneradorLocal.GenerarPreguntas(Material, "Base de Datos", 4);

        Assert.NotEmpty(preguntas);
        Assert.True(preguntas.Count <= 4);

        foreach (var pregunta in preguntas)
        {
            Assert.Equal(4, pregunta.Opciones.Length);
            Assert.Contains("_____", pregunta.Enunciado);
            Assert.Contains(pregunta.Correcta, new[] { "A", "B", "C", "D" });

            // La opción marcada como correcta no debe estar escondida dentro del enunciado.
            int indice = pregunta.Correcta[0] - 'A';
            Assert.False(string.IsNullOrWhiteSpace(pregunta.Opciones[indice]));
            Assert.Equal("Base de Datos", pregunta.Tema);
        }
    }

    [Fact]
    public void GenerarPreguntas_NoRepiteLaMismaOpcionDentroDeUnaPregunta()
    {
        var preguntas = GeneradorLocal.GenerarPreguntas(Material, "Base de Datos", 5);

        foreach (var pregunta in preguntas)
        {
            Assert.Equal(pregunta.Opciones.Length, pregunta.Opciones.Distinct().Count());
        }
    }
}
