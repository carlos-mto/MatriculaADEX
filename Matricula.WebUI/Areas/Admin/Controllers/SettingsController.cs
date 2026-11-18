using System.Security.Claims;
using Matricula.Infrastructure.Services;
using Matricula.WebUI.Areas.Admin.Models;
using Matricula.WebUI.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class SettingsController : AdminControllerBase
{
    private readonly PeriodService _period;
    private readonly AdminUserService _adminUsers;

    public SettingsController(PeriodService period, AdminUserService adminUsers)
    {
        _period = period;
        _adminUsers = adminUsers;
    }

    public async Task<IActionResult> Index()
    {
        var active = await _period.GetActivePeriodAsync();
        return View(new SettingsViewModel { ActivePeriod = active, PeriodForm = new PeriodForm { Period = active } });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePeriod([Bind(Prefix = "PeriodForm")] PeriodForm form)
    {
        if (ModelState.IsValid)
        {
            var result = await _period.SetActivePeriodAsync(form.Period);
            if (result.Success)
            {
                TempData["Success"] = "Periodo activo actualizado.";
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("PeriodForm", result.Error!);
        }
        return View("Index", new SettingsViewModel { ActivePeriod = await _period.GetActivePeriodAsync(), PeriodForm = form });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "PasswordForm")] PasswordForm form)
    {
        if (ModelState.IsValid)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId))
            {
                ModelState.AddModelError("PasswordForm", "No se pudo identificar al usuario.");
            }
            else
            {
                var result = await _adminUsers.ChangePasswordAsync(adminId, form.CurrentPassword, form.NewPassword);
                if (result.Success)
                {
                    // Other sessions are revoked by the new stamp; keep the current one alive.
                    var stamp = await _adminUsers.GetSecurityStampAsync(adminId);
                    var claims = User.Claims.Where(c => c.Type != AuthenticationExtension.SecurityStampClaim).ToList();
                    claims.Add(new Claim(AuthenticationExtension.SecurityStampClaim, stamp ?? ""));
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
                    TempData["Success"] = "Contraseña actualizada.";
                    return RedirectToAction("Index");
                }
                ModelState.AddModelError("PasswordForm", result.Error!);
            }
        }
        // never echo passwords back
        return View("Index", new SettingsViewModel { ActivePeriod = await _period.GetActivePeriodAsync(), PeriodForm = new PeriodForm { Period = await _period.GetActivePeriodAsync() } });
    }
}
