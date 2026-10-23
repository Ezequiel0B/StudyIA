using Microsoft.EntityFrameworkCore;
using StudyIA.Data;
using StudyIA.Domain.Entities;

namespace StudyIA.Services;

// Planificación de exámenes y seguimiento del estudio.
// Igual que en los demás servicios, todo se filtra por usuarioId: cada estudiante ve solo lo suyo.
public class PlanificadorService
{
    private readonly StudyIADbContext _db;

    public PlanificadorService(StudyIADbContext db)
    {
        _db = db;
    }

    // ---------------- Exámenes ----------------

    // Exámenes que todavía no pasaron, del más cercano al más lejano.
    public async Task<List<Examen>> ProximosExamenesAsync(int usuarioId, int cantidad = 20)
    {
        DateTime hoy = DateTime.UtcNow.Date;
        return await _db.Examenes
            .Include(e => e.Materia)
            .Where(e => e.Materia!.UsuarioId == usuarioId && e.Fecha >= hoy)
            .OrderBy(e => e.Fecha)
            .Take(cantidad)
            .ToListAsync();
    }

    public async Task<string?> CrearExamenAsync(int usuarioId, int materiaId, string? titulo, DateTime? fecha, string? notas)
    {
        titulo = titulo?.Trim() ?? "";
        if (titulo.Length == 0 || titulo.Length > 150)
        {
            return "El título del examen es obligatorio (máximo 150 caracteres).";
        }
        if (fecha == null || fecha.Value.Date < DateTime.UtcNow.Date)
        {
            return "Elige una fecha de hoy en adelante.";
        }
        if (!await _db.Materias.AnyAsync(m => m.Id == materiaId && m.UsuarioId == usuarioId))
        {
            return "Selecciona una materia válida.";
        }

        _db.Examenes.Add(new Examen
        {
            MateriaId = materiaId,
            Titulo = titulo,
            Fecha = fecha.Value.Date,
            Notas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim()
        });
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task EliminarExamenAsync(int id, int usuarioId)
    {
        var examen = await _db.Examenes.FirstOrDefaultAsync(e => e.Id == id && e.Materia!.UsuarioId == usuarioId);
        if (examen != null)
        {
            _db.Examenes.Remove(examen);
            await _db.SaveChangesAsync();
        }
    }

    // ---------------- Seguimiento del estudio ----------------

    public async Task<List<SesionEstudio>> UltimasSesionesAsync(int usuarioId, int cantidad = 15)
    {
        return await _db.Sesiones
            .Include(s => s.Materia)
            .Where(s => s.Materia!.UsuarioId == usuarioId)
            .OrderByDescending(s => s.Fecha)
            .Take(cantidad)
            .ToListAsync();
    }

    public async Task<string?> RegistrarSesionAsync(int usuarioId, int materiaId, int minutos, string? tema)
    {
        if (minutos < 5 || minutos > 600)
        {
            return "Los minutos deben estar entre 5 y 600.";
        }
        if (!await _db.Materias.AnyAsync(m => m.Id == materiaId && m.UsuarioId == usuarioId))
        {
            return "Selecciona una materia válida.";
        }

        _db.Sesiones.Add(new SesionEstudio
        {
            MateriaId = materiaId,
            Minutos = minutos,
            Tema = string.IsNullOrWhiteSpace(tema) ? null : tema.Trim()
        });
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task EliminarSesionAsync(int id, int usuarioId)
    {
        var sesion = await _db.Sesiones.FirstOrDefaultAsync(s => s.Id == id && s.Materia!.UsuarioId == usuarioId);
        if (sesion != null)
        {
            _db.Sesiones.Remove(sesion);
            await _db.SaveChangesAsync();
        }
    }

    // Minutos estudiados en los últimos 7 días.
    public async Task<int> MinutosUltimaSemanaAsync(int usuarioId)
    {
        DateTime desde = DateTime.UtcNow.AddDays(-7);
        return await _db.Sesiones
            .Where(s => s.Materia!.UsuarioId == usuarioId && s.Fecha >= desde)
            .SumAsync(s => s.Minutos);
    }
}
