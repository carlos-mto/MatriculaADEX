using Matricula.Infrastructure.Services;
using Matricula.WebUI.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Matricula.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class EnrollmentsController : AdminControllerBase
{
    private readonly MatriculaContext _ctx;
    private readonly EnrollmentService _enrollments;
    private readonly PeriodService _periodService;

    public EnrollmentsController(MatriculaContext ctx, EnrollmentService enrollments, PeriodService periodService)
    {
        _ctx = ctx;
        _enrollments = enrollments;
        _periodService = periodService;
    }

    public async Task<IActionResult> Index(string? period, string? q, Guid? courseId, int page = 1)
    {
        var active = await _periodService.GetActivePeriodAsync();
        period = string.IsNullOrWhiteSpace(period) ? active : period.Trim();

        var query = _ctx.Enrollments.AsNoTracking().Where(e => e.Period == period);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(e => e.Student.FullName.ToLower().Contains(term));
        }
        if (courseId is { } cid)
            query = query.Where(e => _ctx.EnrollmentDetails.Any(d => d.EnrollmentId == e.Id && d.ClassRoomCourse!.CourseId == cid));

        var (items, pager) = await query
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EnrollmentRow(e.Id, e.CreatedAt, e.Student.FullName, e.Student.Cycle,
                e.Shift != null ? e.Shift.Name : null, e.Period,
                _ctx.EnrollmentDetails.Count(d => d.EnrollmentId == e.Id)))
            .ToPagedAsync(page);

        var periods = await _ctx.Enrollments.Select(e => e.Period).Distinct().ToListAsync();
        if (!periods.Contains(active)) periods.Add(active);
        if (!periods.Contains(period)) periods.Add(period);
        var courses = await _ctx.Courses.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

        ViewData["Periods"] = new SelectList(periods.OrderByDescending(p => p), period);
        ViewData["Courses"] = new SelectList(courses, "Id", "Name", courseId);
        ViewData["Q"] = q;
        ViewData["Pager"] = pager;
        return View(items);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var enrollment = await _ctx.Enrollments.AsNoTracking()
            .Include(e => e.Student).Include(e => e.Shift)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (enrollment is null)
        {
            TempData["Error"] = "La matrícula no existe.";
            return RedirectToAction("Index");
        }

        var courses = await _ctx.ClassRoomCourses.AsNoTracking()
            .Include(c => c.Course).Include(c => c.ClassRoom).Include(c => c.Teacher)
            .Where(c => _ctx.EnrollmentDetails.Any(d => d.EnrollmentId == id && d.ClassRoomCourseId == c.Id))
            .OrderBy(c => c.StartTime)
            .ToListAsync();

        return View(new EnrollmentDetailsViewModel { Enrollment = enrollment, Courses = courses });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _enrollments.CancelAsync(id);
        return RedirectWithResult(result, "Matrícula anulada. Se devolvieron los cupos.");
    }
}
