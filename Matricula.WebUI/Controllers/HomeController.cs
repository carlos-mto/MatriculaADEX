using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Matricula.WebUI.Models;
using Matricula.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Matricula.WebUI.Controllers;

[Authorize]
public class HomeController : Controller
{
  private readonly ILogger<HomeController> _logger;

  private MatriculaContext _context;
  private readonly EnrollmentService _enrollmentService;
  private readonly PeriodService _periodService;

  public HomeController(ILogger<HomeController> logger, MatriculaContext context, EnrollmentService enrollmentService, PeriodService periodService)
  {
    _logger = logger;
    _enrollmentService = enrollmentService;
    _context = context;
    _periodService = periodService;
  }

  public SelectList getPopulateShifts()
  {
    List<SelectListItem> selectListItems = _context.Shifts.Select(a => new SelectListItem()
    {
      Text = a.Name,
      Value = a.Id.ToString()
    }).ToList();

    selectListItems.Insert(0, new SelectListItem() { Text = "Seleccionar Turno", Value = "", Selected = true });

    return (SelectList)(ViewData["Shifts"] = new SelectList(selectListItems, "Value", "Text"));
  }

  public async Task<IActionResult> Index()
  {
    ViewData["Students"] = await _context.Students.Select(_ => new { _.Id, _.FullName, _.Cycle }).ToListAsync();
    getPopulateShifts();
    return View();
  }

  [HttpPost]
  public async Task<IActionResult> GetCourses(int cycle, Guid shiftId)
  {
    var period = await _periodService.GetActivePeriodAsync();
    var rows = await _context.ClassRoomCourses
      .Where(a => a.Period == period && a.Cycle == cycle && a.ShiftId == shiftId && a.Capacity > 0)
      .OrderBy(_ => _.StartTime)
      .Select(a => new
      {
        a.Id,
        CourseName = a.Course!.Name,
        ClassRoomName = a.ClassRoom!.Name,
        ShiftName = a.Shift!.Name,
        TeacherName = a.Teacher != null ? a.Teacher.FullName : null,
        a.Capacity,
        a.StartTime,
        a.EndTime
      })
      .ToListAsync();

    var courses = rows.Select(a => new
    {
      id = a.Id,
      courseName = a.CourseName,
      classRoomName = a.ClassRoomName,
      shiftName = a.ShiftName,
      teacherName = a.TeacherName ?? "Por asignar",
      capacity = a.Capacity,
      startTime = a.StartTime.ToString(@"hh\:mm"),
      endTime = a.EndTime.ToString(@"hh\:mm")
    });
    return Json(courses);
  }


  [HttpPost, ValidateAntiForgeryToken]
  public async Task<IActionResult> Index(EnrollmentModel model)
  {
    if (!ModelState.IsValid)
    {
      return RedirectToAction("Index");
    }

    var courseIds = new List<Guid>();
    foreach (var part in (model.listCourses ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
    {
      if (!Guid.TryParse(part, out var id))
      {
        TempData["Error"] = "Lista de cursos inválida.";
        return RedirectToAction("Index");
      }
      courseIds.Add(id);
    }

    var result = await _enrollmentService.EnrollAsync(model.studentSelect, model.shiftSelect, courseIds, await _periodService.GetActivePeriodAsync());
    if (!result.Success)
    {
      TempData["Error"] = result.Error;
      return RedirectToAction("Index");
    }

    return RedirectToAction("Gracias");
  }

  public IActionResult Gracias()
  {
    return View();
  }
  [AllowAnonymous]
  public IActionResult Privacy()
  {
    return View();
  }

  [AllowAnonymous]
  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error()
  {
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}
