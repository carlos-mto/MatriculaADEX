using Matricula.Infrastructure.Data;

namespace Matricula.Infrastructure.Services;

public class ScheduleService
{
    private readonly MatriculaContext _ctx;

    public ScheduleService(MatriculaContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<OperationResult> ValidateAsync(ClassRoomCourse c, Guid? excludeId)
    {
        var course = await _ctx.Courses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == c.CourseId);
        if (course is null) return OperationResult.Fail("El curso no existe.");

        var room = await _ctx.ClassRooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == c.ClassRoomId);
        if (room is null) return OperationResult.Fail("El aula no existe.");

        if (!await _ctx.Shifts.AnyAsync(x => x.Id == c.ShiftId)) return OperationResult.Fail("El turno no existe.");

        if (c.Cycle < 1 || c.Cycle > 10) return OperationResult.Fail("El ciclo debe estar entre 1 y 10.");
        if (c.Capacity < 0) return OperationResult.Fail("La capacidad no puede ser negativa.");
        if (c.EndTime <= c.StartTime) return OperationResult.Fail("La hora de fin debe ser posterior a la de inicio.");

        var roomClash = await _ctx.ClassRoomCourses.AsNoTracking()
            .Include(x => x.Course)
            .Where(x => x.ClassRoomId == c.ClassRoomId && x.Id != excludeId && x.StartTime < c.EndTime && c.StartTime < x.EndTime)
            .FirstOrDefaultAsync();
        if (roomClash is not null)
            return OperationResult.Fail($"El aula {room.Name} ya está ocupada de {roomClash.StartTime:hh\\:mm} a {roomClash.EndTime:hh\\:mm} por {roomClash.Course?.Name}.");

        if (c.TeacherId is { } teacherId)
        {
            var teacher = await _ctx.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teacherId);
            if (teacher is null) return OperationResult.Fail("El profesor no existe.");
            if (!teacher.IsActive) return OperationResult.Fail("El profesor está inactivo.");

            var teacherClash = await _ctx.ClassRoomCourses.AsNoTracking()
                .Include(x => x.Course)
                .Where(x => x.TeacherId == teacherId && x.Id != excludeId && x.StartTime < c.EndTime && c.StartTime < x.EndTime)
                .FirstOrDefaultAsync();
            if (teacherClash is not null)
                return OperationResult.Fail($"El profesor {teacher.FullName} ya tiene clase de {teacherClash.StartTime:hh\\:mm} a {teacherClash.EndTime:hh\\:mm} ({teacherClash.Course?.Name}).");
        }

        return OperationResult.Ok();
    }

    public async Task<OperationResult> CreateAsync(ClassRoomCourse c)
    {
        var result = await ValidateAsync(c, null);
        if (!result.Success) return result;
        _ctx.ClassRoomCourses.Add(c);
        await _ctx.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> UpdateAsync(ClassRoomCourse c)
    {
        var result = await ValidateAsync(c, c.Id);
        if (!result.Success) return result;

        var existing = await _ctx.ClassRoomCourses.FirstOrDefaultAsync(x => x.Id == c.Id);
        if (existing is null) return OperationResult.Fail("El horario no existe.");

        existing.CourseId = c.CourseId;
        existing.ClassRoomId = c.ClassRoomId;
        existing.ShiftId = c.ShiftId;
        existing.TeacherId = c.TeacherId;
        existing.Cycle = c.Cycle;
        existing.Capacity = c.Capacity;
        existing.StartTime = c.StartTime;
        existing.EndTime = c.EndTime;
        await _ctx.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        var existing = await _ctx.ClassRoomCourses.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null) return OperationResult.Fail("El horario no existe.");
        if (await _ctx.EnrollmentDetails.AnyAsync(d => d.ClassRoomCourseId == id))
            return OperationResult.Fail("No se puede eliminar: el horario tiene matrículas.");
        _ctx.ClassRoomCourses.Remove(existing);
        await _ctx.SaveChangesAsync();
        return OperationResult.Ok();
    }
}
