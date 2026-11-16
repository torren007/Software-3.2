// API/Services/AutenticacionService.cs (NUEVO)
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PizzeriaBackend.Data;
using PizzeriaBackend.Models;

namespace PizzeriaBackend.Services;

public interface IAutenticacionService
{
    Task<Usuario?> ValidarAsync(string email, string password);
    Task EntrarAsync(HttpContext http, Usuario usuario, bool persistente);
    Task SalirAsync(HttpContext http);
}

public class AutenticacionService(PizzeriaDb db) : IAutenticacionService
{
    // Hash idéntico al SHA2(password, 256) que usa MySQL en el seed del .sql
    private static string Hashear(string password)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();

    public async Task<Usuario?> ValidarAsync(string email, string password)
    {
        var hash = Hashear(password);
        return await db.Usuarios.FirstOrDefaultAsync(
            u => u.Activo && u.Email == email && u.PasswordHash == hash);
    }

    // Este método es el que materializa la cookie en el navegador
    public async Task EntrarAsync(HttpContext http, Usuario usuario, bool persistente)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol)
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await http.SignInAsync(
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties
            {
                IsPersistent = persistente,                    // "Mantenerme logeado"
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            });
    }

    public Task SalirAsync(HttpContext http) => http.SignOutAsync();
}