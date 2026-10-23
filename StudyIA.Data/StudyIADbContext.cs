using Microsoft.EntityFrameworkCore;
using StudyIA.Domain.Entities;

namespace StudyIA.Data;

// Contexto de Entity Framework Core: es el "puente" entre las clases C# y las tablas de SQL Server.
public class StudyIADbContext : DbContext
{
    public StudyIADbContext(DbContextOptions<StudyIADbContext> options) : base(options)
    {
    }

    // Cada DbSet representa una tabla.
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<Material> Materiales => Set<Material>();
    public DbSet<Autoevaluacion> Autoevaluaciones => Set<Autoevaluacion>();
    public DbSet<Pregunta> Preguntas => Set<Pregunta>();
    public DbSet<MensajeChat> Mensajes => Set<MensajeChat>();
    public DbSet<Examen> Examenes => Set<Examen>();
    public DbSet<SesionEstudio> Sesiones => Set<SesionEstudio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // El nombre de usuario no se puede repetir.
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        // Cada mensaje del chat pertenece a un usuario (si se borra el usuario, se borran sus mensajes).
        modelBuilder.Entity<MensajeChat>()
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice para buscar rápido el historial del chat de un usuario.
        modelBuilder.Entity<MensajeChat>()
            .HasIndex(m => m.UsuarioId);

        // Índices para listar rápido los exámenes por fecha y las sesiones de estudio.
        modelBuilder.Entity<Examen>()
            .HasIndex(e => e.Fecha);
        modelBuilder.Entity<SesionEstudio>()
            .HasIndex(s => s.Fecha);
    }
}
