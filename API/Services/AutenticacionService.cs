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
    Task<Usuario?> RegistrarAsync(string nombre, string email, string password);
    Task EntrarAsync(HttpContext http, Usuario usuario, bool persistente);
    Task SalirAsync(HttpContext http);
}

public class AutenticacionService(PizzeriaDb db) : IAutenticacionService
{
    // SHA-256 en hex minúsculas: byte a byte idéntico al SHA2(..., 256)
    // que usa el INSERT de admin@pizzeria.com en el pizzeria.sql (Parte 1).
    // Nunca se guarda ni se loguea la contraseña en claro.
    private static string Hashear(string password)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)))
            .ToLowerInvariant();

    // Devuelve el usuario solo si existe, está activo y el hash coincide.
    // Si algo falla devuelve null → el controller muestra "email o
    // contraseña incorrectos" sin revelar cuál de los dos falló.
    public async Task<Usuario?> ValidarAsync(string email, string password)
    {
        var hash = Hashear(password);
        return await db.Usuarios.FirstOrDefaultAsync(
            u => u.Activo && u.Email == email && u.PasswordHash == hash);
    }

    // Crea un usuario con rol "Cliente". Devuelve null si el email ya está registrado.
    // El rol NO es parámetro a propósito: nadie puede registrarse como Admin desde la web.
    public async Task<Usuario?> RegistrarAsync(string nombre, string email, string password)
    {
        email = email.Trim();
        if (await db.Usuarios.AnyAsync(u => u.Email == email)) return null;

        var usuario = new Usuario
        {
            Nombre = nombre.Trim(),
            Email = email,
            PasswordHash = Hashear(password)   // mismo hash que usa el login; Rol = "Cliente" por defecto
        };

        db.Usuarios.Add(usuario);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return null; }   // el UNIQUE de Email la frenó (dos registros a la vez)
        return usuario;
    }

    // Materializa la cookie "Pizzeria.Auth" en el navegador
    public async Task EntrarAsync(HttpContext http, Usuario usuario, bool persistente)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol)
        };
        var identidad = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // IsPersistent = checkbox "Mantenerme logeado": la cookie sobrevive
        // al cierre del navegador y expira según el ExpireTimeSpan de Program.cs.
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = persistente });
    }

    public Task SalirAsync(HttpContext http)
        => http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}