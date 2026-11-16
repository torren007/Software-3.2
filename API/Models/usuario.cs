// API/usuario.cs (NUEVO)
namespace PizzeriaBackend.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // CHAR(64) SHA-256 hex
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = "Cliente";
    public bool Activo { get; set; } = true;
}