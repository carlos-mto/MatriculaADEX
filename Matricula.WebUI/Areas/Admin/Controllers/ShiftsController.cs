using Matricula.Infrastructure.Services;
using Matricula.WebUI.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class ShiftsController : AdminControllerBase
{
    private readonly MatriculaContext _ctx;
    private readonly CatalogService _catalog;

    public ShiftsController(MatriculaContext ctx, CatalogService catalog)
    {
        _ctx = ctx;
        _catalog = catalog;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _ctx.Shifts.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new CatalogRow(x.Id, x.Name, _ctx.ClassRoomCourses.Count(c => c.ShiftId == x.Id)))
            .ToListAsync();
        return View(items);
    }

    public IActionResult Create() => View("Form", new NameForm());

    public async Task<IActionResult> Edit(Guid id)
    {
        var e = await _ctx.Shifts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (e is null)
        {
            TempData["Error"] = "El turno no existe.";
            return RedirectToAction("Index");
        }
        return View("Form", new NameForm { Id = e.Id, Name = e.Name });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(NameForm vm) => SaveAsync(vm);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(Guid id, NameForm vm)
    {
        vm.Id = id;
        return SaveAsync(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
        => RedirectWithResult(await _catalog.DeleteShiftAsync(id), "Turno eliminado.");

    private async Task<IActionResult> SaveAsync(NameForm vm)
    {
        if (ModelState.IsValid)
        {
            var result = await _catalog.SaveShiftAsync(new Shift { Id = vm.Id, Name = vm.Name });
            if (result.Success)
            {
                TempData["Success"] = vm.Id == Guid.Empty ? "Turno creado." : "Turno actualizado.";
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", result.Error!);
        }
        return View("Form", vm);
    }
}
