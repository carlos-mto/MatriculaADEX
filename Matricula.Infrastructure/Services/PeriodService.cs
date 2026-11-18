using System.Text.RegularExpressions;
using Matricula.Infrastructure.Data;
using Matricula.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Matricula.Infrastructure.Services;

public class PeriodService
{
    public const string SettingKey = "ActivePeriod";
    private static readonly Regex Format = new(@"^\d{4}-(I|II|III)$|^\d{4}-\d{1,2}$", RegexOptions.Compiled);

    public static bool IsValidPeriod(string? period) =>
        !string.IsNullOrEmpty(period) && period.Length <= 20 && Format.IsMatch(period);

    private readonly MatriculaContext _ctx;
    private readonly EnrollmentSettings _settings;

    public PeriodService(MatriculaContext ctx, IOptions<EnrollmentSettings> settings)
    {
        _ctx = ctx;
        _settings = settings.Value;
    }

    public async Task<string> GetActivePeriodAsync()
    {
        var value = await _ctx.AppSettings.Where(a => a.Key == SettingKey).Select(a => a.Value).FirstOrDefaultAsync();
        return value ?? _settings.ActivePeriod;
    }

    public async Task<OperationResult> SetActivePeriodAsync(string period)
    {
        period = (period ?? "").Trim();
        if (!IsValidPeriod(period))
            return OperationResult.Fail("Formato de periodo inválido. Ej.: 2026-II");

        var setting = await _ctx.AppSettings.FirstOrDefaultAsync(a => a.Key == SettingKey);
        if (setting is null)
            _ctx.AppSettings.Add(new AppSetting { Key = SettingKey, Value = period });
        else
            setting.Value = period;
        await _ctx.SaveChangesAsync();
        return OperationResult.Ok();
    }
}
