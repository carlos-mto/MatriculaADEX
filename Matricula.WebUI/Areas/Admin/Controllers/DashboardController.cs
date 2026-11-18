using Matricula.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DashboardController : AdminControllerBase
{
    private readonly DashboardService _dashboard;
    private readonly PeriodService _period;

    public DashboardController(DashboardService dashboard, PeriodService period)
    {
        _dashboard = dashboard;
        _period = period;
    }

    public async Task<IActionResult> Index()
    {
        var period = await _period.GetActivePeriodAsync();
        ViewData["Period"] = period;
        return View(await _dashboard.GetAsync(period));
    }
}
