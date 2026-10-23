using Microsoft.EntityFrameworkCore;
using StudyIA.Data;
using StudyIA.Domain.Entities;
using StudyIA.Services.Seguridad;

namespace StudyIA.Services;

// Lógica de negocio de los usuarios: registro, inicio de sesión y cambio de contraseña.
public class UsuarioService
{
    public const int MaxIntentos = 5;
    public const int MinutosBloqueo = 10;

    private readonly StudyIADbContext _db;

    public UsuarioService(StudyIADbContext db)
    {
        _db = db;
    }

    // Crea una cuenta nueva. Si algo está mal, devuelve el mensaje de error.
    public async Task<(Usuario? Usuario, string? Error)> RegistrarAsync(string nombre, string nombreUsuario, string contrasena)
    {
        nombre = nombre.Trim();
        nombreUsuario = nombreUsuario.Trim().ToLowerInvariant();

        if (nombre.Length < 2)
        {
            return (null, "El nombre es demasiado corto.");
        }
        if (!ReglasUsuario.NombreUsuarioValido(nombreUsuario))
        {
            return (null, "El usuario debe tener de 3 a 30 caracteres: letras, números, punto, guion o guion bajo.");
        }
        string? errorContrasena = ReglasUsuario.ValidarContrasena(contrasena);
        if (errorContrasena != null)
        {
            return (null, errorContrasena);
        }

        // Regla de negocio: no se permiten dos cuentas con el mismo nombre de usuario.
        bool existe = await _db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario);
        if (existe)
        {
            return (null, "Ese nombre de usuario ya está en uso.");
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            NombreUsuario = nombreUsuario,
            ContrasenaHash = ContrasenaHasher.Hash(contrasena)
        };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        return (usuario, null);
    }

    // Revisa usuario y contraseña. Si son correctos devuelve el usuario; si no, un mensaje de error.
    // Después de 5 intentos fallidos la cuenta se bloquea 10 minutos.
    public async Task<(Usuario? Usuario, string? Error)> ValidarAsync(string nombreUsuario, string contrasena)
    {
        const string errorGeneral = "Usuario o contraseña incorrectos.";
        nombreUsuario = nombreUsuario.Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
        if (usuario == null)
        {
            return (null, errorGeneral);
        }

        if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.UtcNow)
        {
            int minutos = (int)Math.Ceiling((usuario.BloqueadoHasta.Value - DateTime.UtcNow).TotalMinutes);
            return (null, "Cuenta bloqueada por muchos intentos. Intenta de nuevo en " + minutos + " minuto(s).");
        }

        if (!ContrasenaHasher.Verificar(contrasena, usuario.ContrasenaHash))
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= MaxIntentos)
            {
                usuario.BloqueadoHasta = DateTime.UtcNow.AddMinutes(MinutosBloqueo);
                usuario.IntentosFallidos = 0;
            }
            await _db.SaveChangesAsync();
            return (null, errorGeneral);
        }

        // Login correcto: se limpian los intentos fallidos.
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        await _db.SaveChangesAsync();
        return (usuario, null);
    }

    // Cambia la contraseña. Devuelve null si todo salió bien, o el mensaje de error.
    public async Task<string?> CambiarContrasenaAsync(int usuarioId, string actual, string nueva)
    {
        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
        {
            return "El usuario no existe.";
        }
        if (!ContrasenaHasher.Verificar(actual, usuario.ContrasenaHash))
        {
            return "La contraseña actual no es correcta.";
        }
        string? error = ReglasUsuario.ValidarContrasena(nueva);
        if (error != null)
        {
            return error;
        }

        usuario.ContrasenaHash = ContrasenaHasher.Hash(nueva);
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<Usuario?> ObtenerAsync(int usuarioId)
    {
        return await _db.Usuarios.FindAsync(usuarioId);
    }
}
