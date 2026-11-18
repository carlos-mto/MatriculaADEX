namespace Matricula.Core.Entities;

public class AppSetting : BaseEntity
{
    [Required, MaxLength(100)]
    public string Key { get; set; } = "";

    [Required, MaxLength(500)]
    public string Value { get; set; } = "";
}
