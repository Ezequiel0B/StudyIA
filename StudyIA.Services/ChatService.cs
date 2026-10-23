using Microsoft.EntityFrameworkCore;
using StudyIA.Data;
using StudyIA.Domain.Entities;
using StudyIA.Services.IA;

namespace StudyIA.Services;

// Lógica del Asistente StudyIA (chatbot): guarda la conversación y le pide la respuesta a la IA.
public class ChatService
{
    public const int MaxCaracteresPregunta = 1000;

    private readonly StudyIADbContext _db;
    private readonly IIaService _ia;
    private readonly MaterialService _materiales;

    public ChatService(StudyIADbContext db, IIaService ia, MaterialService materiales)
    {
        _db = db;
        _ia = ia;
        _materiales = materiales;
    }

    // Últimos mensajes de la conversación, del más antiguo al más nuevo.
    public async Task<List<MensajeChat>> HistorialAsync(int usuarioId, int maximo = 30)
    {
        var recientes = await _db.Mensajes
            .Where(m => m.UsuarioId == usuarioId)
            .OrderByDescending(m => m.Id)
            .Take(maximo)
            .ToListAsync();

        return recientes.OrderBy(m => m.Id).ToList();
    }

    // El estudiante hace una pregunta: se guarda, se consulta a la IA y se guarda la respuesta.
    public async Task<(MensajeChat? Respuesta, string? Error)> PreguntarAsync(int usuarioId, int? materiaId, string pregunta)
    {
        pregunta = pregunta.Trim();
        if (pregunta.Length == 0)
        {
            return (null, "Escribe una pregunta.");
        }
        if (pregunta.Length > MaxCaracteresPregunta)
        {
            return (null, "La pregunta es demasiado larga (máximo " + MaxCaracteresPregunta + " caracteres).");
        }

        // Si se eligió una materia, debe ser del usuario.
        if (materiaId.HasValue)
        {
            bool valida = await _db.Materias.AnyAsync(m => m.Id == materiaId.Value && m.UsuarioId == usuarioId);
            if (!valida)
            {
                materiaId = null;
            }
        }

        var historial = await HistorialAsync(usuarioId, 6);
        string contexto = await _materiales.ContextoAsync(usuarioId, materiaId, 6000);

        var respuestaIa = await _ia.ChatAsync(pregunta, contexto, historial);

        _db.Mensajes.Add(new MensajeChat { UsuarioId = usuarioId, MateriaId = materiaId, Rol = "usuario", Texto = pregunta });
        var respuesta = new MensajeChat
        {
            UsuarioId = usuarioId,
            MateriaId = materiaId,
            Rol = "asistente",
            Texto = respuestaIa.Texto,
            GeneradoConIa = respuestaIa.EsReal
        };
        _db.Mensajes.Add(respuesta);
        await _db.SaveChangesAsync();
        return (respuesta, null);
    }

    // Borra toda la conversación del usuario.
    public async Task LimpiarAsync(int usuarioId)
    {
        var mensajes = await _db.Mensajes.Where(m => m.UsuarioId == usuarioId).ToListAsync();
        _db.Mensajes.RemoveRange(mensajes);
        await _db.SaveChangesAsync();
    }
}
