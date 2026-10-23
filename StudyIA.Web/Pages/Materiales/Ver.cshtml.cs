using Microsoft.AspNetCore.Mvc;
using StudyIA.Domain.Entities;
using StudyIA.Services;
using StudyIA.Services.IA;

namespace StudyIA.Web.Pages.Materiales;

// Ver un material (READ) y generar su resumen con IA.
public class VerModel : PaginaBase
{
    private readonly MaterialService _materiales;
    private readonly IIaService _ia;

    public VerModel(MaterialService materiales, IIaService ia)
    {
        _materiales = materiales;
        _ia = ia;
    }

    public Material? Material { get; set; }

    // Nombre del motor de IA que se está usando (Azure OpenAI o modo local).
    public string Motor => _ia.NombreMotor;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Material = await _materiales.ObtenerAsync(id, UsuarioId);
        if (Material == null)
        {
            return NotFound();
        }
        return Page();
    }

    // Característica de IA: resumen automático de datos.
    public async Task<IActionResult> OnPostResumirAsync(int id)
    {
        var material = await _materiales.ObtenerAsync(id, UsuarioId);
        if (material == null)
        {
            return NotFound();
        }

        var respuesta = await _ia.ResumirAsync(material.Titulo, material.Contenido);
        await _materiales.GuardarResumenAsync(id, UsuarioId, respuesta.Texto);

        Aviso(respuesta.EsReal ? "Resumen generado con " + _ia.NombreMotor + "." : "Resumen generado con el modo local (sin IA en la nube).");
        return RedirectToPage(new { id });
    }
}
