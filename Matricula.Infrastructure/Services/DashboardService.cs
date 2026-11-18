using Matricula.Infrastructure.Data;

namespace Matricula.Infrastructure.Services;

public record ScheduleOccupancy(Guid ClassRoomCourseId, string CourseName, string ClassRoomName, string ShiftName, int Cycle,
    string? TeacherName, TimeSpan StartTime, TimeSpan EndTime, int Capacity, int Enrolled);

public record TeacherLoad(Guid TeacherId, string FullName, bool IsActive, int ScheduleCount, double WeeklyHours);

public record DashboardStats(int Students, int ActiveTeachers, int Courses, int ClassRooms, int Enrollments,
    IReadOnlyList<ScheduleOccupancy> Schedules, IReadOnlyList<TeacherLoad> TeacherLoads);

public class DashboardService
{
    private readonly MatriculaContext _ctx;

    public DashboardService(MatriculaContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<DashboardStats> GetAsync(string period)
    {
        var students = await _ctx.Students.CountAsync();
        var teachers = await _ctx.Teachers.CountAsync(t => t.IsActive);
        var courses = await _ctx.Courses.CountAsync();
        var rooms = await _ctx.ClassRooms.CountAsync();
        var enrollments = await _ctx.Enrollments.CountAsync(e => e.Period == period);

        var schedules = await _ctx.ClassRoomCourses.AsNoTracking()
            .OrderBy(c => c.Cycle).ThenBy(c => c.StartTime)
            .Select(c => new ScheduleOccupancy(
                c.Id,
                c.Course!.Name,
                c.ClassRoom!.Name,
                c.Shift!.Name,
                c.Cycle,
                c.Teacher != null ? c.Teacher.FullName : null,
                c.StartTime,
                c.EndTime,
                c.Capacity,
                _ctx.EnrollmentDetails.Count(d => d.ClassRoomCourseId == c.Id && d.Enrollment!.Period == period)))
            .ToListAsync();

        var teacherRows = await _ctx.Teachers.AsNoTracking()
            .OrderBy(t => t.FullName)
            .Select(t => new
            {
                t.Id,
                t.FullName,
                t.IsActive,
                Slots = _ctx.ClassRoomCourses.Where(c => c.TeacherId == t.Id).Select(c => new { c.StartTime, c.EndTime }).ToList()
            })
            .ToListAsync();

        var loads = teacherRows
            .Select(t => new TeacherLoad(t.Id, t.FullName, t.IsActive, t.Slots.Count,
                Math.Round(t.Slots.Sum(s => (s.EndTime - s.StartTime).TotalHours), 1)))
            .ToList();

        return new DashboardStats(students, teachers, courses, rooms, enrollments, schedules, loads);
    }
}
