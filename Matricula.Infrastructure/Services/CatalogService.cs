using Matricula.Infrastructure.Data;

namespace Matricula.Infrastructure.Services;

public class CatalogService
{
    private readonly MatriculaContext _ctx;

    public CatalogService(MatriculaContext ctx)
    {
        _ctx = ctx;
    }

    // ---- Student ----
    public async Task<OperationResult> SaveStudentAsync(Student s)
    {
        s.FullName = (s.FullName ?? "").Trim();
        if (s.FullName.Length == 0) return OperationResult.Fail("El nombre del estudiante es obligatorio.");
        if (s.Cycle < 1 || s.Cycle > 10) return OperationResult.Fail("El ciclo debe estar entre 1 y 10.");

        if (!await UpsertAsync(_ctx.Students, s, e => { e.FullName = s.FullName; e.Cycle = s.Cycle; }))
            return OperationResult.Fail("El estudiante no existe.");
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteStudentAsync(Guid id)
    {
        if (await _ctx.Enrollments.AnyAsync(e => e.StudentId == id))
            return OperationResult.Fail("No se puede eliminar: el estudiante tiene matrículas.");
        return await DeleteAsync(_ctx.Students, id, "El estudiante no existe.");
    }

    // ---- Course ----
    public async Task<OperationResult> SaveCourseAsync(Course c)
    {
        c.Name = (c.Name ?? "").Trim();
        if (c.Name.Length == 0) return OperationResult.Fail("El nombre del curso es obligatorio.");
        var lower = c.Name.ToLower();
        if (await _ctx.Courses.AnyAsync(x => x.Id != c.Id && x.Name.Trim().ToLower() == lower))
            return OperationResult.Fail("Ya existe un curso con ese nombre.");

        if (!await UpsertAsync(_ctx.Courses, c, e => e.Name = c.Name))
            return OperationResult.Fail("El curso no existe.");
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteCourseAsync(Guid id)
    {
        if (await _ctx.ClassRoomCourses.AnyAsync(x => x.CourseId == id))
            return OperationResult.Fail("No se puede eliminar: el curso tiene horarios asignados.");
        return await DeleteAsync(_ctx.Courses, id, "El curso no existe.");
    }

    // ---- ClassRoom ----
    public async Task<OperationResult> SaveClassRoomAsync(ClassRoom c)
    {
        c.Name = (c.Name ?? "").Trim();
        if (c.Name.Length == 0) return OperationResult.Fail("El nombre del aula es obligatorio.");
        var lower = c.Name.ToLower();
        if (await _ctx.ClassRooms.AnyAsync(x => x.Id != c.Id && x.Name.Trim().ToLower() == lower))
            return OperationResult.Fail("Ya existe un aula con ese nombre.");

        if (!await UpsertAsync(_ctx.ClassRooms, c, e => e.Name = c.Name))
            return OperationResult.Fail("El aula no existe.");
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteClassRoomAsync(Guid id)
    {
        if (await _ctx.ClassRoomCourses.AnyAsync(x => x.ClassRoomId == id))
            return OperationResult.Fail("No se puede eliminar: el aula tiene horarios asignados.");
        return await DeleteAsync(_ctx.ClassRooms, id, "El aula no existe.");
    }

    // ---- Shift ----
    public async Task<OperationResult> SaveShiftAsync(Shift s)
    {
        s.Name = (s.Name ?? "").Trim();
        if (s.Name.Length == 0) return OperationResult.Fail("El nombre del turno es obligatorio.");
        var lower = s.Name.ToLower();
        if (await _ctx.Shifts.AnyAsync(x => x.Id != s.Id && x.Name.Trim().ToLower() == lower))
            return OperationResult.Fail("Ya existe un turno con ese nombre.");

        if (!await UpsertAsync(_ctx.Shifts, s, e => e.Name = s.Name))
            return OperationResult.Fail("El turno no existe.");
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteShiftAsync(Guid id)
    {
        if (await _ctx.ClassRoomCourses.AnyAsync(x => x.ShiftId == id) || await _ctx.Enrollments.AnyAsync(x => x.ShiftId == id))
            return OperationResult.Fail("No se puede eliminar: el turno tiene horarios o matrículas asignados.");
        return await DeleteAsync(_ctx.Shifts, id, "El turno no existe.");
    }

    // ---- Teacher ----
    public async Task<OperationResult> SaveTeacherAsync(Teacher t)
    {
        t.FullName = (t.FullName ?? "").Trim();
        t.Email = string.IsNullOrWhiteSpace(t.Email) ? null : t.Email.Trim();
        t.Specialty = string.IsNullOrWhiteSpace(t.Specialty) ? null : t.Specialty.Trim();
        if (t.FullName.Length == 0) return OperationResult.Fail("El nombre del profesor es obligatorio.");
        if (t.FullName.Length > 150) return OperationResult.Fail("El nombre del profesor no puede superar 150 caracteres.");
        if (t.Email is { Length: > 200 }) return OperationResult.Fail("El correo no puede superar 200 caracteres.");
        if (t.Specialty is { Length: > 150 }) return OperationResult.Fail("La especialidad no puede superar 150 caracteres.");
        if (t.Email is not null)
        {
            var email = t.Email.ToLower();
            if (await _ctx.Teachers.AnyAsync(x => x.Id != t.Id && x.Email != null && x.Email.ToLower() == email))
                return OperationResult.Fail("Ya existe un profesor con ese correo.");
        }

        if (!await UpsertAsync(_ctx.Teachers, t, e =>
        {
            e.FullName = t.FullName;
            e.Email = t.Email;
            e.Specialty = t.Specialty;
            e.IsActive = t.IsActive;
        }))
            return OperationResult.Fail("El profesor no existe.");
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteTeacherAsync(Guid id)
    {
        if (await _ctx.ClassRoomCourses.AnyAsync(x => x.TeacherId == id))
            return OperationResult.Fail("No se puede eliminar: el profesor tiene horarios asignados; desactívelo en su lugar.");
        return await DeleteAsync(_ctx.Teachers, id, "El profesor no existe.");
    }

    // ---- helpers ----
    // New entity (Id empty) is added; otherwise the existing row is loaded and updated.
    private async Task<bool> UpsertAsync<T>(DbSet<T> set, T entity, Action<T> copy) where T : BaseEntity
    {
        if (entity.Id == Guid.Empty)
        {
            set.Add(entity);
        }
        else
        {
            var existing = await set.FirstOrDefaultAsync(x => x.Id == entity.Id);
            if (existing is null) return false;
            copy(existing);
        }
        await _ctx.SaveChangesAsync();
        return true;
    }

    private async Task<OperationResult> DeleteAsync<T>(DbSet<T> set, Guid id, string notFound) where T : BaseEntity
    {
        var existing = await set.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null) return OperationResult.Fail(notFound);
        set.Remove(existing);
        await _ctx.SaveChangesAsync();
        return OperationResult.Ok();
    }
}
