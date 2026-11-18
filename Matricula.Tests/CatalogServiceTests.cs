using Matricula.Core.Entities;
using Matricula.Infrastructure.Services;

namespace Matricula.Tests;

[Collection("postgres")]
public class CatalogServiceTests
{
    private readonly PostgresFixture _db;

    public CatalogServiceTests(PostgresFixture db) => _db = db;

    [Fact]
    public async Task DeleteStudentWithEnrollments_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var crc = await _db.AddCourseAsync(shift, 1, "08:30", "09:30");
        await using (var ctx = _db.CreateContext())
            Assert.True((await new EnrollmentService(ctx).EnrollAsync(student.Id, shift.Id, [crc.Id], "2026-II")).Success);

        await using var ctx2 = _db.CreateContext();
        var result = await new CatalogService(ctx2).DeleteStudentAsync(student.Id);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeleteTeacherWithSchedules_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var teacher = await _db.AddTeacherAsync();
        await _db.AddCourseAsync(shift, 1, "08:30", "09:30", teacher: teacher);

        await using var ctx = _db.CreateContext();
        var result = await new CatalogService(ctx).DeleteTeacherAsync(teacher.Id);
        Assert.False(result.Success);
        Assert.Contains("desactívelo", result.Error);
    }

    [Fact]
    public async Task DuplicateCourseName_CaseAndSpaces_Fails()
    {
        var name = "Algebra-" + Guid.NewGuid();
        await using (var ctx = _db.CreateContext())
            Assert.True((await new CatalogService(ctx).SaveCourseAsync(new Course { Name = name })).Success);

        await using var ctx2 = _db.CreateContext();
        var result = await new CatalogService(ctx2).SaveCourseAsync(new Course { Name = "  " + name.ToUpper() + " " });
        Assert.False(result.Success);
    }
}
