namespace Matricula.WebUI.Models;

public class LoginModel
{
    [Required(ErrorMessage = "Ingrese el usuario.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Ingrese la contraseña.")]
    public string Password { get; set; } = "";
}
