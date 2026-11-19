using System.ComponentModel.DataAnnotations;

namespace PizzeriaBackend.Models;

// Formulario de "Crear cuenta". Ojo: NO tiene campo Rol.
// Quien se registra siempre queda como "Cliente"; el rol nunca viene del formulario.
public class RegistroViewModel
{
    [Required(ErrorMessage = "Ingresá tu nombre.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede pasar de 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu email.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [MaxLength(150, ErrorMessage = "El email no puede pasar de 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá una contraseña.")]
    [MinLength(8, ErrorMessage = "La contraseña tiene que tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repetí la contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Repetir contraseña")]
    public string ConfirmarPassword { get; set; } = string.Empty;

    // Para volver a donde ibas (ej: /Pedido/Agregar?variedad=Calabresa)
    public string? ReturnUrl { get; set; }
}
