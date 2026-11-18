using Matricula.Core.Entities;
using Matricula.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Matricula.Tests;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public MatriculaContext CreateContext() =>
        new(new DbContextOptionsBuilder<MatriculaContext>().UseNpgsql(_container.GetConnectionString()).Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    // Seeding helpers: every call creates unique rows so tests stay independent.
    public async Task<Shift> AddShiftAsync()
    {
        await using var ctx = CreateContext();
        var shift = new Shift { Name = "Turno-" + Guid.NewGuid() };
        ctx.Shifts.Add(shift);
        await ctx.SaveChangesAsync();
        return shift;
    }

    public async Task<Student> AddStudentAsync(int cycle = 1)
    {
        await using var ctx = CreateContext();
        var student = new Student { FullName = "Alumno-" + Guid.NewGuid(), Cycle = cycle };
        ctx.Students.Add(student);
        await ctx.SaveChangesAsync();
        return student;
    }

    public async Task<ClassRoomCourse> AddCourseAsync(Shift shift, int cycle, string start, string end, int capacity = 10)
    {
        await using var ctx = CreateContext();
        var crc = new ClassRoomCourse
        {
            Course = new Course { Name = "Curso-" + Guid.NewGuid() },
            ClassRoom = new ClassRoom { Name = "Aula-" + Guid.NewGuid() },
            ShiftId = shift.Id,
            Cycle = cycle,
            Capacity = capacity,
            StartTime = TimeSpan.Parse(start),
            EndTime = TimeSpan.Parse(end)
        };
        ctx.ClassRoomCourses.Add(crc);
        await ctx.SaveChangesAsync();
        return crc;
    }
}

[CollectionDefinition("postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>;
