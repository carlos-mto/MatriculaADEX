using Matricula.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Tests;

[Collection("postgres")]
public class EnrollmentCancelTests
{
    private readonly PostgresFixture _db;

    public EnrollmentCancelTests(PostgresFixture db) => _db = db;

    [Fact]
    public async Task Cancel_RestoresCapacityAndRemovesEnrollmentAndDetails()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var c1 = await _db.AddCourseAsync(shift, 1, "08:30", "09:30", 5);
        var c2 = await _db.AddCourseAsync(shift, 1, "09:30", "10:30", 5);
        await using (var ctx = _db.CreateContext())
            Assert.True((await new EnrollmentService(ctx).EnrollAsync(student.Id, shift.Id, [c1.Id, c2.Id], "2026-II")).Success);

        Guid enrollmentId;
        await using (var ctx = _db.CreateContext())
            enrollmentId = (await ctx.Enrollments.SingleAsync(e => e.StudentId == student.Id)).Id;

        await using (var ctx = _db.CreateContext())
        {
            var result = await new EnrollmentService(ctx).CancelAsync(enrollmentId);
            Assert.True(result.Success, result.Error);
        }

        await using var check = _db.CreateContext();
        Assert.False(await check.Enrollments.AnyAsync(e => e.Id == enrollmentId));
        Assert.False(await check.EnrollmentDetails.AnyAsync(d => d.EnrollmentId == enrollmentId));
        Assert.Equal(5, await check.ClassRoomCourses.Where(c => c.Id == c1.Id).Select(c => c.Capacity).SingleAsync());
        Assert.Equal(5, await check.ClassRoomCourses.Where(c => c.Id == c2.Id).Select(c => c.Capacity).SingleAsync());
    }

    [Fact]
    public async Task Cancel_NotFound_Fails()
    {
        await using var ctx = _db.CreateContext();
        var result = await new EnrollmentService(ctx).CancelAsync(Guid.NewGuid());
        Assert.False(result.Success);
        Assert.Equal("La matrícula no existe.", result.Error);
    }
}
