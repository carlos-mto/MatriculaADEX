using Matricula.Core.Entities;
using Matricula.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Tests;

[Collection("postgres")]
public class ScheduleServiceTests
{
    private readonly PostgresFixture _db;

    public ScheduleServiceTests(PostgresFixture db) => _db = db;

    private async Task<ClassRoomCourse> NewScheduleAsync(Shift shift, Guid roomId, string start, string end, Guid? teacherId = null, string period = "2026-II")
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
            Period = period,
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

    private async Task<uint> VersionAsync(Guid id)
    {
        await using var ctx = _db.CreateContext();
        return await ctx.ClassRoomCourses.Where(c => c.Id == id).Select(c => c.Version).SingleAsync();
    }

    private async Task<OperationResult> UpdateAsync(ClassRoomCourse crc, Action<ClassRoomCourse> change, uint? version = null)
    {
        var copy = new ClassRoomCourse
        {
            Id = crc.Id, CourseId = crc.CourseId, ClassRoomId = crc.ClassRoomId, ShiftId = crc.ShiftId, TeacherId = crc.TeacherId,
            Cycle = crc.Cycle, Period = crc.Period, Capacity = crc.Capacity, StartTime = crc.StartTime, EndTime = crc.EndTime,
            Version = version ?? await VersionAsync(crc.Id)
        };
        change(copy);
        await using var ctx = _db.CreateContext();
        return await new ScheduleService(ctx).UpdateAsync(copy);
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
            Period = "2026-II", Version = await VersionAsync(crc.Id),
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

    [Fact]
    public async Task SameRoomSameTime_DifferentPeriods_Allowed_SamePeriodRejected()
    {
        var shift = await _db.AddShiftAsync();
        var room = await _db.AddRoomAsync();
        var teacher = await _db.AddTeacherAsync();
        Assert.True((await CreateAsync(await NewScheduleAsync(shift, room.Id, "08:00", "09:00", teacher.Id, "2026-II"))).Success);

        var other = await CreateAsync(await NewScheduleAsync(shift, room.Id, "08:00", "09:00", teacher.Id, "2027-I"));
        Assert.True(other.Success, other.Error);

        var same = await CreateAsync(await NewScheduleAsync(shift, room.Id, "08:30", "09:30", null, "2027-I"));
        Assert.False(same.Success);
        Assert.Contains("ya está ocupada", same.Error);
    }

    [Fact]
    public async Task InvalidPeriod_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var room = await _db.AddRoomAsync();
        var result = await CreateAsync(await NewScheduleAsync(shift, room.Id, "08:00", "09:00", null, "xx"));
        Assert.False(result.Success);
        Assert.Contains("Formato de periodo inválido", result.Error);
    }

    [Fact]
    public async Task CopyFromPeriod_CopiesWithGivenCapacity_AndFailsWhenDestinationNotEmpty()
    {
        var from = "2040-" + Random.Shared.Next(10, 99);
        var to = "2041-" + Random.Shared.Next(10, 99);
        var shift = await _db.AddShiftAsync();
        var teacher = await _db.AddTeacherAsync();
        var r1 = await _db.AddRoomAsync();
        var r2 = await _db.AddRoomAsync();
        var a = await _db.AddCourseAsync(shift, 1, "08:00", "09:00", 3, r1, teacher, from);
        var b = await _db.AddCourseAsync(shift, 1, "09:00", "10:00", 3, r2, null, from);

        await using (var ctx = _db.CreateContext())
        {
            var result = await new ScheduleService(ctx).CopyFromPeriodAsync(from, to, 25);
            Assert.True(result.Success, result.Error);
            Assert.Equal(2, result.Copied);
        }

        await using var check = _db.CreateContext();
        var copies = await check.ClassRoomCourses.Where(c => c.Period == to).ToListAsync();
        Assert.Equal(2, copies.Count);
        Assert.All(copies, c => Assert.Equal(25, c.Capacity));
        Assert.Contains(copies, c => c.CourseId == a.CourseId && c.TeacherId == teacher.Id && c.ClassRoomId == r1.Id);
        Assert.Contains(copies, c => c.CourseId == b.CourseId && c.ClassRoomId == r2.Id);

        await using var ctx2 = _db.CreateContext();
        var again = await new ScheduleService(ctx2).CopyFromPeriodAsync(from, to, 25);
        Assert.False(again.Success);
        Assert.Equal("El periodo destino ya tiene horarios.", again.Error);
    }

