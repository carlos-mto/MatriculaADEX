using System.Security.Claims;
using Matricula.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace Matricula.WebUI.Extensions;

public static class AuthenticationExtension
{
  public const string SecurityStampClaim = "sstamp";

  public static WebApplicationBuilder RegisterAuthentication(this WebApplicationBuilder builder)
  {
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
      .AddCookie(o =>
      {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
        // Revoke sessions whose security stamp no longer matches (e.g. after a password change).
        o.Events.OnValidatePrincipal = async context =>
        {
          var stamp = context.Principal?.FindFirst(SecurityStampClaim)?.Value;
          var idValue = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          var current = Guid.TryParse(idValue, out var adminId)
            ? await context.HttpContext.RequestServices.GetRequiredService<AdminUserService>().GetSecurityStampAsync(adminId)
            : null;
          if (current is null || stamp != current)
          {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
          }
        };
      });

    builder.Services.AddAuthorization();

    var keysPath = Environment.GetEnvironmentVariable("DATAPROTECTION_KEYS_PATH");
    if (!string.IsNullOrEmpty(keysPath))
    {
      builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("Matricula");
    }

    return builder;
  }
}
