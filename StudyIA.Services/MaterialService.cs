using System.Text;
using Microsoft.EntityFrameworkCore;
using StudyIA.Data;
using StudyIA.Domain.Entities;

namespace StudyIA.Services;

// CRUD de materiales de estudio (apuntes).
public class MaterialService
{
    public const int MinCaracteres = 50;
    public const int MaxCaracteres = 100_000;

    private readonly StudyIADbContext _db;

    public MaterialService(StudyIADbContext db)
    {
        _db = db;
    }

    // READ (lista): si se indica materiaId, solo los de esa materia.
    public async Task<List<Material>> ListarAsync(int usuarioId, int? materiaId = null)
    {
        var consulta = _db.Materiales
            .Include(m => m.Materia)
            .Where(m => m.Materia!.UsuarioId == usuarioId);

        if (materiaId.HasValue)
        {
            consulta = consulta.Where(m => m.MateriaId == materiaId.Value);
        }

        return await consulta.OrderByDescending(m => m.FechaSubida).ToListAsync();
    }

    // READ (uno)
    public async Task<Material?> ObtenerAsync(int id, int usuarioId)
    {
        return await _db.Materiales
            .Include(m => m.Materia)
            .FirstOrDefaultAsync(m => m.Id == id && m.Materia!.UsuarioId == usuarioId);
    }

    // CREATE
    public async Task<(Material? Material, string? Error)> CrearAsync(int usuarioId, int materiaId, string titulo, string contenido)
    {
        string? error = await ValidarAsync(usuarioId, materiaId, titulo, contenido);
        if (error != null)
        {
            return (null, error);
        }

        var material = new Material
        {
            MateriaId = materiaId,
            Titulo = titulo.Trim(),
            Contenido = contenido.Trim()
        };
        _db.Materiales.Add(material);
        await _db.SaveChangesAsync();
        return (material, null);
    }

    // UPDATE
    public async Task<string?> ActualizarAsync(int id, int usuarioId, int materiaId, string titulo, string contenido)
    {
        var material = await ObtenerAsync(id, usuarioId);
        if (material == null)
        {
            return "El material no existe.";
        }

        string? error = await ValidarAsync(usuarioId, materiaId, titulo, contenido);
        if (error != null)
        {
            return error;
        }

        material.MateriaId = materiaId;
        material.Titulo = titulo.Trim();
        // Si el texto cambió, el resumen anterior ya no sirve.
        if (material.Contenido != contenido.Trim())
        {
            material.Resumen = null;
        }
        material.Contenido = contenido.Trim();
        await _db.SaveChangesAsync();
        return null;
    }

    // DELETE
    public async Task<bool> EliminarAsync(int id, int usuarioId)
    {
        var material = await ObtenerAsync(id, usuarioId);
        if (material == null)
        {
            return false;
        }
        _db.Materiales.Remove(material);
        await _db.SaveChangesAsync();
        return true;
    }

    // Guarda el resumen que generó la IA.
    public async Task<bool> GuardarResumenAsync(int id, int usuarioId, string resumen)
    {
        var material = await ObtenerAsync(id, usuarioId);
        if (material == null)
        {
            return false;
        }
        material.Resumen = resumen;
        await _db.SaveChangesAsync();
        return true;
    }

    // Junta el texto de los materiales (de una materia o de todas) para dárselo a la IA como contexto.
    public async Task<string> ContextoAsync(int usuarioId, int? materiaId, int maxCaracteres)
    {
        var consulta = _db.Materiales.Where(m => m.Materia!.UsuarioId == usuarioId);
        if (materiaId.HasValue)
        {
            consulta = consulta.Where(m => m.MateriaId == materiaId.Value);
        }

        var materiales = await consulta.OrderByDescending(m => m.FechaSubida).ToListAsync();

        var texto = new StringBuilder();
        foreach (var material in materiales)
        {
            if (texto.Length >= maxCaracteres)
            {
                break;
            }
            texto.AppendLine("## " + material.Titulo);
            texto.AppendLine(material.Contenido);
            texto.AppendLine();
        }

        string resultado = texto.ToString().Trim();
        return resultado.Length <= maxCaracteres ? resultado : resultado.Substring(0, maxCaracteres);
    }

    // Validaciones comunes de crear y actualizar.
    private async Task<string?> ValidarAsync(int usuarioId, int materiaId, string titulo, string contenido)
    {
        if (string.IsNullOrWhiteSpace(titulo))
        {
            return "El título es obligatorio.";
        }
        if (contenido.Trim().Length < MinCaracteres)
        {
            return "El contenido es muy corto (mínimo " + MinCaracteres + " caracteres) para que la IA pueda usarlo.";
        }
        if (contenido.Length > MaxCaracteres)
        {
            return "El contenido es demasiado largo (máximo " + MaxCaracteres + " caracteres).";
        }

        // La materia debe existir y ser del usuario.
        bool materiaValida = await _db.Materias.AnyAsync(m => m.Id == materiaId && m.UsuarioId == usuarioId);
        return materiaValida ? null : "La materia seleccionada no es válida.";
    }
}
