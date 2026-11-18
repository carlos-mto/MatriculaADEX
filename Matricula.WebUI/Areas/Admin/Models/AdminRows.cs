namespace Matricula.WebUI.Areas.Admin.Models;

public record CatalogRow(Guid Id, string Name, int Count);

public record EnrollmentRow(Guid Id, DateTime CreatedAt, string Student, int Cycle, string? Shift, string Period, int Courses);

public record TeacherRow(Guid Id, string FullName, string? Email, string? Specialty, bool IsActive, int Schedules);

public class EnrollmentDetailsViewModel
{
    public Enrollment Enrollment { get; set; } = null!;
    public List<ClassRoomCourse> Courses { get; set; } = new();
}
