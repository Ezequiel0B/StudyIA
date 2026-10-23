using Microsoft.AspNetCore.Mvc;
using StudyIA.Domain.Entities;
using StudyIA.Services;
using StudyIA.Services.IA;

namespace StudyIA.Web.Pages;

// Asistente StudyIA (chatbot): característica de Inteligencia Artificial del proyecto.
// Entrada: pregunta del estudiante + su material.  Procesamiento: IA.  Salida: respuesta.
public class AsistenteModel : PaginaBase
{
    private readonly ChatService _chat;
    private readonly MateriaService _materias;
    private readonly IIaService _ia;

    public AsistenteModel(ChatService chat, MateriaService materias, IIaService ia)
    {
        _chat = chat;
        _materias = materias;
        _ia = ia;
    }

    [BindProperty(SupportsGet = true)]
    public int? MateriaId { get; set; }

    [BindProperty]
    public string Pregunta { get; set; } = "";

    public List<MensajeChat> Historial { get; set; } = new();
    public List<Materia> Materias { get; set; } = new();
    public string Motor => _ia.NombreMotor;
    public bool IaActiva => _ia.EstaConfigurada;

    public async Task OnGetAsync()
    {
        Materias = await _materias.ListarAsync(UsuarioId);
        Historial = await _chat.HistorialAsync(UsuarioId);
    }

    public async Task<IActionResult> OnPostPreguntarAsync(int? materiaId)
    {
        var (respuesta, error) = await _chat.PreguntarAsync(UsuarioId, materiaId, Pregunta ?? "");
        if (respuesta == null && error != null)
        {
            AvisoError(error);
        }

        // Patrón "Post-Redirect-Get": se redirige para no reenviar el formulario al refrescar.
        return RedirectToPage(new { materiaId });
    }

    public async Task<IActionResult> OnPostLimpiarAsync()
    {
        await _chat.LimpiarAsync(UsuarioId);
        Aviso("Conversación borrada.");
        return RedirectToPage();
    }
}
