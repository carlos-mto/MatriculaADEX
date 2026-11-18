using Matricula.Core.Entities;
using Matricula.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Tests;

[Collection("postgres")]
public class ScheduleServiceTests
{
    private readonly PostgresFixture _db;

    public ScheduleServiceTests(PostgresFixture db) => _db = db;

    private async Task<ClassRoomCourse> NewScheduleAsync(Shift shift, Guid roomId, string start, string end, Guid? teacherId = null)
    {
        await using var ctx = _db.CreateContext();
        var course = new Course { Name = "Curso-" + Guid.NewGuid() };
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();
        return new ClassRoomCourse
        {
            CourseId = course.Id,
            ClassRoomId = roomId,
            ShiftId = shift.Id,
            TeacherId = teacherId,
            Cycle = 1,
            Capacity = 10,
            StartTime = TimeSpan.Parse(start),
            EndTime = TimeSpan.Parse(end)
        };
    }

    private async Task<OperationResult> CreateAsync(ClassRoomCourse c)
    {
        await using var ctx = _db.CreateContext();
        return await new ScheduleService(ctx).CreateAsync(c);
    }

    [Fact]
    public async Task EndNotAfterStart_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var room = await _db.AddRoomAsync();
        var result = await CreateAsync(await NewScheduleAsync(shift, room.Id, "10:00", "10:00"));
        Assert.False(result.Success);
        Assert.Equal("La hora de fin debe ser posterior a la de inicio.", result.Error);
    }

    [Fact]
    public async Task RoomOverlap_Fails_AdjacentOk()
    {
        var shift = await _db.AddShiftAsync();
        var room = await _db.AddRoomAsync();
        Assert.True((await CreateAsync(await NewScheduleAsync(shift, room.Id, "08:00", "09:00"))).Success);

        var overlap = await CreateAsync(await NewScheduleAsync(shift, room.Id, "08:30", "09:30"));
        Assert.False(overlap.Success);
        Assert.Contains("ya está ocupada", overlap.Error);

        var adjacent = await CreateAsync(await NewScheduleAsync(shift, room.Id, "09:00", "10:00"));
        Assert.True(adjacent.Success, adjacent.Error);
    }

    [Fact]
    public async Task TeacherOverlapInDifferentRooms_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var teacher = await _db.AddTeacherAsync();
        var r1 = await _db.AddRoomAsync();
        var r2 = await _db.AddRoomAsync();
        Assert.True((await CreateAsync(await NewScheduleAsync(shift, r1.Id, "08:00", "09:00", teacher.Id))).Success);

        var result = await CreateAsync(await NewScheduleAsync(shift, r2.Id, "08:30", "09:30", teacher.Id));
        Assert.False(result.Success);
        Assert.Contains("ya tiene clase", result.Error);
    }

    [Fact]
    public async Task InactiveTeacher_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var teacher = await _db.AddTeacherAsync(active: false);
        var room = await _db.AddRoomAsync();
        var result = await CreateAsync(await NewScheduleAsync(shift, room.Id, "08:00", "09:00", teacher.Id));
        Assert.False(result.Success);
        Assert.Equal("El profesor está inactivo.", result.Error);
    }

    [Fact]
    public async Task UpdateExcludingItself_Succeeds()
    {
        var shift = await _db.AddShiftAsync();
        var room = await _db.AddRoomAsync();
        var crc = await NewScheduleAsync(shift, room.Id, "08:00", "09:00");
        Assert.True((await CreateAsync(crc)).Success);

        await using var ctx = _db.CreateContext();
        var result = await new ScheduleService(ctx).UpdateAsync(new ClassRoomCourse
        {
            Id = crc.Id, CourseId = crc.CourseId, ClassRoomId = room.Id, ShiftId = shift.Id, Cycle = 2, Capacity = 5,
            StartTime = TimeSpan.Parse("08:15"), EndTime = TimeSpan.Parse("09:15")
        });
        Assert.True(result.Success, result.Error);
        Assert.Equal(2, (await _db.CreateContext().ClassRoomCourses.SingleAsync(c => c.Id == crc.Id)).Cycle);
    }

    [Fact]
    public async Task DeleteWithEnrollments_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var crc = await _db.AddCourseAsync(shift, 1, "08:30", "09:30");
        await using (var ctx = _db.CreateContext())
            Assert.True((await new EnrollmentService(ctx).EnrollAsync(student.Id, shift.Id, [crc.Id], "2026-II")).Success);

        await using var ctx2 = _db.CreateContext();
        var result = await new ScheduleService(ctx2).DeleteAsync(crc.Id);
        Assert.False(result.Success);
        Assert.Equal("No se puede eliminar: el horario tiene matrículas.", result.Error);
    }
}
