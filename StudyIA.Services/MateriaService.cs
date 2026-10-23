using Microsoft.EntityFrameworkCore;
using StudyIA.Data;
using StudyIA.Domain.Entities;

namespace StudyIA.Services;

// Datos que necesita el Dashboard por cada materia.
public record MateriaConDominio(Materia Materia, int Dominio, int CantidadMateriales, int CantidadEvaluaciones);

// CRUD de materias. Todas las consultas filtran por usuarioId para que
// cada estudiante solo vea y modifique SUS materias.
public class MateriaService
{
    private readonly StudyIADbContext _db;

    public MateriaService(StudyIADbContext db)
    {
        _db = db;
    }

    // READ (lista)
    public async Task<List<Materia>> ListarAsync(int usuarioId)
    {
        return await _db.Materias
            .Where(m => m.UsuarioId == usuarioId)
            .OrderBy(m => m.Nombre)
            .ToListAsync();
    }

    // READ (una)
    public async Task<Materia?> ObtenerAsync(int id, int usuarioId)
    {
        return await _db.Materias.FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId);
    }

    // CREATE
    public async Task<(Materia? Materia, string? Error)> CrearAsync(int usuarioId, string nombre, string? descripcion)
    {
        nombre = nombre.Trim();
        if (nombre.Length == 0)
        {
            return (null, "El nombre de la materia es obligatorio.");
        }

        // Regla de negocio: el estudiante no puede tener dos materias con el mismo nombre.
        bool repetida = await _db.Materias.AnyAsync(m => m.UsuarioId == usuarioId && m.Nombre == nombre);
        if (repetida)
        {
            return (null, "Ya tienes una materia con ese nombre.");
        }

        var materia = new Materia
        {
            UsuarioId = usuarioId,
            Nombre = nombre,
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim()
        };
        _db.Materias.Add(materia);
        await _db.SaveChangesAsync();
        return (materia, null);
    }

    // UPDATE
    public async Task<string?> ActualizarAsync(int id, int usuarioId, string nombre, string? descripcion)
    {
        var materia = await ObtenerAsync(id, usuarioId);
        if (materia == null)
        {
            return "La materia no existe.";
        }

        nombre = nombre.Trim();
        if (nombre.Length == 0)
        {
            return "El nombre de la materia es obligatorio.";
        }

        bool repetida = await _db.Materias.AnyAsync(m => m.UsuarioId == usuarioId && m.Nombre == nombre && m.Id != id);
        if (repetida)
        {
            return "Ya tienes otra materia con ese nombre.";
        }

        materia.Nombre = nombre;
        materia.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        await _db.SaveChangesAsync();
        return null;
    }

    // DELETE (también se borran sus materiales y autoevaluaciones, por la relación en cascada)
    public async Task<bool> EliminarAsync(int id, int usuarioId)
    {
        var materia = await ObtenerAsync(id, usuarioId);
        if (materia == null)
        {
            return false;
        }
        _db.Materias.Remove(materia);
        await _db.SaveChangesAsync();
        return true;
    }

    // Para el Dashboard: materias con su nivel de dominio calculado.
    public async Task<List<MateriaConDominio>> ListarConDominioAsync(int usuarioId)
    {
        var materias = await _db.Materias
            .Where(m => m.UsuarioId == usuarioId)
            .Include(m => m.Autoevaluaciones)
            .OrderBy(m => m.Nombre)
            .ToListAsync();

        // Cuántos materiales tiene cada materia.
        var materiales = await _db.Materiales
            .Where(x => x.Materia!.UsuarioId == usuarioId)
            .Select(x => x.MateriaId)
            .ToListAsync();

        return materias
            .Select(m => new MateriaConDominio(
                m,
                Calculos.CalcularDominio(m.Autoevaluaciones),
                materiales.Count(id => id == m.Id),
                m.Autoevaluaciones.Count(a => a.Finalizada)))
            .ToList();
    }
}
