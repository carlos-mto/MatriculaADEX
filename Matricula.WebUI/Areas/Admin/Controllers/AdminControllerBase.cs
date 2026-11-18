using Matricula.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.WebUI.Areas.Admin.Controllers;

public abstract class AdminControllerBase : Controller
{
    // PRG for delete-style actions: result goes to TempData and back to the list.
    protected IActionResult RedirectWithResult(OperationResult result, string successMessage, string action = "Index")
    {
        if (result.Success) TempData["Success"] = successMessage;
        else TempData["Error"] = result.Error;
        return RedirectToAction(action);
    }
}
