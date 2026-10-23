using StudyIA.Domain.Entities;
using StudyIA.Services;

namespace StudyIA.Web.Pages;

// Dashboard: resumen de las materias del estudiante y su nivel de dominio.
public class DashboardModel : PaginaBase
{
    private readonly MateriaService _materias;
    private readonly PlanificadorService _planificador;

    public DashboardModel(MateriaService materias, PlanificadorService planificador)
    {
        _materias = materias;
        _planificador = planificador;
    }

    public List<Examen> ProximosExamenes { get; set; } = new();
    public int MinutosSemana { get; set; }

    public List<MateriaConDominio> Materias { get; set; } = new();

    public int TotalMateriales => Materias.Sum(m => m.CantidadMateriales);

    // Promedio del dominio de las materias que ya tienen autoevaluaciones terminadas.
    public int DominioGeneral
    {
        get
        {
            var conEvaluaciones = Materias.Where(m => m.CantidadEvaluaciones > 0).ToList();
            if (conEvaluaciones.Count == 0)
            {
                return 0;
            }
            return (int)Math.Round(conEvaluaciones.Average(m => (double)m.Dominio));
        }
    }

    // La materia con menor dominio (entre las que tienen material) es la que se recomienda reforzar.
    public MateriaConDominio? Sugerida =>
        Materias.Where(m => m.CantidadMateriales > 0).OrderBy(m => m.Dominio).FirstOrDefault();

    public async Task OnGetAsync()
    {
        Materias = await _materias.ListarConDominioAsync(UsuarioId);
        ProximosExamenes = await _planificador.ProximosExamenesAsync(UsuarioId, 3);
        MinutosSemana = await _planificador.MinutosUltimaSemanaAsync(UsuarioId);
    }
}
