using SistemaInscripcionesAcademicas.Api.Exceptions;
using SistemaInscripcionesAcademicas.Api.Repositories;

using Microsoft.EntityFrameworkCore;
using SistemaInscripcionesAcademicas.Api.Data;
using SistemaInscripcionesAcademicas.Api.DTOs.Inscripciones;
using SistemaInscripcionesAcademicas.Api.Models;
using SistemaInscripcionesAcademicas.Api.Services;
using Xunit;

namespace SistemaInscripcionesAcademicas.Tests;

public class InscripcionServiceTests
{
    private static async Task<ApplicationDbContext> CrearContextoAsync()
    {
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:").Options;
        var contexto = new ApplicationDbContext(opciones);
        await contexto.Database.OpenConnectionAsync();
        await contexto.Database.EnsureCreatedAsync();
        return contexto;
    }

    [Fact]
    public async Task InscribirCuartaMateria_DebeRechazarInscripcion()
    {
        // ARRANGE: preparar una base de datos de prueba

        await using var context = await CrearContextoAsync();

        var usuario = new Usuario
        {
            Id = "usuario-1",
            Nombre = "Juan Pérez",
            UserName = "juan@prueba.com",
            Email = "juan@prueba.com"
        };

        var estudiante = new Estudiante
        {
            Id = 1,
            UsuarioId = usuario.Id,
            Usuario = usuario,
            Activo = true
        };

        context.Estudiantes.Add(estudiante);

        // Crear cuatro profesores diferentes
        for (int i = 1; i <= 4; i++)
        {
            context.Profesores.Add(new Profesor
            {
                Id = i,
                Nombre = $"Profesor {i}",
                Activo = true
            });
        }

        // Crear cuatro materias de tres créditos
        for (int i = 1; i <= 4; i++)
        {
            context.Materias.Add(new Materia
            {
                Id = i,
                Nombre = $"Materia {i}",
                Creditos = 3,
                ProfesorId = i,
                Activo = true
            });
        }

        // Juan ya tiene tres materias inscritas
        for (int i = 1; i <= 3; i++)
        {
            context.Inscripciones.Add(new Inscripcion
            {
                Id = i,
                EstudianteId = estudiante.Id,
                MateriaId = i,
                FechaInscripcion = DateTime.UtcNow,
                Activa = true
            });
        }

        await context.SaveChangesAsync();

        var service = new InscripcionService(new EstudianteRepository(context),
            new MateriaRepository(context), new InscripcionRepository(context), new UnidadTrabajo(context));

        var nuevaInscripcion = new CrearInscripcionDto
        {
            MateriaId = 4
        };

        // ACT: intentar inscribir la cuarta materia

        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => service.InscribirAsync(estudiante.Id, nuevaInscripcion)
        );

        // ASSERT: verificar que fue rechazada

        Assert.Equal(
            "No puede inscribir más de tres materias.",
            excepcion.Message
        );

        var totalInscripciones = await context.Inscripciones
            .CountAsync(i => i.EstudianteId == estudiante.Id && i.Activa);

