using Matricula.Infrastructure.Services;
using Matricula.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Matricula.Tests;

[Collection("postgres")]
public class PeriodServiceTests
{
    private readonly PostgresFixture _db;

    public PeriodServiceTests(PostgresFixture db) => _db = db;

    private static IOptions<EnrollmentSettings> Config(string period) => Options.Create(new EnrollmentSettings { ActivePeriod = period });

    [Fact]
    public async Task SetAndGet_ValidatesFormat_AndFallsBackToConfig()
    {
        // Single test: the setting is global state in the shared database.
        await using (var ctx = _db.CreateContext())
            Assert.Equal("2030-I", await new PeriodService(ctx, Config("2030-I")).GetActivePeriodAsync());

        await using (var ctx = _db.CreateContext())
        {
            var bad = await new PeriodService(ctx, Config("2030-I")).SetActivePeriodAsync("2026-IV");
            Assert.False(bad.Success);
            Assert.Equal("Formato de periodo inválido. Ej.: 2026-II", bad.Error);
        }

        await using (var ctx = _db.CreateContext())
            Assert.True((await new PeriodService(ctx, Config("2030-I")).SetActivePeriodAsync(" 2031-II ")).Success);
        await using (var ctx = _db.CreateContext())
            Assert.Equal("2031-II", await new PeriodService(ctx, Config("2030-I")).GetActivePeriodAsync());
        await using (var ctx = _db.CreateContext())
            Assert.True((await new PeriodService(ctx, Config("2030-I")).SetActivePeriodAsync("2031-03")).Success);
    }
}
