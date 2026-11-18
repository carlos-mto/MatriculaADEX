namespace Matricula.Core.Entities;

public class AdminUser : BaseEntity
{
    [Required, MaxLength(100)]
    public string Username { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    [Required]
    public string Salt { get; set; } = "";

    [Required, MaxLength(64)]
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
}
