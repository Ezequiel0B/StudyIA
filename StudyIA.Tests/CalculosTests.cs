using StudyIA.Domain.Entities;
using StudyIA.Services;
using Xunit;

namespace StudyIA.Tests;

// Pruebas de las reglas de negocio: calificación, dominio y temas débiles.
public class CalculosTests
{
    private static Autoevaluacion CrearEvaluacion(int diasAtras, int correctas, int total, bool finalizada = true)
    {
        return new Autoevaluacion
        {
            Fecha = DateTime.UtcNow.AddDays(-diasAtras),
            Correctas = correctas,
            Total = total,
            Finalizada = finalizada
        };
    }

    private static Pregunta CrearPregunta(int id, string tema, string correcta)
    {
        return new Pregunta { Id = id, Tema = tema, Enunciado = "?", RespuestaCorrecta = correcta };
    }

    [Theory]
    [InlineData(7, 10, 70)]
    [InlineData(0, 10, 0)]
    [InlineData(10, 10, 100)]
    [InlineData(1, 3, 33)]
    [InlineData(5, 0, 0)]
    public void Porcentaje_CalculaCorrectamente(int correctas, int total, int esperado)
    {
        Assert.Equal(esperado, Calculos.Porcentaje(correctas, total));
    }

    [Fact]
    public void CalcularDominio_SinEvaluaciones_EsCero()
    {
        Assert.Equal(0, Calculos.CalcularDominio(new List<Autoevaluacion>()));
    }

    [Fact]
    public void CalcularDominio_IgnoraLasEvaluacionesSinTerminar()
    {
        var evaluaciones = new List<Autoevaluacion>
        {
            CrearEvaluacion(1, 8, 10),
            CrearEvaluacion(0, 0, 10, finalizada: false)
        };
        Assert.Equal(80, Calculos.CalcularDominio(evaluaciones));
    }

    [Fact]
    public void CalcularDominio_PromediaSoloLasUltimasCinco()
    {
        var evaluaciones = new List<Autoevaluacion>();
        // 5 recientes con 100% y una muy antigua con 0%: la antigua no debe contar.
        for (int i = 0; i < 5; i++)
        {
            evaluaciones.Add(CrearEvaluacion(i, 10, 10));
        }
        evaluaciones.Add(CrearEvaluacion(30, 0, 10));

        Assert.Equal(100, Calculos.CalcularDominio(evaluaciones));
    }

    [Theory]
    [InlineData(0, "Basico")]
    [InlineData(49, "Basico")]
    [InlineData(50, "Intermedio")]
    [InlineData(74, "Intermedio")]
    [InlineData(75, "Avanzado")]
    [InlineData(100, "Avanzado")]
    public void NivelSugerido_SegunElDominio(int dominio, string esperado)
    {
        Assert.Equal(esperado, Calculos.NivelSugerido(dominio));
    }

    [Theory]
    [InlineData(10, "mal")]
    [InlineData(60, "regular")]
    [InlineData(90, "bien")]
    public void ClasificarTema_SegunElPorcentaje(int porcentaje, string esperado)
    {
        Assert.Equal(esperado, Calculos.ClasificarTema(porcentaje));
    }

    [Fact]
    public void Calificar_CuentaLasRespuestasCorrectasYMarcaComoFinalizada()
    {
        var evaluacion = new Autoevaluacion();
        evaluacion.Preguntas.Add(CrearPregunta(1, "SQL", "A"));
        evaluacion.Preguntas.Add(CrearPregunta(2, "SQL", "B"));
        evaluacion.Preguntas.Add(CrearPregunta(3, "Normalización", "C"));

        var respuestas = new Dictionary<int, string>
        {
            { 1, "A" },   // correcta
            { 2, "d" },   // incorrecta (también prueba que se acepte minúscula)
            { 3, "C" }    // correcta
        };

        int correctas = Calculos.Calificar(evaluacion, respuestas);

        Assert.Equal(2, correctas);
        Assert.Equal(2, evaluacion.Correctas);
        Assert.Equal(3, evaluacion.Total);
        Assert.True(evaluacion.Finalizada);
        Assert.Equal(67, evaluacion.Porcentaje);
        Assert.Equal("D", evaluacion.Preguntas[1].RespuestaUsuario);
    }

    [Fact]
    public void Calificar_UnaPreguntaSinResponderCuentaComoIncorrecta()
    {
        var evaluacion = new Autoevaluacion();
        evaluacion.Preguntas.Add(CrearPregunta(1, "SQL", "A"));
        evaluacion.Preguntas.Add(CrearPregunta(2, "SQL", "B"));

        int correctas = Calculos.Calificar(evaluacion, new Dictionary<int, string> { { 1, "A" } });

        Assert.Equal(1, correctas);
        Assert.Null(evaluacion.Preguntas[1].RespuestaUsuario);
    }

    [Fact]
    public void TemasPorDesempeno_OrdenaDePeorAMejor()
    {
        var preguntas = new List<Pregunta>
        {
            new Pregunta { Id = 1, Tema = "SQL", RespuestaCorrecta = "A", RespuestaUsuario = "A" },
            new Pregunta { Id = 2, Tema = "SQL", RespuestaCorrecta = "A", RespuestaUsuario = "A" },
            new Pregunta { Id = 3, Tema = "Normalización", RespuestaCorrecta = "B", RespuestaUsuario = "A" },
            new Pregunta { Id = 4, Tema = "Normalización", RespuestaCorrecta = "C", RespuestaUsuario = "C" }
        };

        var temas = Calculos.TemasPorDesempeno(preguntas);

        Assert.Equal(2, temas.Count);
        Assert.Equal("Normalización", temas[0].Tema);   // 50%: el más débil va primero
        Assert.Equal(50, temas[0].Porcentaje);
        Assert.Equal("SQL", temas[1].Tema);              // 100%
        Assert.Equal(100, temas[1].Porcentaje);
    }
}
