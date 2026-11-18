using Matricula.Infrastructure.Services;
using Matricula.WebUI.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Matricula.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class SchedulesController : AdminControllerBase
{
    private readonly MatriculaContext _ctx;
    private readonly ScheduleService _schedules;
    private readonly PeriodService _periods;

    public SchedulesController(MatriculaContext ctx, ScheduleService schedules, PeriodService periods)
    {
        _ctx = ctx;
        _schedules = schedules;
        _periods = periods;
    }

    public async Task<IActionResult> Index(string? period, int? cycle, Guid? shiftId, Guid? teacherId)
    {
        var active = await _periods.GetActivePeriodAsync();
        period = string.IsNullOrWhiteSpace(period) ? active : period.Trim();
        var query = _ctx.ClassRoomCourses.AsNoTracking()
            .Include(c => c.Course).Include(c => c.ClassRoom).Include(c => c.Shift).Include(c => c.Teacher)
            .Where(c => c.Period == period);
        if (cycle is { } cy) query = query.Where(c => c.Cycle == cy);
        if (shiftId is { } sid) query = query.Where(c => c.ShiftId == sid);
        if (teacherId is { } tid) query = query.Where(c => c.TeacherId == tid);

        var items = await query.OrderBy(c => c.Cycle).ThenBy(c => c.Shift!.Name).ThenBy(c => c.StartTime).ToListAsync();

        var periods = await _ctx.ClassRoomCourses.AsNoTracking().Select(c => c.Period).Distinct().ToListAsync();
        if (!periods.Contains(active)) periods.Add(active);
        if (!periods.Contains(period)) periods.Add(period);
        periods.Sort(StringComparer.Ordinal);
        ViewData["Periods"] = new SelectList(periods, period);
        ViewData["Period"] = period;
        ViewData["ActivePeriod"] = active;
        ViewData["Cycles"] = new SelectList(Enumerable.Range(1, 10), cycle);
        ViewData["Shifts"] = new SelectList(await _ctx.Shifts.AsNoTracking().OrderBy(s => s.Name).ToListAsync(), "Id", "Name", shiftId);
        ViewData["Teachers"] = new SelectList(await _ctx.Teachers.AsNoTracking().OrderBy(t => t.FullName).ToListAsync(), "Id", "FullName", teacherId);
        return View(items);
    }

    public async Task<IActionResult> Create()
    {
        await LoadListsAsync(null, Guid.Empty);
        return View("Form", new ScheduleForm { Period = await _periods.GetActivePeriodAsync() });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var c = await _ctx.ClassRoomCourses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null)
        {
            TempData["Error"] = "El horario no existe.";
            return RedirectToAction("Index");
        }
        await LoadListsAsync(c.TeacherId, c.Id);
        return View("Form", new ScheduleForm
        {
            Id = c.Id,
            CourseId = c.CourseId,
            ClassRoomId = c.ClassRoomId,
            ShiftId = c.ShiftId,
            TeacherId = c.TeacherId,
            Cycle = c.Cycle,
            StartTime = c.StartTime.ToString(@"hh\:mm"),
            EndTime = c.EndTime.ToString(@"hh\:mm"),
            Capacity = c.Capacity,
            Period = c.Period,
            Version = c.Version
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(ScheduleForm vm) => SaveAsync(vm);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(Guid id, ScheduleForm vm)
    {
        vm.Id = id;
        return SaveAsync(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyFrom(string fromPeriod, string toPeriod, int capacity)
    {
        var result = await _schedules.CopyFromPeriodAsync(fromPeriod, toPeriod, capacity);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Index");
        }
        TempData["Success"] = $"Se copiaron {result.Copied} horarios al periodo {toPeriod.Trim()}."
            + (result.Skipped > 0 ? $" Se omitieron {result.Skipped} por cruces de horario." : "");
        return RedirectToAction("Index", new { period = toPeriod.Trim() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
        => RedirectWithResult(await _schedules.DeleteAsync(id), "Horario eliminado.");

    private async Task<IActionResult> SaveAsync(ScheduleForm vm)
    {
        var start = ParseTime(vm.StartTime);
        var end = ParseTime(vm.EndTime);
        if (!string.IsNullOrWhiteSpace(vm.StartTime) && start is null) ModelState.AddModelError(nameof(vm.StartTime), "Hora de inicio inválida.");
        if (!string.IsNullOrWhiteSpace(vm.EndTime) && end is null) ModelState.AddModelError(nameof(vm.EndTime), "Hora de fin inválida.");

        if (ModelState.IsValid)
        {
            var entity = new ClassRoomCourse
            {
                Id = vm.Id,
                CourseId = vm.CourseId!.Value,
                ClassRoomId = vm.ClassRoomId!.Value,
                ShiftId = vm.ShiftId!.Value,
                TeacherId = vm.TeacherId,
                Cycle = vm.Cycle,
                Capacity = vm.Capacity,
                StartTime = start!.Value,
                EndTime = end!.Value,
                Period = (vm.Period ?? "").Trim(),
                Version = vm.Version
            };
            var result = vm.Id == Guid.Empty ? await _schedules.CreateAsync(entity) : await _schedules.UpdateAsync(entity);
            if (result.Success)
            {
                TempData["Success"] = vm.Id == Guid.Empty ? "Horario creado." : "Horario actualizado.";
                return RedirectToAction("Index", new { period = entity.Period });
            }
            ModelState.AddModelError("", result.Error!);
        }

        await LoadListsAsync(vm.TeacherId, vm.Id);
        return View("Form", vm);
    }

    private static TimeSpan? ParseTime(string? s) =>
        TimeSpan.TryParse(s, out var t) && t >= TimeSpan.Zero && t < TimeSpan.FromHours(24) ? t : null;

    // Teachers: only active ones, plus the one currently selected even if inactive.
    private async Task LoadListsAsync(Guid? currentTeacherId, Guid scheduleId)
    {
        // Structural fields are frozen once the schedule has enrollments (also enforced by the service).
        ViewData["Locked"] = scheduleId != Guid.Empty && await _ctx.EnrollmentDetails.AnyAsync(d => d.ClassRoomCourseId == scheduleId);
        ViewData["Courses"] = new SelectList(await _ctx.Courses.AsNoTracking().OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        ViewData["ClassRooms"] = new SelectList(await _ctx.ClassRooms.AsNoTracking().OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        ViewData["Shifts"] = new SelectList(await _ctx.Shifts.AsNoTracking().OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
        ViewData["Teachers"] = new SelectList(
            await _ctx.Teachers.AsNoTracking().Where(t => t.IsActive || t.Id == currentTeacherId).OrderBy(t => t.FullName).ToListAsync(),
            "Id", "FullName");
    }
}
