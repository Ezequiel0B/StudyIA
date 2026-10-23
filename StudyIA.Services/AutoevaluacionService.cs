using Microsoft.EntityFrameworkCore;
using StudyIA.Data;
using StudyIA.Domain.Entities;
using StudyIA.Services.IA;

namespace StudyIA.Services;

// Lógica de las autoevaluaciones adaptativas:
// 1) toma el material de la materia, 2) detecta los temas donde el estudiante va mal,
// 3) le pide a la IA preguntas (dando prioridad a esos temas), 4) guarda y califica.
public class AutoevaluacionService
{
    public static readonly string[] Niveles = { "Basico", "Intermedio", "Avanzado" };

    private readonly StudyIADbContext _db;
    private readonly IIaService _ia;
    private readonly MaterialService _materiales;

    public AutoevaluacionService(StudyIADbContext db, IIaService ia, MaterialService materiales)
    {
        _db = db;
        _ia = ia;
        _materiales = materiales;
    }

    // Genera una autoevaluación nueva y la guarda en la base de datos.
    public async Task<(Autoevaluacion? Evaluacion, string? Error)> GenerarAsync(int usuarioId, int materiaId, string nivel, int cantidad)
    {
        var materia = await _db.Materias.FirstOrDefaultAsync(m => m.Id == materiaId && m.UsuarioId == usuarioId);
        if (materia == null)
        {
            return (null, "La materia no existe.");
        }

        cantidad = Math.Clamp(cantidad, 3, 10);
        if (!Niveles.Contains(nivel))
        {
            nivel = "Intermedio";
        }

        string contexto = await _materiales.ContextoAsync(usuarioId, materiaId, 6000);
        if (contexto == "")
        {
            return (null, "Primero sube material de esta materia para poder generar preguntas.");
        }

        // Parte "adaptativa": los temas con menos de 70% de aciertos se priorizan.
        var temas = await TemasPorMateriaAsync(materiaId, usuarioId);
        var temasDebiles = temas.Where(t => t.Porcentaje < 70).Take(3).Select(t => t.Tema).ToList();

        var resultado = await _ia.GenerarPreguntasAsync(materia.Nombre, contexto, nivel, cantidad, temasDebiles);
        if (resultado.Preguntas.Count == 0)
        {
            return (null, "No se pudieron generar preguntas con este material. Agrega más texto e inténtalo otra vez.");
        }

        var evaluacion = new Autoevaluacion
        {
            MateriaId = materiaId,
            Nivel = nivel,
            GeneradaConIa = resultado.EsReal,
            Total = resultado.Preguntas.Count
        };

        foreach (var p in resultado.Preguntas)
        {
            evaluacion.Preguntas.Add(new Pregunta
            {
                Tema = p.Tema,
                Enunciado = p.Enunciado,
                OpcionA = p.Opciones[0],
                OpcionB = p.Opciones[1],
                OpcionC = p.Opciones[2],
                OpcionD = p.Opciones[3],
                RespuestaCorrecta = p.Correcta,
                Explicacion = p.Explicacion
            });
        }

        _db.Autoevaluaciones.Add(evaluacion);
        await _db.SaveChangesAsync();
        return (evaluacion, null);
    }

    public async Task<Autoevaluacion?> ObtenerAsync(int id, int usuarioId)
    {
        return await _db.Autoevaluaciones
            .Include(e => e.Preguntas)
            .Include(e => e.Materia)
            .FirstOrDefaultAsync(e => e.Id == id && e.Materia!.UsuarioId == usuarioId);
    }

    // Recibe las respuestas del estudiante, califica y guarda el resultado.
    public async Task<Autoevaluacion?> ResponderAsync(int id, int usuarioId, IDictionary<int, string> respuestas)
    {
        var evaluacion = await ObtenerAsync(id, usuarioId);
        if (evaluacion == null || evaluacion.Finalizada)
        {
            return evaluacion;
        }

        Calculos.Calificar(evaluacion, respuestas);
        await _db.SaveChangesAsync();
        return evaluacion;
    }

    // Historial de las últimas 20 autoevaluaciones.
    public async Task<List<Autoevaluacion>> HistorialAsync(int usuarioId, int? materiaId = null)
    {
        var consulta = _db.Autoevaluaciones
            .Include(e => e.Materia)
            .Where(e => e.Materia!.UsuarioId == usuarioId);

        if (materiaId.HasValue)
        {
            consulta = consulta.Where(e => e.MateriaId == materiaId.Value);
        }

        return await consulta.OrderByDescending(e => e.Fecha).Take(20).ToListAsync();
    }

    // Desempeño por tema en todas las autoevaluaciones terminadas de una materia.
    public async Task<List<TemaResumen>> TemasPorMateriaAsync(int materiaId, int usuarioId)
    {
        var preguntas = await _db.Preguntas
            .Where(p => p.Autoevaluacion!.MateriaId == materiaId
                     && p.Autoevaluacion.Materia!.UsuarioId == usuarioId
                     && p.Autoevaluacion.Finalizada)
            .ToListAsync();

        return Calculos.TemasPorDesempeno(preguntas);
    }
}
