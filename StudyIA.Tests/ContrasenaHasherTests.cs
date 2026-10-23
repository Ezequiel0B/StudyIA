using StudyIA.Services.Seguridad;
using Xunit;

namespace StudyIA.Tests;

// Pruebas de la seguridad de contraseñas.
public class ContrasenaHasherTests
{
    [Fact]
    public void Hash_NoGuardaLaContrasenaEnTextoPlano()
    {
        string hash = ContrasenaHasher.Hash("MiClave123*");
        Assert.DoesNotContain("MiClave123*", hash);
    }

    [Fact]
    public void Verificar_ConContrasenaCorrecta_DevuelveTrue()
    {
        string hash = ContrasenaHasher.Hash("MiClave123*");
        Assert.True(ContrasenaHasher.Verificar("MiClave123*", hash));
    }

    [Fact]
    public void Verificar_ConContrasenaIncorrecta_DevuelveFalse()
    {
        string hash = ContrasenaHasher.Hash("MiClave123*");
        Assert.False(ContrasenaHasher.Verificar("otraClave", hash));
    }

    [Fact]
    public void Hash_DeLaMismaContrasena_GeneraValoresDistintosPorLaSal()
    {
        string hash1 = ContrasenaHasher.Hash("MiClave123*");
        string hash2 = ContrasenaHasher.Hash("MiClave123*");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verificar_ConHashInvalido_DevuelveFalseSinFallar()
    {
        Assert.False(ContrasenaHasher.Verificar("abc", "esto-no-es-un-hash"));
    }
}