        Assert.Equal(3, totalInscripciones);
    }


    [Fact]
    public async Task InscribirMateriaDuplicada_DebeRechazarInscripcion()
    {
        // ARRANGE: crear una base de datos independiente
        await using var context = await CrearContextoAsync();

        var usuario = new Usuario
        {
            Id = "usuario-1",
            Nombre = "Juan Pérez",
            UserName = "juan@prueba.com",
            Email = "juan@prueba.com"
        };

        var estudiante = new Estudiante
        {
            Id = 1,
            UsuarioId = usuario.Id,
            Usuario = usuario,
            Activo = true
        };

        var profesor = new Profesor
        {
            Id = 1,
            Nombre = "Profesor Matemáticas",
            Activo = true
        };

        var materia = new Materia
        {
            Id = 1,
            Nombre = "Matemáticas",
            Creditos = 3,
            ProfesorId = profesor.Id,
            Profesor = profesor,
            Activo = true
        };

        var inscripcionExistente = new Inscripcion
        {
            Id = 1,
            EstudianteId = estudiante.Id,
            MateriaId = materia.Id,
            FechaInscripcion = DateTime.UtcNow,
            Activa = true
        };

        context.Estudiantes.Add(estudiante);
        context.Materias.Add(materia);
        context.Inscripciones.Add(inscripcionExistente);

        await context.SaveChangesAsync();

        var service = new InscripcionService(new EstudianteRepository(context),
            new MateriaRepository(context), new InscripcionRepository(context), new UnidadTrabajo(context));

        var nuevaInscripcion = new CrearInscripcionDto
        {
            MateriaId = 1
        };

        // ACT: intentar inscribir nuevamente Matemáticas
        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => service.InscribirAsync(estudiante.Id, nuevaInscripcion)
        );

        // ASSERT: verificar el motivo del rechazo
        Assert.Equal(
            "Ya está inscrito en esta materia.",
            excepcion.Message
        );

        // Verificar que no se creó otra inscripción activa
        var totalInscripciones = await context.Inscripciones
            .CountAsync(i =>
                i.EstudianteId == estudiante.Id &&
                i.MateriaId == materia.Id &&
                i.Activa);

        Assert.Equal(1, totalInscripciones);
    }


    [Fact]
    public async Task InscribirMateriasDelMismoProfesor_DebeRechazarInscripcion()
    {
        // ARRANGE
        await using var context = await CrearContextoAsync();

        var usuario = new Usuario
        {
            Id = "usuario-1",
            Nombre = "Juan Pérez",
            UserName = "juan@prueba.com",
            Email = "juan@prueba.com"
        };

        var estudiante = new Estudiante
        {
            Id = 1,
            UsuarioId = usuario.Id,
            Usuario = usuario,
            Activo = true
        };

        var profesor = new Profesor
        {
            Id = 1,
            Nombre = "Carlos Pérez",
            Activo = true
        };

        // Dos materias diferentes del mismo profesor
        var matematicas = new Materia
        {
            Id = 1,
            Nombre = "Matemáticas",
            Creditos = 3,
            ProfesorId = profesor.Id,
            Profesor = profesor,
            Activo = true
        };

        var estadistica = new Materia
        {
            Id = 2,
            Nombre = "Estadística",
            Creditos = 3,
            ProfesorId = profesor.Id,
            Profesor = profesor,
            Activo = true
        };

        // Juan ya está inscrito en Matemáticas
        var inscripcionExistente = new Inscripcion
        {
            Id = 1,
            EstudianteId = estudiante.Id,
            MateriaId = matematicas.Id,
            FechaInscripcion = DateTime.UtcNow,
            Activa = true
        };

        context.Estudiantes.Add(estudiante);
        context.Materias.AddRange(matematicas, estadistica);
        context.Inscripciones.Add(inscripcionExistente);

        await context.SaveChangesAsync();

        var service = new InscripcionService(new EstudianteRepository(context),
            new MateriaRepository(context), new InscripcionRepository(context), new UnidadTrabajo(context));

        var nuevaInscripcion = new CrearInscripcionDto
        {
            MateriaId = estadistica.Id
        };

        // ACT: intentar inscribir Estadística
        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => service.InscribirAsync(estudiante.Id, nuevaInscripcion)
        );

        // ASSERT: comprobar el rechazo
        Assert.Equal(
            "No puede inscribir dos materias del mismo profesor.",
            excepcion.Message
        );

        // Comprobar que solo permanece la primera inscripción
        var totalInscripciones = await context.Inscripciones
            .CountAsync(i =>
                i.EstudianteId == estudiante.Id &&
                i.Activa);

        Assert.Equal(1, totalInscripciones);
    }


    [Fact]
    public async Task InscribirMateriaValida_DebeRegistrarInscripcion()
    {
        // ARRANGE: preparar la base de datos de prueba
        await using var context = await CrearContextoAsync();

        var usuario = new Usuario
        {
            Id = "usuario-1",
            Nombre = "Juan Pérez",
            UserName = "juan@prueba.com",
            Email = "juan@prueba.com"
        };

        var estudiante = new Estudiante
        {
            Id = 1,
            UsuarioId = usuario.Id,
            Usuario = usuario,
            Activo = true
        };

        var profesor = new Profesor
        {
            Id = 1,
            Nombre = "Carlos Pérez",
            Activo = true
        };

        var materia = new Materia
        {
            Id = 1,
            Nombre = "Matemáticas",
            Creditos = 3,
            ProfesorId = profesor.Id,
            Profesor = profesor,
            Activo = true
        };

        context.Estudiantes.Add(estudiante);
        context.Materias.Add(materia);

        await context.SaveChangesAsync();

        var service = new InscripcionService(new EstudianteRepository(context),
            new MateriaRepository(context), new InscripcionRepository(context), new UnidadTrabajo(context));

        var dto = new CrearInscripcionDto
        {
            MateriaId = materia.Id
        };

        // ACT: realizar la inscripción
        var resultado = await service.InscribirAsync(
            estudiante.Id,
            dto
        );

        // ASSERT: comprobar la respuesta
        Assert.Equal(materia.Id, resultado.MateriaId);
        Assert.Equal("Matemáticas", resultado.Materia);
        Assert.Equal("Carlos Pérez", resultado.Profesor);
        Assert.Equal(3, resultado.Creditos);
        Assert.True(resultado.Activa);

        // Comprobar que se guardó en la base de datos
        var inscripcionGuardada = await context.Inscripciones
            .SingleAsync();

        Assert.Equal(estudiante.Id, inscripcionGuardada.EstudianteId);
        Assert.Equal(materia.Id, inscripcionGuardada.MateriaId);
        Assert.True(inscripcionGuardada.Activa);
    }


    [Fact]
    public async Task CancelarInscripcion_DebeLiberarCupo()
    {
        // ARRANGE
        await using var context = await CrearContextoAsync();

        var usuario = new Usuario
        {
            Id = "usuario-1",
            Nombre = "Juan Pérez",
            UserName = "juan@prueba.com",
            Email = "juan@prueba.com"
        };

        var estudiante = new Estudiante
        {
            Id = 1,
            UsuarioId = usuario.Id,
            Usuario = usuario,
            Activo = true
        };

        context.Estudiantes.Add(estudiante);

        // Cuatro profesores diferentes
        for (int i = 1; i <= 4; i++)
        {
            context.Profesores.Add(new Profesor
            {
                Id = i,
                Nombre = $"Profesor {i}",
                Activo = true
            });
        }

        // Cuatro materias, cada una con un profesor diferente
        for (int i = 1; i <= 4; i++)
        {
            context.Materias.Add(new Materia
            {
                Id = i,
                Nombre = $"Materia {i}",
                Creditos = 3,
                ProfesorId = i,
                Activo = true
            });
        }

        // Juan tiene tres inscripciones activas
        for (int i = 1; i <= 3; i++)
        {
            context.Inscripciones.Add(new Inscripcion
            {
                Id = i,
                EstudianteId = estudiante.Id,
                MateriaId = i,
                FechaInscripcion = DateTime.UtcNow,
                Activa = true
            });
        }

        await context.SaveChangesAsync();

        var service = new InscripcionService(new EstudianteRepository(context),
            new MateriaRepository(context), new InscripcionRepository(context), new UnidadTrabajo(context));

        // ACT 1: cancelar la primera inscripción
        bool cancelada = await service.CancelarAsync(
            estudiante.Id,
            1
        );

        // ASSERT 1: comprobar que quedó inactiva
        Assert.True(cancelada);

        var inscripcionCancelada = await context.Inscripciones
            .SingleAsync(i => i.Id == 1);

        Assert.False(inscripcionCancelada.Activa);

        var cuposOcupados = await context.Inscripciones
            .CountAsync(i =>
                i.EstudianteId == estudiante.Id &&
                i.Activa);

        Assert.Equal(2, cuposOcupados);

        // ACT 2: inscribir una nueva materia
        var nuevaInscripcion = await service.InscribirAsync(
            estudiante.Id,
            new CrearInscripcionDto
            {
                MateriaId = 4
            }
        );

        // ASSERT 2: la nueva inscripción fue exitosa
        Assert.Equal(4, nuevaInscripcion.MateriaId);
        Assert.True(nuevaInscripcion.Activa);

        // Juan debe tener nuevamente tres materias activas
        var totalActivas = await context.Inscripciones
            .CountAsync(i =>
                i.EstudianteId == estudiante.Id &&
                i.Activa);

        Assert.Equal(3, totalActivas);

        // El historial debe conservar las cuatro inscripciones
        var totalHistorico = await context.Inscripciones
            .CountAsync(i =>
                i.EstudianteId == estudiante.Id);

        Assert.Equal(4, totalHistorico);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public async Task InscribirConRegistroInactivo_DebeRechazarInscripcion(
        bool estudianteActivo,
        bool materiaActiva,
        bool profesorActivo)
    {
        // ARRANGE
        await using var context = await CrearContextoAsync();

        var usuario = new Usuario
        {
            Id = "usuario-1",
            Nombre = "Juan Pérez",
            UserName = "juan@prueba.com",
            Email = "juan@prueba.com"
        };

        var estudiante = new Estudiante
        {
            Id = 1,
            UsuarioId = usuario.Id,
            Usuario = usuario,
            Activo = estudianteActivo
        };

        var profesor = new Profesor
        {
            Id = 1,
            Nombre = "Carlos Pérez",
            Activo = profesorActivo
        };

        var materia = new Materia
        {
            Id = 1,
            Nombre = "Matemáticas",
            Creditos = 3,
            ProfesorId = profesor.Id,
            Profesor = profesor,
            Activo = materiaActiva
        };

        context.Estudiantes.Add(estudiante);
        context.Materias.Add(materia);

        await context.SaveChangesAsync();

        var service = new InscripcionService(new EstudianteRepository(context),
            new MateriaRepository(context), new InscripcionRepository(context), new UnidadTrabajo(context));

        var dto = new CrearInscripcionDto
        {
            MateriaId = materia.Id
        };

        // ACT
        var excepcion = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => service.InscribirAsync(estudiante.Id, dto)
        );

        // ASSERT
        var mensajeEsperado = !estudianteActivo
            ? "El estudiante no existe o está inactivo."
            : "La materia no está disponible.";

        Assert.Equal(mensajeEsperado, excepcion.Message);

        var totalInscripciones = await context.Inscripciones.CountAsync();

        Assert.Equal(0, totalInscripciones);
    }

}
