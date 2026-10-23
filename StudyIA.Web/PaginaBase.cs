using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StudyIA.Web;

// Clase base de las páginas que requieren sesión iniciada.
// Evita repetir en cada página el código para saber quién es el usuario actual.
public abstract class PaginaBase : PageModel
{
    // Id del usuario que inició sesión (se guarda en la cookie al hacer login).
    protected int UsuarioId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    // Mensaje verde que se muestra una vez en la siguiente página.
    protected void Aviso(string mensaje) => TempData["Mensaje"] = mensaje;

    // Mensaje rojo que se muestra una vez en la siguiente página.
    protected void AvisoError(string mensaje) => TempData["MensajeError"] = mensaje;
}
