using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StudyIA.Domain.Entities;
using StudyIA.Services;

namespace StudyIA.Web.Pages;

// Perfil del usuario: ver sus datos y cambiar la contraseña.
public class PerfilModel : PaginaBase
{
    private readonly UsuarioService _usuarios;

    public PerfilModel(UsuarioService usuarios)
    {
        _usuarios = usuarios;
    }

    public Usuario? Usuario { get; set; }

    [BindProperty]
    public CambioInput Input { get; set; } = new();

    public class CambioInput
    {
        [Required(ErrorMessage = "Escribe tu contraseña actual.")]
        [DataType(DataType.Password)]
        public string Actual { get; set; } = "";

        [Required(ErrorMessage = "Escribe la nueva contraseña.")]
        [DataType(DataType.Password)]
        public string Nueva { get; set; } = "";

        [Compare("Nueva", ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string Confirmar { get; set; } = "";
    }

    public async Task OnGetAsync()
    {
        Usuario = await _usuarios.ObtenerAsync(UsuarioId);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Usuario = await _usuarios.ObtenerAsync(UsuarioId);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        string? error = await _usuarios.CambiarContrasenaAsync(UsuarioId, Input.Actual, Input.Nueva);
        if (error != null)
        {
            ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        Aviso("Contraseña actualizada.");
        return RedirectToPage("/Perfil");
    }
}
