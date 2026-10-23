using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudyIA.Data;
using StudyIA.Domain.Entities;
using StudyIA.Services.Seguridad;

namespace StudyIA.Services;

// Crea la base de datos (si no existe) y carga datos de ejemplo la primera vez.
// Usuario de prueba:  estudiante  /  Estudiante123*
public static class DatosIniciales
{
    public static async Task InicializarAsync(StudyIADbContext db, ILogger logger, int intentos = 60)
    {
        // Cuando se usa Docker, SQL Server tarda unos segundos en arrancar: se reintenta hasta que responda.
        for (int i = 1; i <= intentos; i++)
        {
            try
            {
                // Si el proyecto ya tiene migraciones se aplican; si no, se crean las tablas directo.
                if (db.Database.GetMigrations().Any())
                {
                    await db.Database.MigrateAsync();
                }
                else
                {
                    await db.Database.EnsureCreatedAsync();
                }
                break;
            }
            catch (Exception ex) when (i < intentos)
            {
                logger.LogWarning("La base de datos aún no está lista (intento {Intento} de {Total}): {Mensaje}", i, intentos, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        if (await db.Usuarios.AnyAsync())
        {
            return;
        }

        logger.LogInformation("Base de datos vacía: se cargan los datos de ejemplo.");

        var demo = new Usuario
        {
            Nombre = "Estudiante Demo",
            NombreUsuario = "estudiante",
            ContrasenaHash = ContrasenaHasher.Hash("Estudiante123*")
        };

        var baseDatos = new Materia { Nombre = "Base de Datos", Descripcion = "Modelo relacional, SQL y normalización" };
        var programacion = new Materia { Nombre = "Programación II", Descripcion = "Programación orientada a objetos" };
        var matematica = new Materia { Nombre = "Matemática IV", Descripcion = "Cálculo diferencial" };

        // Las materias se agregan a la lista del usuario: así Entity Framework las guarda junto con él.
        demo.Materias.Add(baseDatos);
        demo.Materias.Add(programacion);
        demo.Materias.Add(matematica);

        baseDatos.Materiales.Add(new Material
        {
            Titulo = "Guía de normalización",
            Contenido =
                "La normalización es el proceso de organizar las tablas de una base de datos para reducir la redundancia de datos. " +
                "La primera forma normal exige que cada columna contenga valores atómicos y que no existan grupos repetidos. " +
                "La segunda forma normal exige que todos los atributos dependan de la clave primaria completa. " +
                "La tercera forma normal elimina las dependencias transitivas, es decir, atributos que dependen de otros atributos que no son la clave. " +
                "Una clave foránea es una columna que referencia la clave primaria de otra tabla y permite mantener la integridad referencial. " +
                "Una relación de uno a muchos se implementa colocando la clave foránea en la tabla del lado muchos."
        });
        baseDatos.Materiales.Add(new Material
        {
            Titulo = "Apuntes de SQL",
            Contenido =
                "La sentencia SELECT permite consultar los datos de una o varias tablas. " +
                "La cláusula WHERE filtra los registros que cumplen una condición. " +
                "La instrucción INSERT agrega nuevos registros y la instrucción UPDATE modifica registros existentes. " +
                "La sentencia DELETE elimina registros de una tabla y debe usarse con una condición para no borrar todo. " +
                "La cláusula JOIN combina filas de dos tablas relacionadas mediante una columna en común. " +
                "Las funciones de agregación como COUNT, SUM y AVG calculan valores a partir de un conjunto de filas."
        });

        programacion.Materiales.Add(new Material
        {
            Titulo = "Programación orientada a objetos",
            Contenido =
                "La programación orientada a objetos organiza el código en clases que representan entidades del problema. " +
                "El encapsulamiento consiste en ocultar los datos internos de una clase y exponerlos solo mediante propiedades y métodos. " +
                "La herencia permite que una clase derivada reutilice los atributos y métodos de una clase base. " +
                "El polimorfismo permite que objetos de distintas clases respondan de forma diferente al mismo método. " +
                "Una interfaz define un contrato de métodos que las clases que la implementan están obligadas a cumplir. " +
                "Una clase abstracta no se puede instanciar y sirve como base para otras clases."
        });

        matematica.Materiales.Add(new Material
        {
            Titulo = "Introducción a las derivadas",
            Contenido =
                "La derivada de una función mide la rapidez con la que cambia su valor respecto a su variable independiente. " +
                "Geométricamente, la derivada en un punto es la pendiente de la recta tangente a la gráfica en ese punto. " +
                "La regla de la potencia establece que la derivada de x elevado a n es n multiplicado por x elevado a n menos uno. " +
                "La regla del producto indica cómo derivar la multiplicación de dos funciones. " +
                "La regla de la cadena se usa para derivar funciones compuestas. " +
                "Los puntos críticos de una función se encuentran igualando su derivada a cero."
        });

        // Una autoevaluación ya resuelta, para que el Dashboard muestre el dominio desde el primer momento.
        var ejemplo = new Autoevaluacion
        {
            Materia = baseDatos,
            Nivel = "Intermedio",
            Finalizada = true,
            Correctas = 3,
            Total = 5,
            Fecha = DateTime.UtcNow.AddDays(-2),
            Preguntas =
            {
                Crear("Normalización", "¿Qué forma normal elimina las dependencias transitivas?", "Primera", "Segunda", "Tercera", "Cuarta", "C", "C"),
                Crear("Normalización", "¿Qué exige la primera forma normal?", "Valores atómicos en cada columna", "Dependencias transitivas", "Claves foráneas", "Índices", "A", "A"),
                Crear("Normalización", "La segunda forma normal exige que los atributos dependan de…", "Cualquier columna", "La clave primaria completa", "Una vista", "Un índice", "B", "A"),
                Crear("Claves foráneas", "Una clave foránea sirve para…", "Cifrar datos", "Referenciar la clave primaria de otra tabla", "Ordenar filas", "Eliminar tablas", "B", "B"),
                Crear("SQL básico", "¿Qué cláusula filtra registros?", "ORDER BY", "GROUP BY", "WHERE", "JOIN", "C", "D")
            }
        };

        db.Usuarios.Add(demo);
        db.Autoevaluaciones.Add(ejemplo);
        await db.SaveChangesAsync();
    }

    // Ayuda para crear una pregunta de ejemplo.
    private static Pregunta Crear(string tema, string enunciado, string a, string b, string c, string d, string correcta, string elegida)
    {
        return new Pregunta
        {
            Tema = tema,
            Enunciado = enunciado,
            OpcionA = a,
            OpcionB = b,
            OpcionC = c,
            OpcionD = d,
            RespuestaCorrecta = correcta,
            RespuestaUsuario = elegida,
            Explicacion = "Respuesta correcta: " + correcta + "."
        };
    }
}
