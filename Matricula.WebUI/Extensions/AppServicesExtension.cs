using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Matricula.Core.Interfaces;
using Matricula.Infrastructure.Repositories;
using Matricula.Infrastructure.Services;

namespace Matricula.WebUI.Extensions;

public static class AppServicesExtension
{
  public static void RegisterAppServices(this WebApplicationBuilder builder)
  {
    builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<EnrollmentService>();
  }
}
