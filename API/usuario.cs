namespace PizzeriaBackend.Models;

public class Usuario
{
    public int Id { get; set; }

    // VARCHAR(150) UNIQUE de la tabla Usuarios
    public string Email { get; set; } = string.Empty;

    // CHAR(64): SHA-256 en hexadecimal, el mismo formato que devuelve
    // SHA2('...', 256) en MySQL. Nunca guardamos la contraseña en claro.
    public string PasswordHash { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = "Cliente";

    // TINYINT(1): 1 = puede entrar, 0 = usuario dado de baja
    public bool Activo { get; set; } = true;
}