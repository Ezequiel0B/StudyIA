using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StudyIA.Domain.Entities;
using StudyIA.Services;
using StudyIA.Services.IA;

namespace StudyIA.Web.Pages.Evaluaciones;

// Pantalla para generar autoevaluaciones (característica de IA) y ver el historial.
public class IndexModel : PaginaBase
{
    private readonly AutoevaluacionService _evaluaciones;
    private readonly MateriaService _materias;
    private readonly IIaService _ia;

    public IndexModel(AutoevaluacionService evaluaciones, MateriaService materias, IIaService ia)
    {
        _evaluaciones = evaluaciones;
        _materias = materias;
        _ia = ia;
    }

    // Materia elegida en el paso 1 (viene de la URL).
    [BindProperty(SupportsGet = true)]
    public int? MateriaId { get; set; }

    [BindProperty]
    public GenerarInput Input { get; set; } = new();

    public class GenerarInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una materia.")]
        public int MateriaId { get; set; }

        [Required(ErrorMessage = "Elige un nivel.")]
        public string Nivel { get; set; } = "Intermedio";

        [Range(3, 10, ErrorMessage = "La cantidad de preguntas debe estar entre 3 y 10.")]
        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; } = 5;
    }

    public List<MateriaConDominio> Materias { get; set; } = new();
    public List<Autoevaluacion> Historial { get; set; } = new();
    public List<TemaResumen> Temas { get; set; } = new();
    public MateriaConDominio? MateriaSeleccionada { get; set; }
    public string NivelSugerido { get; set; } = "Intermedio";
    public string Motor => _ia.NombreMotor;

    public async Task OnGetAsync()
    {
        await CargarAsync();

        // Se propone el nivel según el dominio actual de la materia.
        if (MateriaSeleccionada != null)
        {
            Input.MateriaId = MateriaSeleccionada.Materia.Id;
            Input.Nivel = NivelSugerido;
        }
    }

    // Genera la autoevaluación con IA y lleva al estudiante a resolverla.
    public async Task<IActionResult> OnPostGenerarAsync()
    {
        MateriaId = Input.MateriaId;

        if (!ModelState.IsValid)
        {
            await CargarAsync();
            return Page();
        }

        var (evaluacion, error) = await _evaluaciones.GenerarAsync(UsuarioId, Input.MateriaId, Input.Nivel, Input.Cantidad);
        if (evaluacion == null)
        {
            ModelState.AddModelError(string.Empty, error ?? "No se pudo generar la autoevaluación.");
            await CargarAsync();
            return Page();
        }

        return RedirectToPage("/Evaluaciones/Resolver", new { id = evaluacion.Id });
    }

    private async Task CargarAsync()
    {
        Materias = await _materias.ListarConDominioAsync(UsuarioId);
        Historial = await _evaluaciones.HistorialAsync(UsuarioId, MateriaId);

        if (MateriaId.HasValue)
        {
            MateriaSeleccionada = Materias.FirstOrDefault(m => m.Materia.Id == MateriaId.Value);
            if (MateriaSeleccionada != null)
            {
                NivelSugerido = Calculos.NivelSugerido(MateriaSeleccionada.Dominio);
                Temas = await _evaluaciones.TemasPorMateriaAsync(MateriaSeleccionada.Materia.Id, UsuarioId);
            }
        }
    }
}
