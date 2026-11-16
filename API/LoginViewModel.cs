using System.ComponentModel.DataAnnotations;

namespace PizzeriaBackend.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresá tu email.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // "Mantenerme logeado": si está en true, la cookie es persistente
    // y sobrevive al cierre del navegador.
    [Display(Name = "Mantenerme logeado")]
    public bool Persistente { get; set; }

    // A dónde volver después de loguearse (evita perder la URL que
    // pediste originalmente, ej: /Pedido/Agregar?variedad=Calabresa)
    public string? ReturnUrl { get; set; }
}