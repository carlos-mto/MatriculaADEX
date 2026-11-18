using Matricula.Infrastructure.Services;

namespace Matricula.Tests;

public class SecurityHelperTests
{
    [Fact]
    public void HashAndVerify_CorrectPassword_ReturnsTrue()
    {
        var salt = SecurityHelper.GenerateSalt();
        var hash = SecurityHelper.HashPassword("Secreta123!", salt);
        Assert.True(SecurityHelper.VerifyPassword("Secreta123!", salt, hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var salt = SecurityHelper.GenerateSalt();
        var hash = SecurityHelper.HashPassword("Secreta123!", salt);
        Assert.False(SecurityHelper.VerifyPassword("otra", salt, hash));
    }

    [Fact]
    public void Verify_GarbageBase64_ReturnsFalse()
    {
        var salt = SecurityHelper.GenerateSalt();
        Assert.False(SecurityHelper.VerifyPassword("x", salt, "no-es-base64!!"));
        Assert.False(SecurityHelper.VerifyPassword("x", "###", "###"));
    }
}
