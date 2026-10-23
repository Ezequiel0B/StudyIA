using System.Text.RegularExpressions;

namespace StudyIA.Services.Seguridad;

// Reglas para el nombre de usuario y la contraseña. Están separadas para poder probarlas fácil.
public static class ReglasUsuario
{
    // Entre 3 y 30 caracteres: letras, números, punto, guion y guion bajo.
    private static readonly Regex PatronUsuario = new("^[a-zA-Z0-9._-]{3,30}$", RegexOptions.Compiled);

    public static bool NombreUsuarioValido(string? nombreUsuario)
    {
        return !string.IsNullOrWhiteSpace(nombreUsuario) && PatronUsuario.IsMatch(nombreUsuario);
    }

    // Devuelve null si la contraseña es buena, o el mensaje de qué le falta.
    public static string? ValidarContrasena(string? contrasena)
    {
        if (string.IsNullOrEmpty(contrasena) || contrasena.Length < 8)
        {
            return "La contraseña debe tener al menos 8 caracteres.";
        }
        if (!contrasena.Any(char.IsUpper) || !contrasena.Any(char.IsLower) || !contrasena.Any(char.IsDigit))
        {
            return "La contraseña debe tener mayúscula, minúscula y número.";
        }
        return null;
    }
}
