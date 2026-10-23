using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StudyIA.Services;

namespace StudyIA.Web.Pages.Materias;

// Crear (CREATE) y editar (UPDATE) una materia.
public class EditarModel : PaginaBase
{
    private readonly MateriaService _materias;

    public EditarModel(MateriaService materias)
    {
        _materias = materias;
    }

    // Viene de la ruta: si es null estamos creando, si tiene valor estamos editando.
    public int? Id { get; set; }

    [BindProperty]
    public MateriaInput Input { get; set; } = new();

    public class MateriaInput
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede pasar de 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = "";

        [StringLength(300, ErrorMessage = "La descripción no puede pasar de 300 caracteres.")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        Id = id;
        if (id.HasValue)
        {
            var materia = await _materias.ObtenerAsync(id.Value, UsuarioId);
            if (materia == null)
            {
                return NotFound();
            }
            Input = new MateriaInput { Nombre = materia.Nombre, Descripcion = materia.Descripcion };
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        Id = id;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        string? error;
        if (id.HasValue)
        {
            error = await _materias.ActualizarAsync(id.Value, UsuarioId, Input.Nombre, Input.Descripcion);
        }
        else
        {
            var resultado = await _materias.CrearAsync(UsuarioId, Input.Nombre, Input.Descripcion);
            error = resultado.Error;
        }

        if (error != null)
        {
            ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        Aviso(id.HasValue ? "Materia actualizada." : "Materia creada.");
        return RedirectToPage("/Materias/Index");
    }
}
