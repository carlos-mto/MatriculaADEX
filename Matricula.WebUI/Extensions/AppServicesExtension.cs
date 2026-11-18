using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Matricula.Infrastructure.Services;

namespace Matricula.WebUI.Extensions;

public static class AppServicesExtension
{
  public static void RegisterAppServices(this WebApplicationBuilder builder)
  {
    builder.Services.Configure<EnrollmentSettings>(builder.Configuration.GetSection("Enrollment"));
    builder.Services.AddScoped<EnrollmentService>();
    builder.Services.AddScoped<PeriodService>();
    builder.Services.AddScoped<ScheduleService>();
    builder.Services.AddScoped<CatalogService>();
    builder.Services.AddScoped<AdminUserService>();
    builder.Services.AddScoped<DashboardService>();
  }
}
