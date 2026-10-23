using Microsoft.AspNetCore.Mvc;
using StudyIA.Domain.Entities;
using StudyIA.Services;

namespace StudyIA.Web.Pages;

// Planificador: exámenes próximos y registro de las sesiones de estudio.
public class PlanificadorModel : PaginaBase
{
    private readonly PlanificadorService _planificador;
    private readonly MateriaService _materias;

    public PlanificadorModel(PlanificadorService planificador, MateriaService materias)
    {
        _planificador = planificador;
        _materias = materias;
    }

    public List<Materia> Materias { get; set; } = new();
    public List<Examen> Examenes { get; set; } = new();
    public List<SesionEstudio> Sesiones { get; set; } = new();
    public int MinutosSemana { get; set; }

    public async Task OnGetAsync()
    {
        await CargarAsync();
    }

    // Cada botón del formulario llama a su propio "handler": OnPostExamen, OnPostSesion, etc.
    public async Task<IActionResult> OnPostExamenAsync(int materiaId, string? titulo, DateTime? fecha, string? notas)
    {
        string? error = await _planificador.CrearExamenAsync(UsuarioId, materiaId, titulo, fecha, notas);
        if (error != null) AvisoError(error); else Aviso("Examen agregado.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarExamenAsync(int id)
    {
        await _planificador.EliminarExamenAsync(id, UsuarioId);
        Aviso("Examen eliminado.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSesionAsync(int materiaId, int minutos, string? tema)
    {
        string? error = await _planificador.RegistrarSesionAsync(UsuarioId, materiaId, minutos, tema);
        if (error != null) AvisoError(error); else Aviso("Sesión de estudio registrada.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarSesionAsync(int id)
    {
        await _planificador.EliminarSesionAsync(id, UsuarioId);
        Aviso("Sesión eliminada.");
        return RedirectToPage();
    }

    private async Task CargarAsync()
    {
        Materias = await _materias.ListarAsync(UsuarioId);
        Examenes = await _planificador.ProximosExamenesAsync(UsuarioId);
        Sesiones = await _planificador.UltimasSesionesAsync(UsuarioId);
        MinutosSemana = await _planificador.MinutosUltimaSemanaAsync(UsuarioId);
    }

    // Texto para mostrar cuántos días faltan.
    public static string DiasRestantes(DateTime fecha)
    {
        int dias = (int)(fecha.Date - DateTime.UtcNow.Date).TotalDays;
        return dias == 0 ? "¡Hoy!" : dias == 1 ? "Mañana" : "En " + dias + " días";
    }
}
