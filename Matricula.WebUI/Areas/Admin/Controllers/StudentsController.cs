using Matricula.Infrastructure.Services;
using Matricula.WebUI.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Matricula.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class StudentsController : AdminControllerBase
{
    private readonly MatriculaContext _ctx;
    private readonly CatalogService _catalog;

    public StudentsController(MatriculaContext ctx, CatalogService catalog)
    {
        _ctx = ctx;
        _catalog = catalog;
    }

    public async Task<IActionResult> Index(string? q, int? cycle, int page = 1)
    {
        var query = _ctx.Students.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(s => s.FullName.ToLower().Contains(term));
        }
        if (cycle is { } cy) query = query.Where(s => s.Cycle == cy);

        var (items, pager) = await query.OrderBy(s => s.FullName).ToPagedAsync(page);
        ViewData["Q"] = q;
        ViewData["Cycles"] = new SelectList(Enumerable.Range(1, 10), cycle);
        ViewData["Pager"] = pager;
        return View(items);
    }

    public IActionResult Create() => View("Form", new StudentForm());

    public async Task<IActionResult> Edit(Guid id)
    {
        var s = await _ctx.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (s is null)
        {
            TempData["Error"] = "El estudiante no existe.";
            return RedirectToAction("Index");
        }
        return View("Form", new StudentForm { Id = s.Id, FullName = s.FullName, Cycle = s.Cycle });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(StudentForm vm) => SaveAsync(vm);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(Guid id, StudentForm vm)
    {
        vm.Id = id;
        return SaveAsync(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
        => RedirectWithResult(await _catalog.DeleteStudentAsync(id), "Alumno eliminado.");

    private async Task<IActionResult> SaveAsync(StudentForm vm)
    {
        if (ModelState.IsValid)
        {
            var result = await _catalog.SaveStudentAsync(new Student { Id = vm.Id, FullName = vm.FullName, Cycle = vm.Cycle });
            if (result.Success)
            {
                TempData["Success"] = vm.Id == Guid.Empty ? "Alumno creado." : "Alumno actualizado.";
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", result.Error!);
        }
        return View("Form", vm);
    }
}
