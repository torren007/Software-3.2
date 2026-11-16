using System.ComponentModel.DataAnnotations;

namespace PizzeriaBackend.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresá tu email."), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu contraseña."), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Mantenerme logeado")] // "Cookies": sesión persistente
    public bool Persistente { get; set; }

    public string? ReturnUrl { get; set; }
}