using Matricula.Infrastructure.Data;

namespace Matricula.Infrastructure.Services;

public class AdminUserService
{
    private readonly MatriculaContext _ctx;

    public AdminUserService(MatriculaContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<OperationResult> ChangePasswordAsync(Guid adminId, string current, string newPassword)
    {
        var user = await _ctx.AdminUsers.FirstOrDefaultAsync(u => u.Id == adminId);
        if (user is null) return OperationResult.Fail("El usuario no existe.");

        if (!SecurityHelper.VerifyPassword(current ?? "", user.Salt, user.PasswordHash))
            return OperationResult.Fail("La contraseña actual es incorrecta.");

        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
            return OperationResult.Fail("La nueva contraseña debe tener al menos 8 caracteres.");

        user.Salt = SecurityHelper.GenerateSalt();
        user.PasswordHash = SecurityHelper.HashPassword(newPassword, user.Salt);
        await _ctx.SaveChangesAsync();
        return OperationResult.Ok();
    }
}
