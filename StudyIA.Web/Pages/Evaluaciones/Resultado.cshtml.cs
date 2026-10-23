using Microsoft.AspNetCore.Mvc;
using StudyIA.Domain.Entities;
using StudyIA.Services;

namespace StudyIA.Web.Pages.Evaluaciones;

// Muestra el resultado de una autoevaluación ya calificada.
public class ResultadoModel : PaginaBase
{
    private readonly AutoevaluacionService _evaluaciones;
    private readonly MateriaService _materias;

    public ResultadoModel(AutoevaluacionService evaluaciones, MateriaService materias)
    {
        _evaluaciones = evaluaciones;
        _materias = materias;
    }

    public Autoevaluacion? Evaluacion { get; set; }
    public List<TemaResumen> Temas { get; set; } = new();
    public int DominioMateria { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Evaluacion = await _evaluaciones.ObtenerAsync(id, UsuarioId);
        if (Evaluacion == null)
        {
            return NotFound();
        }

        // Si todavía no la ha resuelto, se le manda a resolverla.
        if (!Evaluacion.Finalizada)
        {
            return RedirectToPage("/Evaluaciones/Resolver", new { id });
        }

        // Temas de ESTA autoevaluación, de peor a mejor.
        Temas = Calculos.TemasPorDesempeno(Evaluacion.Preguntas);

        var materias = await _materias.ListarConDominioAsync(UsuarioId);
        DominioMateria = materias.FirstOrDefault(m => m.Materia.Id == Evaluacion.MateriaId)?.Dominio ?? 0;
        return Page();
    }
}
