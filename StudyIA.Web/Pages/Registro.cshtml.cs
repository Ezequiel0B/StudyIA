using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudyIA.Services;

namespace StudyIA.Web.Pages;

// Registro de usuarios nuevos.
public class RegistroModel : PageModel
{
    private readonly UsuarioService _usuarios;

    public RegistroModel(UsuarioService usuarios)
    {
        _usuarios = usuarios;
    }

    [BindProperty]
    public RegistroInput Input { get; set; } = new();

    public class RegistroInput
    {
        [Required(ErrorMessage = "Escribe tu nombre.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "Escribe un nombre de usuario.")]
        [RegularExpression("^[a-zA-Z0-9._-]{3,30}$", ErrorMessage = "Usa de 3 a 30 caracteres: letras, números, punto, guion o guion bajo.")]
        [Display(Name = "Usuario")]
        public string NombreUsuario { get; set; } = "";

        [Required(ErrorMessage = "Escribe una contraseña.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; } = "";

        [Required(ErrorMessage = "Repite la contraseña.")]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string Confirmar { get; set; } = "";
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (usuario, error) = await _usuarios.RegistrarAsync(Input.Nombre, Input.NombreUsuario, Input.Contrasena);
        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, error ?? "No se pudo crear la cuenta.");
            return Page();
        }

        // Al registrarse, el usuario entra directamente a la aplicación.
        await SesionHelper.IniciarSesionAsync(HttpContext, usuario);
        return RedirectToPage("/Dashboard");
    }
}
