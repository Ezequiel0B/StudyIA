using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StudyIA.Web.Pages;

// Página que se muestra cuando ocurre una excepción no controlada (ver UseExceptionHandler en Program.cs).
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? IdSolicitud { get; set; }

    public void OnGet()
    {
        IdSolicitud = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
