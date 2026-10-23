using Microsoft.AspNetCore.Mvc;
using StudyIA.Domain.Entities;
using StudyIA.Services;

namespace StudyIA.Web.Pages.Evaluaciones;

// Resolver una autoevaluación: se muestran las preguntas y al enviar se califica.
public class ResolverModel : PaginaBase
{
    private readonly AutoevaluacionService _evaluaciones;

    public ResolverModel(AutoevaluacionService evaluaciones)
    {
        _evaluaciones = evaluaciones;
    }

    public Autoevaluacion? Evaluacion { get; set; }

    // Llave = id de la pregunta, valor = letra elegida.
    [BindProperty]
    public Dictionary<int, string> Respuestas { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Evaluacion = await _evaluaciones.ObtenerAsync(id, UsuarioId);
        if (Evaluacion == null)
        {
            return NotFound();
        }

        // Si ya se calificó, se va directo al resultado.
        if (Evaluacion.Finalizada)
        {
            return RedirectToPage("/Evaluaciones/Resultado", new { id });
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var evaluacion = await _evaluaciones.ResponderAsync(id, UsuarioId, Respuestas);
        if (evaluacion == null)
        {
            return NotFound();
        }
        return RedirectToPage("/Evaluaciones/Resultado", new { id });
    }
}
