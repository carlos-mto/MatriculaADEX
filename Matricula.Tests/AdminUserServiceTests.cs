using Matricula.Core.Entities;
using Matricula.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Tests;

[Collection("postgres")]
public class AdminUserServiceTests
{
    private readonly PostgresFixture _db;

    public AdminUserServiceTests(PostgresFixture db) => _db = db;

    private async Task<Guid> AddAdminAsync()
    {
        await using var ctx = _db.CreateContext();
        var salt = SecurityHelper.GenerateSalt();
        var admin = new AdminUser { Username = "admin-" + Guid.NewGuid(), Salt = salt, PasswordHash = SecurityHelper.HashPassword("Vieja1234", salt) };
        ctx.AdminUsers.Add(admin);
        await ctx.SaveChangesAsync();
        return admin.Id;
    }

    private async Task<OperationResult> ChangeAsync(Guid id, string current, string next)
    {
        await using var ctx = _db.CreateContext();
        return await new AdminUserService(ctx).ChangePasswordAsync(id, current, next);
    }

    [Fact]
    public async Task WrongCurrent_Fails()
    {
        var id = await AddAdminAsync();
        Assert.False((await ChangeAsync(id, "incorrecta", "Nueva12345")).Success);
    }

    [Fact]
    public async Task ShortNew_Fails()
    {
        var id = await AddAdminAsync();
        var result = await ChangeAsync(id, "Vieja1234", "corta");
        Assert.False(result.Success);
        Assert.Equal("La nueva contraseña debe tener al menos 8 caracteres.", result.Error);
    }

    [Fact]
    public async Task Success_NewPasswordVerifies()
    {
        var id = await AddAdminAsync();
        var result = await ChangeAsync(id, "Vieja1234", "Nueva12345");
        Assert.True(result.Success, result.Error);

        await using var ctx = _db.CreateContext();
        var admin = await ctx.AdminUsers.SingleAsync(a => a.Id == id);
        Assert.True(SecurityHelper.VerifyPassword("Nueva12345", admin.Salt, admin.PasswordHash));
        Assert.False(SecurityHelper.VerifyPassword("Vieja1234", admin.Salt, admin.PasswordHash));
    }
}
