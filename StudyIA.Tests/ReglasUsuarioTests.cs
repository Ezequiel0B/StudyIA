using StudyIA.Services.Seguridad;
using Xunit;

namespace StudyIA.Tests;

// Pruebas del nombre de usuario y de la fortaleza de la contraseña.
public class ReglasUsuarioTests
{
    [Theory]
    [InlineData("carlos")]
    [InlineData("carlos.cl261896")]
    [InlineData("user_name-1")]
    public void NombreUsuarioValido_ConFormatoCorrecto_DevuelveTrue(string usuario)
    {
        Assert.True(ReglasUsuario.NombreUsuarioValido(usuario));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("con espacios")]
    [InlineData("correo@ejemplo.com")]
    [InlineData("este_nombre_de_usuario_es_demasiado_largo")]
    public void NombreUsuarioValido_ConFormatoIncorrecto_DevuelveFalse(string usuario)
    {
        Assert.False(ReglasUsuario.NombreUsuarioValido(usuario));
    }

    [Fact]
    public void ValidarContrasena_ConContrasenaFuerte_DevuelveNull()
    {
        Assert.Null(ReglasUsuario.ValidarContrasena("Estudiante123*"));
    }

    [Theory]
    [InlineData("corta1A")]
    [InlineData("sinmayuscula123")]
    [InlineData("SINMINUSCULA123")]
    [InlineData("SinNumeroAlguno")]
    public void ValidarContrasena_ConContrasenaDebil_DevuelveMensaje(string contrasena)
    {
        Assert.NotNull(ReglasUsuario.ValidarContrasena(contrasena));
    }
}
