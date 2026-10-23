using StudyIA.Domain.Entities;

namespace StudyIA.Services;

// Resumen del desempeño en un tema (por ejemplo "Normalización": 2 de 5 correctas).
public record TemaResumen(string Tema, int Correctas, int Total)
{
    public int Porcentaje => Calculos.Porcentaje(Correctas, Total);
}

// Reglas de negocio puras (no tocan la base de datos), por eso son fáciles de probar.
public static class Calculos
{
    public static int Porcentaje(int correctas, int total)
    {
        if (total <= 0)
        {
            return 0;
        }
        return (int)Math.Round(correctas * 100.0 / total);
    }

    // El dominio de una materia es el promedio de sus últimas 5 autoevaluaciones terminadas.
    public static int CalcularDominio(IEnumerable<Autoevaluacion> evaluaciones)
    {
        var ultimas = evaluaciones
            .Where(e => e.Finalizada)
            .OrderByDescending(e => e.Fecha)
            .Take(5)
            .ToList();

        if (ultimas.Count == 0)
        {
            return 0;
        }
        return (int)Math.Round(ultimas.Average(e => (double)e.Porcentaje));
    }

    // Según el dominio actual, se sugiere qué nivel de dificultad conviene.
    public static string NivelSugerido(int dominio)
    {
        if (dominio < 50) return "Basico";
        if (dominio < 75) return "Intermedio";
        return "Avanzado";
    }

    // Devuelve la palabra que usa el CSS para pintar el color: mal (rojo), regular (amarillo), bien (verde).
    public static string ClasificarTema(int porcentaje)
    {
        if (porcentaje < 50) return "mal";
        if (porcentaje < 75) return "regular";
        return "bien";
    }

    // Califica una autoevaluación: compara lo que contestó el estudiante con la respuesta correcta.
    // respuestas: llave = Id de la pregunta, valor = letra elegida ("A", "B", "C" o "D").
    public static int Calificar(Autoevaluacion evaluacion, IDictionary<int, string> respuestas)
    {
        int correctas = 0;

        foreach (var pregunta in evaluacion.Preguntas)
        {
            if (respuestas.TryGetValue(pregunta.Id, out var elegida) && !string.IsNullOrWhiteSpace(elegida))
            {
                pregunta.RespuestaUsuario = elegida.Trim().ToUpperInvariant();
            }
            else
            {
                pregunta.RespuestaUsuario = null;
            }

            if (pregunta.RespuestaUsuario == pregunta.RespuestaCorrecta)
            {
                correctas++;
            }
        }

        evaluacion.Correctas = correctas;
        evaluacion.Total = evaluacion.Preguntas.Count;
        evaluacion.Finalizada = true;
        return correctas;
    }

    // Agrupa las preguntas por tema y calcula cuántas acertó el estudiante en cada uno.
    // El resultado va de peor a mejor: los primeros son los temas a reforzar.
    public static List<TemaResumen> TemasPorDesempeno(IEnumerable<Pregunta> preguntasRespondidas)
    {
        return preguntasRespondidas
            .GroupBy(p => p.Tema)
            .Select(g => new TemaResumen(
                g.Key,
                g.Count(p => p.RespuestaUsuario == p.RespuestaCorrecta),
                g.Count()))
            .OrderBy(t => t.Porcentaje)
            .ThenBy(t => t.Tema)
            .ToList();
    }
}
