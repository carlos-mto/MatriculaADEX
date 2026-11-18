using Matricula.Infrastructure.Services;

namespace Matricula.Tests;

[Collection("postgres")]
public class DashboardServiceTests
{
    private readonly PostgresFixture _db;

    public DashboardServiceTests(PostgresFixture db) => _db = db;

    [Fact]
    public async Task Stats_CountEnrolledPerSchedule_AndTeacherHours()
    {
        var period = "2099-" + Random.Shared.Next(10, 99);
        var shift = await _db.AddShiftAsync();
        var teacher = await _db.AddTeacherAsync();
        var s1 = await _db.AddStudentAsync();
        var s2 = await _db.AddStudentAsync();
        var c1 = await _db.AddCourseAsync(shift, 1, "08:00", "09:30", teacher: teacher);
        var c2 = await _db.AddCourseAsync(shift, 1, "10:00", "11:00", teacher: teacher);

        await using (var ctx = _db.CreateContext())
            Assert.True((await new EnrollmentService(ctx).EnrollAsync(s1.Id, shift.Id, [c1.Id, c2.Id], period)).Success);
        await using (var ctx = _db.CreateContext())
            Assert.True((await new EnrollmentService(ctx).EnrollAsync(s2.Id, shift.Id, [c1.Id], period)).Success);
        await using (var ctx = _db.CreateContext())
            Assert.True((await new EnrollmentService(ctx).EnrollAsync(s2.Id, shift.Id, [c2.Id], "2098-I")).Success);

        await using var dctx = _db.CreateContext();
        var stats = await new DashboardService(dctx).GetAsync(period);

        Assert.Equal(2, stats.Enrollments);
        Assert.Equal(2, stats.Schedules.Single(s => s.ClassRoomCourseId == c1.Id).Enrolled);
        Assert.Equal(1, stats.Schedules.Single(s => s.ClassRoomCourseId == c2.Id).Enrolled);
        var load = stats.TeacherLoads.Single(t => t.TeacherId == teacher.Id);
        Assert.Equal(2, load.ScheduleCount);
        Assert.Equal(2.5, load.WeeklyHours);
    }
}
