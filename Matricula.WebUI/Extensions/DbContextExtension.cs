using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Matricula.WebUI.Extensions;

public static class DbContextExtension
{
  public static WebApplicationBuilder RegisterDbContext(this WebApplicationBuilder builder)
  {
    builder.Services.AddDbContextFactory<MatriculaContext>(opt =>
    {
      opt.UseNpgsql(ConnectionStringFactory.FromEnvironment());
    }, ServiceLifetime.Scoped);

    builder.Services.AddScoped<MatriculaContext>(
    sp => sp.GetRequiredService<IDbContextFactory<MatriculaContext>>()
    .CreateDbContext());

    return builder;
  }

  public static void ExecuteMigrations(this WebApplication app)
  {
    using var serviceScope = app.Services.CreateScope();
    serviceScope
      .ServiceProvider
      .GetRequiredService<IDbContextFactory<MatriculaContext>>()
      .CreateDbContext()
      .Database
      .Migrate();
  }
}