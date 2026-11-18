using Matricula.Infrastructure.Services;
using Matricula.WebUI.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class TeachersController : AdminControllerBase
{
    private readonly MatriculaContext _ctx;
    private readonly CatalogService _catalog;

    public TeachersController(MatriculaContext ctx, CatalogService catalog)
    {
        _ctx = ctx;
        _catalog = catalog;
    }

    // estado: "activo" | "inactivo" | empty
    public async Task<IActionResult> Index(string? q, string? estado)
    {
        var query = _ctx.Teachers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(t => t.FullName.ToLower().Contains(term));
        }
        if (estado == "activo") query = query.Where(t => t.IsActive);
        else if (estado == "inactivo") query = query.Where(t => !t.IsActive);

        var items = await query.OrderBy(t => t.FullName)
            .Select(t => new TeacherRow(t.Id, t.FullName, t.Email, t.Specialty, t.IsActive,
                _ctx.ClassRoomCourses.Count(c => c.TeacherId == t.Id)))
            .ToListAsync();
        ViewData["Q"] = q;
        ViewData["Estado"] = estado;
        return View(items);
    }

    public IActionResult Create() => View("Form", new TeacherForm());

    public async Task<IActionResult> Edit(Guid id)
    {
        var t = await _ctx.Teachers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null)
        {
            TempData["Error"] = "El profesor no existe.";
            return RedirectToAction("Index");
        }
        return View("Form", new TeacherForm { Id = t.Id, FullName = t.FullName, Email = t.Email, Specialty = t.Specialty, IsActive = t.IsActive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(TeacherForm vm) => SaveAsync(vm);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(Guid id, TeacherForm vm)
    {
        vm.Id = id;
        return SaveAsync(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
        => RedirectWithResult(await _catalog.DeleteTeacherAsync(id), "Profesor eliminado.");

    private async Task<IActionResult> SaveAsync(TeacherForm vm)
    {
        if (ModelState.IsValid)
        {
            var result = await _catalog.SaveTeacherAsync(new Teacher
            {
                Id = vm.Id,
                FullName = vm.FullName,
                Email = vm.Email,
                Specialty = vm.Specialty,
                IsActive = vm.IsActive
            });
            if (result.Success)
            {
                TempData["Success"] = vm.Id == Guid.Empty ? "Profesor creado." : "Profesor actualizado.";
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", result.Error!);
        }
        return View("Form", vm);
    }
}
