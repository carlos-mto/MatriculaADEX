namespace Matricula.Core.Entities;

public class Teacher : BaseEntity
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = "";

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(150)]
    public string? Specialty { get; set; }

    public bool IsActive { get; set; } = true;
}
