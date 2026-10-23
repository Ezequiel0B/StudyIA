// Punto de entrada de StudyIA: aquí se configuran los servicios y el pipeline de la aplicación web.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using StudyIA.Data;
using StudyIA.Services;
using StudyIA.Services.IA;

var builder = WebApplication.CreateBuilder(args);

// Archivo opcional para guardar la clave de la IA en tu equipo (no se sube a Git). Ver appsettings.Local.json.example.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// ---- Páginas Razor y seguridad básica ----
// Todas las páginas requieren iniciar sesión, excepto Login, Registro y Error.
builder.Services.AddRazorPages(opciones =>
{
    opciones.Conventions.AuthorizeFolder("/");
    opciones.Conventions.AllowAnonymousToPage("/Login");
    opciones.Conventions.AllowAnonymousToPage("/Registro");
    opciones.Conventions.AllowAnonymousToPage("/Error");
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/";
        opciones.AccessDeniedPath = "/";
        opciones.Cookie.Name = "StudyIA.Auth";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SameSite = SameSiteMode.Lax;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;

        // Para las rutas /api se responde 401 en vez de redirigir a la pantalla de login.
        opciones.Events.OnRedirectToLogin = contexto =>
        {
            if (contexto.Request.Path.StartsWithSegments("/api"))
            {
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
            else
            {
                contexto.Response.Redirect(contexto.RedirectUri);
            }
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// ---- Base de datos (Entity Framework Core + SQL Server) ----
string cadenaConexion = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection'.");
// La cadena puede apuntar a una base EXTERNA (por ejemplo Azure SQL Database): se cambia con la
// variable de entorno ConnectionStrings__DefaultConnection, sin tocar el código.
builder.Services.AddDbContext<StudyIADbContext>(opciones => opciones.UseSqlServer(cadenaConexion, sql =>
{
    sql.MigrationsAssembly("StudyIA.Data");   // aquí viven las migraciones
    sql.EnableRetryOnFailure(5);              // reintenta si la base en la nube falla un instante
}));

// Detrás de Azure App Service o de un proxy, así se sabe si la petición original era HTTPS.
builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
{
    opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opciones.KnownNetworks.Clear();
    opciones.KnownProxies.Clear();
});

// ---- Servicios de la aplicación (capa de lógica de negocio) ----
builder.Services.Configure<IaOptions>(builder.Configuration.GetSection(IaOptions.Seccion));
builder.Services.AddHttpClient<IIaService, IaService>(cliente => cliente.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<MateriaService>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<AutoevaluacionService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<PlanificadorService>();

var app = builder.Build();

// ---- Crear la base de datos y cargar datos de ejemplo al arrancar ----
using (var alcance = app.Services.CreateScope())
{
    var db = alcance.ServiceProvider.GetRequiredService<StudyIADbContext>();
    var logger = alcance.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatosIniciales.InicializarAsync(db, logger);
}

// ---- Manejo global de errores ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();

// Cabeceras de seguridad básicas para todas las respuestas.
app.Use(async (contexto, siguiente) =>
{
    contexto.Response.Headers["X-Content-Type-Options"] = "nosniff";
    contexto.Response.Headers["X-Frame-Options"] = "DENY";
    contexto.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await siguiente();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// ---- API REST mínima ----
// Sirve para comprobar que la aplicación está viva (por ejemplo, desde Docker o Azure).
app.MapGet("/api/health", () => Results.Ok(new { estado = "ok", fecha = DateTime.UtcNow })).AllowAnonymous();

// Devuelve en JSON las materias del usuario que inició sesión, con su dominio.
app.MapGet("/api/materias", async (ClaimsPrincipal usuario, MateriaService materias) =>
{
    int usuarioId = int.Parse(usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var lista = await materias.ListarConDominioAsync(usuarioId);
    return Results.Ok(lista.Select(m => new
    {
        id = m.Materia.Id,
        nombre = m.Materia.Nombre,
        dominio = m.Dominio,
        materiales = m.CantidadMateriales,
        autoevaluaciones = m.CantidadEvaluaciones
    }));
}).RequireAuthorization();

app.Run();
