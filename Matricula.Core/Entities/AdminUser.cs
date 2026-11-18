namespace Matricula.Core.Entities;

public class AdminUser : BaseEntity
{
    [Required, MaxLength(100)]
    public string Username { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    [Required]
    public string Salt { get; set; } = "";
}
