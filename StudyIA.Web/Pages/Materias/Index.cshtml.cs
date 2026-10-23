using Microsoft.AspNetCore.Mvc;
using StudyIA.Domain.Entities;
using StudyIA.Services;

namespace StudyIA.Web.Pages.Materias;

// Lista de materias (READ) y eliminación (DELETE).
public class IndexModel : PaginaBase
{
    private readonly MateriaService _materias;

    public IndexModel(MateriaService materias)
    {
        _materias = materias;
    }

    public List<Materia> Lista { get; set; } = new();

    public async Task OnGetAsync()
    {
        Lista = await _materias.ListarAsync(UsuarioId);
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        bool eliminada = await _materias.EliminarAsync(id, UsuarioId);
        if (eliminada)
        {
            Aviso("Materia eliminada.");
        }
        else
        {
            AvisoError("No se encontró la materia.");
        }
        return RedirectToPage();
    }
}
