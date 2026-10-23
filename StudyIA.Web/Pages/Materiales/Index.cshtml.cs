using Microsoft.AspNetCore.Mvc;
using StudyIA.Domain.Entities;
using StudyIA.Services;

namespace StudyIA.Web.Pages.Materiales;

// Lista de materiales (READ) con filtro por materia, y eliminación (DELETE).
public class IndexModel : PaginaBase
{
    private readonly MaterialService _materiales;
    private readonly MateriaService _materias;

    public IndexModel(MaterialService materiales, MateriaService materias)
    {
        _materiales = materiales;
        _materias = materias;
    }

    // Viene de la URL: /Materiales?materiaId=3
    [BindProperty(SupportsGet = true)]
    public int? MateriaId { get; set; }

    public List<Material> Lista { get; set; } = new();
    public List<Materia> Materias { get; set; } = new();

    public async Task OnGetAsync()
    {
        Materias = await _materias.ListarAsync(UsuarioId);
        Lista = await _materiales.ListarAsync(UsuarioId, MateriaId);
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        bool eliminado = await _materiales.EliminarAsync(id, UsuarioId);
        if (eliminado)
        {
            Aviso("Material eliminado.");
        }
        else
        {
            AvisoError("No se encontró el material.");
        }
        return RedirectToPage(new { materiaId = MateriaId });
    }
}