    [Fact]
    public async Task StaleVersion_AfterEnrollment_Fails()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var crc = await _db.AddCourseAsync(shift, 1, "08:30", "09:30");
        var staleVersion = await VersionAsync(crc.Id);
        await using (var ctx = _db.CreateContext())
            Assert.True((await new EnrollmentService(ctx).EnrollAsync(student.Id, shift.Id, [crc.Id], "2026-II")).Success);

        var result = await UpdateAsync(crc, c => c.Capacity = 99, staleVersion);
        Assert.False(result.Success);
        Assert.Equal(ScheduleService.ConcurrencyError, result.Error);

        var fresh = await UpdateAsync(crc, c => c.Capacity = 99);
        Assert.True(fresh.Success, fresh.Error);
    }

    [Fact]
    public async Task Enrolled_StructuralChangeRejected_RoomTeacherCapacityAllowed()
    {
        var shift = await _db.AddShiftAsync();
        var student = await _db.AddStudentAsync();
        var crc = await _db.AddCourseAsync(shift, 1, "08:30", "09:30");
        await using (var ctx = _db.CreateContext())
            Assert.True((await new EnrollmentService(ctx).EnrollAsync(student.Id, shift.Id, [crc.Id], "2026-II")).Success);

        const string msg = "El horario tiene matrículas: solo se puede cambiar aula, profesor y cupos.";
        Assert.Equal(msg, (await UpdateAsync(crc, c => c.StartTime = TimeSpan.Parse("08:45"))).Error);
        Assert.Equal(msg, (await UpdateAsync(crc, c => c.EndTime = TimeSpan.Parse("09:45"))).Error);
        Assert.Equal(msg, (await UpdateAsync(crc, c => c.Cycle = 2)).Error);
        Assert.Equal(msg, (await UpdateAsync(crc, c => c.Period = "2027-I")).Error);
        Assert.Equal(msg, (await UpdateAsync(crc, c => c.ShiftId = Guid.NewGuid())).Error);
        Assert.Equal(msg, (await UpdateAsync(crc, c => c.CourseId = Guid.NewGuid())).Error);

        var room = await _db.AddRoomAsync();
        var teacher = await _db.AddTeacherAsync();
        var ok = await UpdateAsync(crc, c => { c.ClassRoomId = room.Id; c.TeacherId = teacher.Id; c.Capacity = 4; });
        Assert.True(ok.Success, ok.Error);
    }

    [Fact]
    public async Task InactiveCurrentTeacher_KeepsOnUpdate_DifferentInactiveRejected()
    {
        var shift = await _db.AddShiftAsync();
        var teacher = await _db.AddTeacherAsync();
        var crc = await _db.AddCourseAsync(shift, 1, "08:30", "09:30", teacher: teacher);
        await using (var ctx = _db.CreateContext())
            await ctx.Teachers.Where(t => t.Id == teacher.Id).ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, false));

        var ok = await UpdateAsync(crc, c => c.Capacity = 7);
        Assert.True(ok.Success, ok.Error);

        var otherInactive = await _db.AddTeacherAsync(active: false);
        var bad = await UpdateAsync(crc, c => c.TeacherId = otherInactive.Id);
        Assert.False(bad.Success);
        Assert.Equal("El profesor está inactivo.", bad.Error);
    }
}
