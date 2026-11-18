using System.Security.Claims;
using Matricula.Infrastructure.Services;
using Matricula.WebUI.Extensions;
using Matricula.WebUI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Matricula.WebUI.Controllers;

public class AccountController : Controller
{
  private static readonly string DummySalt = SecurityHelper.GenerateSalt();
  private static readonly string DummyHash = SecurityHelper.HashPassword("dummy-password", DummySalt);

  private readonly MatriculaContext _context;

  public AccountController(MatriculaContext context)
  {
    _context = context;
  }

  [AllowAnonymous]
  public IActionResult Login(string? returnUrl = null)
  {
    if (User.Identity?.IsAuthenticated == true)
      return RedirectToAction("Index", "Home");
    ViewData["ReturnUrl"] = returnUrl;
    return View(new LoginModel());
  }

  [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("login")]
  public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
  {
    ViewData["ReturnUrl"] = returnUrl;
    if (!ModelState.IsValid)
      return View(model);

    var user = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Username == model.Username);
    var valid = SecurityHelper.VerifyPassword(model.Password, user?.Salt ?? DummySalt, user?.PasswordHash ?? DummyHash);
    if (user is null || !valid)
    {
      ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
      return View(model);
    }

    var claims = new List<Claim>
    {
      new(ClaimTypes.Name, user.Username),
      new(ClaimTypes.NameIdentifier, user.Id.ToString()),
      new(AuthenticationExtension.SecurityStampClaim, user.SecurityStamp)
    };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
      return Redirect(returnUrl);
    return RedirectToAction("Index", "Home");
  }

  [HttpPost, ValidateAntiForgeryToken]
  public async Task<IActionResult> Logout()
  {
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return RedirectToAction("Login");
  }
}
