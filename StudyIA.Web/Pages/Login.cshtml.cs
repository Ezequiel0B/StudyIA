using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudyIA.Services;

namespace StudyIA.Web.Pages;

// Inicio de sesión: valida el formulario, revisa el usuario en la base de datos y crea la cookie.
public class LoginModel : PageModel
{
    private readonly UsuarioService _usuarios;

    public LoginModel(UsuarioService usuarios)
    {
        _usuarios = usuarios;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    // Datos del formulario con sus validaciones.
    public class LoginInput
    {
        [Required(ErrorMessage = "Escribe tu usuario.")]
        [Display(Name = "Usuario")]
        public string NombreUsuario { get; set; } = "";

        [Required(ErrorMessage = "Escribe tu contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; } = "";
    }

    public IActionResult OnGet()
    {
        // Si ya inició sesión, no tiene sentido mostrar el login otra vez.
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Dashboard");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (usuario, error) = await _usuarios.ValidarAsync(Input.NombreUsuario, Input.Contrasena);
        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Usuario o contraseña incorrectos.");
            return Page();
        }

        await SesionHelper.IniciarSesionAsync(HttpContext, usuario);
        return RedirectToPage("/Dashboard");
    }
}
