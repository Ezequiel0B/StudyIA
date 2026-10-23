using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StudyIA.Domain.Entities;
using StudyIA.Services;

namespace StudyIA.Web.Pages.Materiales;

// Crear (CREATE) y editar (UPDATE) un material de estudio.
public class EditarModel : PaginaBase
{
    private const long MaxBytesArchivo = 5 * 1024 * 1024; // 5 MB

    private readonly MaterialService _materiales;
    private readonly MateriaService _materias;

    public EditarModel(MaterialService materiales, MateriaService materias)
    {
        _materiales = materiales;
        _materias = materias;
    }

    public int? Id { get; set; }
    public List<Materia> Materias { get; set; } = new();

    [BindProperty]
    public MaterialInput Input { get; set; } = new();

    // Archivo opcional (.txt, .md, .csv, .docx o .pdf).
    [BindProperty]
    public IFormFile? Archivo { get; set; }

    public class MaterialInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una materia.")]
        [Display(Name = "Materia")]
        public int MateriaId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(150, ErrorMessage = "El título no puede pasar de 150 caracteres.")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = "";

        // No lleva [Required] porque puede venir de un archivo; el servicio valida el largo mínimo.
        [Display(Name = "Contenido")]
        public string? Contenido { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id, int? materiaId)
    {
        Id = id;
        Materias = await _materias.ListarAsync(UsuarioId);

        if (id.HasValue)
        {
            var material = await _materiales.ObtenerAsync(id.Value, UsuarioId);
            if (material == null)
            {
                return NotFound();
            }
            Input = new MaterialInput { MateriaId = material.MateriaId, Titulo = material.Titulo, Contenido = material.Contenido };
        }
        else if (materiaId.HasValue)
        {
            Input.MateriaId = materiaId.Value;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        Id = id;
        Materias = await _materias.ListarAsync(UsuarioId);

        // Si se subió un archivo, se lee su texto.
        if (Archivo != null && Archivo.Length > 0)
        {
            if (Archivo.Length > MaxBytesArchivo)
            {
                ModelState.AddModelError(string.Empty, "El archivo es demasiado grande (máximo 5 MB).");
            }
            else
            {
                // ExtractorTexto sabe leer .txt, .md, .csv, .docx y .pdf.
                using var flujo = Archivo.OpenReadStream();
                var (texto, errorArchivo) = await ExtractorTexto.ExtraerAsync(Archivo.FileName, flujo);
                if (texto == null)
                {
                    ModelState.AddModelError(string.Empty, errorArchivo ?? "No se pudo leer el archivo.");
                }
                else
                {
                    // Si el texto es muy largo se corta al máximo permitido.
                    Input.Contenido = texto.Length > MaterialService.MaxCaracteres
                        ? texto.Substring(0, MaterialService.MaxCaracteres)
                        : texto;
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        string contenido = Input.Contenido ?? "";
        string? error;
        if (id.HasValue)
        {
            error = await _materiales.ActualizarAsync(id.Value, UsuarioId, Input.MateriaId, Input.Titulo, contenido);
        }
        else
        {
            var resultado = await _materiales.CrearAsync(UsuarioId, Input.MateriaId, Input.Titulo, contenido);
            error = resultado.Error;
        }

        if (error != null)
        {
            ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        Aviso(id.HasValue ? "Material actualizado." : "Material guardado.");
        return RedirectToPage("/Materiales/Index", new { materiaId = Input.MateriaId });
    }
}
