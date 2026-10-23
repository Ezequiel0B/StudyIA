using System.Security.Cryptography;

namespace StudyIA.Services.Seguridad;

// Se encarga de proteger las contraseñas.
// NUNCA se guarda la contraseña tal cual: se guarda un "hash" (PBKDF2 con sal aleatoria).
// Formato guardado: iteraciones.sal.hash (todo en Base64)
public static class ContrasenaHasher
{
    private const int Iteraciones = 100_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    public static string Hash(string contrasena)
    {
        byte[] sal = RandomNumberGenerator.GetBytes(TamanoSal);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
        return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    // Compara la contraseña escrita con el hash guardado en la base de datos.
    public static bool Verificar(string contrasena, string guardado)
    {
        string[] partes = guardado.Split('.');
        if (partes.Length != 3 || !int.TryParse(partes[0], out int iteraciones))
        {
            return false;
        }

        try
        {
            byte[] sal = Convert.FromBase64String(partes[1]);
            byte[] hashGuardado = Convert.FromBase64String(partes[2]);
            byte[] hashNuevo = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, hashGuardado.Length);
            return CryptographicOperations.FixedTimeEquals(hashGuardado, hashNuevo);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
