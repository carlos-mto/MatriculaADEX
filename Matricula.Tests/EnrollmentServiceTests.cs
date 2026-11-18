using Matricula.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Tests;

[Collection("postgres")]
public class EnrollmentServiceTests
{
    private const string Period = "2026-II";
    private readonly PostgresFixture _db;

    public EnrollmentServiceTests(PostgresFixture db) => _db = db;

    // Each call uses its own context, as in a real request.
    private async Task<OperationResult> EnrollAsync(Guid studentId, Guid shiftId, IReadOnlyList<Guid> ids, string period = Period)
    {
        await using var ctx = _db.CreateContext();
        return await new EnrollmentService(ctx).EnrollAsync(studentId, shiftId, ids, period);
    }

    private async Task<int> CountEnrollmentsAsync(Guid studentId)
    {
        await using var ctx = _db.CreateContext();
        return await ctx.Enrollments.CountAsync(e => e.StudentId == studentId);
    }

    private async Task<int> CapacityAsync(Guid crcId)
    {
        await using var ctx = _db.CreateContext();
        return await ctx.ClassRoomCourses.Where(c => c.Id == crcId).Select(c => c.Capacity).SingleAsync();
    }

    [Fact]
    public async Task Success_CreatesEnrollmentDetailsAndDecrementsCapacity()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var c1 = await _db.AddCourseAsync(shift, 1, "08:30", "09:30", 5);
        var c2 = await _db.AddCourseAsync(shift, 1, "09:30", "10:30", 7);

        var result = await EnrollAsync(student.Id, shift.Id, [c1.Id, c2.Id]);

        Assert.True(result.Success, result.Error);
        await using var ctx = _db.CreateContext();
        var enrollment = await ctx.Enrollments.SingleAsync(e => e.StudentId == student.Id);
        Assert.Equal(Period, enrollment.Period);
        Assert.Equal(shift.Id, enrollment.ShiftId);
        Assert.Equal(2, await ctx.EnrollmentDetails.CountAsync(d => d.EnrollmentId == enrollment.Id));
        Assert.Equal(4, await CapacityAsync(c1.Id));
        Assert.Equal(6, await CapacityAsync(c2.Id));
    }

    [Fact]
    public async Task EmptyList_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();

        var result = await EnrollAsync(student.Id, shift.Id, []);

        Assert.False(result.Success);
        Assert.Equal(0, await CountEnrollmentsAsync(student.Id));
    }

    [Fact]
    public async Task DuplicatedIds_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var c1 = await _db.AddCourseAsync(shift, 1, "08:30", "09:30");

        var result = await EnrollAsync(student.Id, shift.Id, [c1.Id, c1.Id]);

        Assert.False(result.Success);
        Assert.Equal(0, await CountEnrollmentsAsync(student.Id));
        Assert.Equal(10, await CapacityAsync(c1.Id));
    }

    [Fact]
    public async Task CourseFromAnotherCycle_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync(cycle: 1);
        var other = await _db.AddCourseAsync(shift, 2, "08:30", "09:30");

        var result = await EnrollAsync(student.Id, shift.Id, [other.Id]);

        Assert.False(result.Success);
        Assert.Equal(0, await CountEnrollmentsAsync(student.Id));
    }

    [Fact]
    public async Task CourseFromAnotherShift_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var otherShift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var other = await _db.AddCourseAsync(otherShift, 1, "08:30", "09:30");

        var result = await EnrollAsync(student.Id, shift.Id, [other.Id]);

        Assert.False(result.Success);
        Assert.Equal(0, await CountEnrollmentsAsync(student.Id));
    }

    [Fact]
    public async Task OverlappingTimes_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var c1 = await _db.AddCourseAsync(shift, 1, "08:30", "09:30");
        var c2 = await _db.AddCourseAsync(shift, 1, "09:00", "10:00");

        var result = await EnrollAsync(student.Id, shift.Id, [c1.Id, c2.Id]);

        Assert.False(result.Success);
        Assert.Equal(0, await CountEnrollmentsAsync(student.Id));
    }

    [Fact]
    public async Task AdjacentTimes_Succeeds()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var c1 = await _db.AddCourseAsync(shift, 1, "08:30", "09:30");
        var c2 = await _db.AddCourseAsync(shift, 1, "09:30", "10:30");

        var result = await EnrollAsync(student.Id, shift.Id, [c1.Id, c2.Id]);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public async Task ZeroCapacity_FailsAndRollsBack()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var ok = await _db.AddCourseAsync(shift, 1, "08:30", "09:30", 5);
        var full = await _db.AddCourseAsync(shift, 1, "09:30", "10:30", 0);

        var result = await EnrollAsync(student.Id, shift.Id, [ok.Id, full.Id]);

        Assert.False(result.Success);
        Assert.Equal(0, await CountEnrollmentsAsync(student.Id));
        Assert.Equal(5, await CapacityAsync(ok.Id));
        Assert.Equal(0, await CapacityAsync(full.Id));
    }

    [Fact]
    public async Task SameStudentSamePeriodTwice_SecondFails()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var c1 = await _db.AddCourseAsync(shift, 1, "08:30", "09:30");

        var first = await EnrollAsync(student.Id, shift.Id, [c1.Id]);
        var second = await EnrollAsync(student.Id, shift.Id, [c1.Id]);

        Assert.True(first.Success, first.Error);
        Assert.False(second.Success);
        Assert.Equal(1, await CountEnrollmentsAsync(student.Id));
        Assert.Equal(9, await CapacityAsync(c1.Id));
    }

    [Fact]
    public async Task SameStudentDifferentPeriod_Succeeds()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var c1 = await _db.AddCourseAsync(shift, 1, "08:30", "09:30");

        var first = await EnrollAsync(student.Id, shift.Id, [c1.Id], "2026-I");
        var second = await EnrollAsync(student.Id, shift.Id, [c1.Id], "2026-II");

        Assert.True(first.Success, first.Error);
        Assert.True(second.Success, second.Error);
        Assert.Equal(2, await CountEnrollmentsAsync(student.Id));
    }

    [Fact]
    public async Task ConcurrentEnrollments_OnlyOneGetsLastSeat()
    {
        var shift = await _db.AddShiftAsync();
        var s1 = await _db.AddStudentAsync();
        var s2 = await _db.AddStudentAsync();
        var course = await _db.AddCourseAsync(shift, 1, "08:30", "09:30", 1);

        var results = await Task.WhenAll(
            Task.Run(() => EnrollAsync(s1.Id, shift.Id, [course.Id])),
            Task.Run(() => EnrollAsync(s2.Id, shift.Id, [course.Id])));

        Assert.Equal(1, results.Count(r => r.Success));
        Assert.Equal(0, await CapacityAsync(course.Id));
        Assert.Equal(1, await CountEnrollmentsAsync(s1.Id) + await CountEnrollmentsAsync(s2.Id));
    }
}
