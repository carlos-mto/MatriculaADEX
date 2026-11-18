namespace Matricula.WebUI.Areas.Admin.Models;

public class NameForm
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar {1} caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = "";
}

public class StudentForm
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre completo.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar {1} caracteres.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = "";

    [Range(1, 10, ErrorMessage = "El ciclo debe estar entre {1} y {2}.")]
    [Display(Name = "Ciclo")]
    public int Cycle { get; set; } = 1;
}

public class TeacherForm
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre completo.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar {1} caracteres.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = "";

    [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
    [StringLength(200, ErrorMessage = "El correo no puede superar {1} caracteres.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(150, ErrorMessage = "La especialidad no puede superar {1} caracteres.")]
    [Display(Name = "Especialidad")]
    public string? Specialty { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;
}

public class ScheduleForm
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Seleccione un curso.")]
    [Display(Name = "Curso")]
    public Guid? CourseId { get; set; }

    [Required(ErrorMessage = "Seleccione un aula.")]
    [Display(Name = "Aula")]
    public Guid? ClassRoomId { get; set; }

    [Required(ErrorMessage = "Seleccione un turno.")]
    [Display(Name = "Turno")]
    public Guid? ShiftId { get; set; }

    [Display(Name = "Profesor")]
    public Guid? TeacherId { get; set; }

    [Range(1, 10, ErrorMessage = "El ciclo debe estar entre {1} y {2}.")]
    [Display(Name = "Ciclo")]
    public int Cycle { get; set; } = 1;

    [Required(ErrorMessage = "Ingrese la hora de inicio.")]
    [Display(Name = "Hora inicio")]
    public string StartTime { get; set; } = "";

    [Required(ErrorMessage = "Ingrese la hora de fin.")]
    [Display(Name = "Hora fin")]
    public string EndTime { get; set; } = "";

    [Range(0, 1000, ErrorMessage = "Los cupos deben estar entre {1} y {2}.")]
    [Display(Name = "Cupos disponibles")]
    public int Capacity { get; set; }

    [Required(ErrorMessage = "Ingrese el periodo.")]
    [StringLength(20, ErrorMessage = "El periodo no puede superar {1} caracteres.")]
    [Display(Name = "Periodo")]
    public string Period { get; set; } = "";

    public uint Version { get; set; }
}

public class PeriodForm
{
    [Required(ErrorMessage = "Ingrese el periodo.")]
    [StringLength(20, ErrorMessage = "El periodo no puede superar {1} caracteres.")]
    [Display(Name = "Periodo")]
    public string Period { get; set; } = "";
}

public class PasswordForm
{
    [Required(ErrorMessage = "Ingrese la contraseña actual.")]
    [Display(Name = "Contraseña actual")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La nueva contraseña debe tener al menos {2} caracteres.")]
    [Display(Name = "Nueva contraseña")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirme la nueva contraseña.")]
    [Compare(nameof(NewPassword), ErrorMessage = "La confirmación no coincide con la nueva contraseña.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = "";
}

public class SettingsViewModel
{
    public string ActivePeriod { get; set; } = "";
    public PeriodForm PeriodForm { get; set; } = new();
    public PasswordForm PasswordForm { get; set; } = new();
}
