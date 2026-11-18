using Matricula.Infrastructure.Data;
using Npgsql;

namespace Matricula.Infrastructure.Services;

public class EnrollmentService
{
    private readonly MatriculaContext _ctx;

    public EnrollmentService(MatriculaContext ctx)
    {
        _ctx = ctx;
    }

    private static OperationResult Fail(string error) => OperationResult.Fail(error);

    public async Task<OperationResult> EnrollAsync(Guid studentId, Guid shiftId, IReadOnlyList<Guid> classRoomCourseIds, string period, CancellationToken ct = default)
    {
        if (classRoomCourseIds.Count == 0)
            return Fail("Debe agregar al menos un curso.");

        if (classRoomCourseIds.Distinct().Count() != classRoomCourseIds.Count)
            return Fail("Hay cursos repetidos.");

        var student = await _ctx.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null)
            return Fail("El estudiante no existe.");

        if (!await _ctx.Shifts.AnyAsync(s => s.Id == shiftId, ct))
            return Fail("El turno no existe.");

        if (await _ctx.Enrollments.AnyAsync(e => e.StudentId == studentId && e.Period == period, ct))
            return Fail($"El estudiante ya está matriculado en el periodo {period}.");

        var courses = await _ctx.ClassRoomCourses
            .Include(c => c.Course)
            .Where(c => classRoomCourseIds.Contains(c.Id))
            .OrderBy(c => c.Id)
            .ToListAsync(ct);

        if (courses.Count != classRoomCourseIds.Count)
            return Fail("Alguno de los cursos no existe.");

        foreach (var c in courses)
        {
            if (c.Period != period)
                return Fail($"El curso {c.Course?.Name} no pertenece al periodo {period}.");
            if (c.Cycle != student.Cycle || c.ShiftId != shiftId)
                return Fail($"El curso {c.Course?.Name} no corresponde al ciclo/turno del estudiante.");
        }

        for (var i = 0; i < courses.Count; i++)
        {
            for (var j = i + 1; j < courses.Count; j++)
            {
                var a = courses[i];
                var b = courses[j];
                if (a.StartTime < b.EndTime && b.StartTime < a.EndTime)
                    return Fail($"Los cursos {a.Course?.Name} y {b.Course?.Name} se cruzan en horario.");
            }
        }

        await using var tx = await _ctx.Database.BeginTransactionAsync(ct);
        try
        {
            var enrollment = new Enrollment { StudentId = studentId, ShiftId = shiftId, Period = period };
            _ctx.Enrollments.Add(enrollment);
            await _ctx.SaveChangesAsync(ct);

            foreach (var course in courses)
            {
                var rows = await _ctx.ClassRoomCourses
                    .Where(c => c.Id == course.Id && c.Capacity > 0)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Capacity, c => c.Capacity - 1)
                        .SetProperty(c => c.UpdatedAt, DateTime.UtcNow), ct);

                if (rows == 0)
                {
                    await tx.RollbackAsync(ct);
                    return Fail($"No hay cupos disponibles en {course.Course?.Name}.");
                }

                _ctx.EnrollmentDetails.Add(new EnrollmentDetail
                {
                    EnrollmentId = enrollment.Id,
                    ClassRoomCourseId = course.Id
                });
            }

            await _ctx.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return OperationResult.Ok();
        }
        catch (DbUpdateException ex) when (!IsConflict(ex))
        {
            await tx.RollbackAsync(ct);
            if (ex.InnerException is PostgresException { SqlState: "23505" })
                return Fail($"El estudiante ya está matriculado en el periodo {period}.");
            throw;
        }
        catch (Exception ex) when (IsConflict(ex))
        {
            await tx.RollbackAsync(ct);
            return Fail("Hubo un conflicto al registrar la matrícula, inténtelo nuevamente.");
        }
    }

    public async Task<OperationResult> CancelAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        await using var tx = await _ctx.Database.BeginTransactionAsync(ct);
        try
        {
            var enrollment = await _ctx.Enrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);
            if (enrollment is null)
                return Fail("La matrícula no existe.");

            var crcIds = await _ctx.EnrollmentDetails
                .Where(d => d.EnrollmentId == enrollmentId)
                .Select(d => d.ClassRoomCourseId)
                .ToListAsync(ct);

            foreach (var id in crcIds)
            {
                await _ctx.ClassRoomCourses
                    .Where(c => c.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Capacity, c => c.Capacity + 1)
                        .SetProperty(c => c.UpdatedAt, DateTime.UtcNow), ct);
            }

            _ctx.Enrollments.Remove(enrollment);
            await _ctx.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return OperationResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            return Fail("La matrícula ya fue anulada.");
        }
        catch (Exception ex) when (IsConflict(ex))
        {
            await tx.RollbackAsync(ct);
            return Fail("Hubo un conflicto al cancelar la matrícula, inténtelo nuevamente.");
        }
    }

    // Deadlock (40P01) / serialization failure (40001); SaveChanges wraps it, ExecuteUpdate does not.
    private static bool IsConflict(Exception ex) =>
        (ex as PostgresException ?? ex.InnerException as PostgresException) is { SqlState: "40P01" or "40001" };
}
