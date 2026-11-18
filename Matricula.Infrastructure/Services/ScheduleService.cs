using Matricula.Infrastructure.Data;

namespace Matricula.Infrastructure.Services;

public record CopyScheduleResult(bool Success, string? Error, int Copied, int Skipped)
{
    public static CopyScheduleResult Fail(string error) => new(false, error, 0, 0);
}

public class ScheduleService
{
    private readonly MatriculaContext _ctx;

    public ScheduleService(MatriculaContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<OperationResult> ValidateAsync(ClassRoomCourse c, Guid? excludeId)
    {
        if (!PeriodService.IsValidPeriod(c.Period)) return OperationResult.Fail("Formato de periodo inválido. Ej.: 2026-II");

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
            .Where(x => x.Period == c.Period && x.ClassRoomId == c.ClassRoomId && x.Id != excludeId && x.StartTime < c.EndTime && c.StartTime < x.EndTime)
            .FirstOrDefaultAsync();
        if (roomClash is not null)
            return OperationResult.Fail($"El aula {room.Name} ya está ocupada de {roomClash.StartTime:hh\\:mm} a {roomClash.EndTime:hh\\:mm} por {roomClash.Course?.Name}.");

        if (c.TeacherId is { } teacherId)
        {
            var teacher = await _ctx.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teacherId);
            if (teacher is null) return OperationResult.Fail("El profesor no existe.");
            if (!teacher.IsActive)
            {
                // An update may keep the teacher already stored even if later deactivated.
                var currentTeacherId = excludeId is { } id
                    ? await _ctx.ClassRoomCourses.AsNoTracking().Where(x => x.Id == id).Select(x => x.TeacherId).FirstOrDefaultAsync()
                    : null;
                if (currentTeacherId != teacherId) return OperationResult.Fail("El profesor está inactivo.");
            }

            var teacherClash = await _ctx.ClassRoomCourses.AsNoTracking()
                .Include(x => x.Course)
                .Where(x => x.Period == c.Period && x.TeacherId == teacherId && x.Id != excludeId && x.StartTime < c.EndTime && c.StartTime < x.EndTime)
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

    public const string ConcurrencyError = "El horario fue modificado por otra operación (por ejemplo, una matrícula). Recargue e intente de nuevo.";

    public async Task<OperationResult> UpdateAsync(ClassRoomCourse c)
    {
        var existing = await _ctx.ClassRoomCourses.FirstOrDefaultAsync(x => x.Id == c.Id);
        if (existing is null) return OperationResult.Fail("El horario no existe.");

        if (existing.Version != c.Version) return OperationResult.Fail(ConcurrencyError);

        if (await _ctx.EnrollmentDetails.AnyAsync(d => d.ClassRoomCourseId == c.Id)
            && (existing.CourseId != c.CourseId || existing.Cycle != c.Cycle || existing.ShiftId != c.ShiftId
                || existing.Period != c.Period || existing.StartTime != c.StartTime || existing.EndTime != c.EndTime))
            return OperationResult.Fail("El horario tiene matrículas: solo se puede cambiar aula, profesor y cupos.");

        var result = await ValidateAsync(c, c.Id);
        if (!result.Success) return result;

        existing.CourseId = c.CourseId;
        existing.ClassRoomId = c.ClassRoomId;
        existing.ShiftId = c.ShiftId;
        existing.TeacherId = c.TeacherId;
        existing.Cycle = c.Cycle;
        existing.Period = c.Period;
        existing.Capacity = c.Capacity;
        existing.StartTime = c.StartTime;
        existing.EndTime = c.EndTime;
        _ctx.Entry(existing).Property(x => x.Version).OriginalValue = c.Version;
        try
        {
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult.Fail(ConcurrencyError);
        }
        return OperationResult.Ok();
    }

    public async Task<CopyScheduleResult> CopyFromPeriodAsync(string fromPeriod, string toPeriod, int capacity)
    {
        fromPeriod = (fromPeriod ?? "").Trim();
        toPeriod = (toPeriod ?? "").Trim();
        if (!PeriodService.IsValidPeriod(fromPeriod) || !PeriodService.IsValidPeriod(toPeriod))
            return CopyScheduleResult.Fail("Formato de periodo inválido. Ej.: 2026-II");
        if (fromPeriod == toPeriod) return CopyScheduleResult.Fail("El periodo origen y destino deben ser distintos.");
        if (capacity < 0) return CopyScheduleResult.Fail("La capacidad no puede ser negativa.");

        var source = await _ctx.ClassRoomCourses.AsNoTracking()
            .Where(x => x.Period == fromPeriod)
            .OrderBy(x => x.Cycle).ThenBy(x => x.StartTime)
            .ToListAsync();
        if (source.Count == 0) return CopyScheduleResult.Fail("El periodo origen no tiene horarios.");
        if (await _ctx.ClassRoomCourses.AnyAsync(x => x.Period == toPeriod))
            return CopyScheduleResult.Fail("El periodo destino ya tiene horarios.");

        var inactiveTeachers = await _ctx.Teachers.AsNoTracking().Where(t => !t.IsActive).Select(t => t.Id).ToListAsync();

        await using var tx = await _ctx.Database.BeginTransactionAsync();
        var copied = 0;
        var skipped = 0;
        foreach (var s in source)
        {
            var copy = new ClassRoomCourse
            {
                CourseId = s.CourseId,
                ClassRoomId = s.ClassRoomId,
                ShiftId = s.ShiftId,
                // A teacher deactivated since then is left unassigned instead of failing the copy.
                TeacherId = s.TeacherId is { } t && inactiveTeachers.Contains(t) ? null : s.TeacherId,
                Cycle = s.Cycle,
                Period = toPeriod,
                Capacity = capacity,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            };
            if (!(await ValidateAsync(copy, null)).Success)
            {
                skipped++;
                continue;
            }
            _ctx.ClassRoomCourses.Add(copy);
            await _ctx.SaveChangesAsync();
            copied++;
        }
        await tx.CommitAsync();
        return new CopyScheduleResult(true, null, copied, skipped);
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
