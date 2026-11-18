using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace Matricula.WebUI.Extensions;

public static class AuthenticationExtension
{
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
