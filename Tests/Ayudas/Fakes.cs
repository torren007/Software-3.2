using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;

namespace API.Tests.Ayudas;

/// <summary>Servicio de pedidos de mentira: permite probar el controlador sin base de datos.</summary>
public class PedidoServiceFalso : IPedidoService
{
    public List<Pizza> Menu { get; set; } = new();
    public Pedido? PedidoAGuardar { get; set; }
    public Pedido? PedidoExistente { get; set; }

    public int LlamadasCrearDesdeMenu { get; private set; }
    public NuevoPedidoDTO? UltimoFormulario { get; private set; }

    public Task<Pedido> CrearAsync(PedidoDTO dto) => throw new NotSupportedException();

    public Task<Pedido?> CrearDesdeMenuAsync(NuevoPedidoDTO dto)
    {
        LlamadasCrearDesdeMenu++;
        UltimoFormulario = dto;
        return Task.FromResult(PedidoAGuardar);
    }

    public Task<Pedido?> ObtenerAsync(int id)
        => Task.FromResult(PedidoExistente is not null && PedidoExistente.Id == id ? PedidoExistente : null);

    public Task<List<Pizza>> ObtenerMenuAsync() => Task.FromResult(Menu);
}

/// <summary>Servicio de autenticación de mentira: cuenta cuántas veces se lo llamó.</summary>
public class AutenticacionServiceFalso : IAutenticacionService
{
    public Usuario? UsuarioValido { get; set; }
    public Usuario? UsuarioRegistrado { get; set; }

    public int Entradas { get; private set; }
    public int Salidas { get; private set; }

    public Task<Usuario?> ValidarAsync(string email, string password) => Task.FromResult(UsuarioValido);
    public Task<Usuario?> RegistrarAsync(string nombre, string email, string password) => Task.FromResult(UsuarioRegistrado);

    public Task EntrarAsync(HttpContext http, Usuario usuario, bool persistente)
    {
        Entradas++;
        return Task.CompletedTask;
    }

    public Task SalirAsync(HttpContext http)
    {
        Salidas++;
        return Task.CompletedTask;
    }
}

/// <summary>
/// IAuthenticationService de mentira: registra lo que AutenticacionService le pide
/// (SignIn / SignOut) sin necesitar el middleware de cookies.
/// </summary>
public class AuthenticationServiceFalso : IAuthenticationService
{
    public string? EsquemaSignIn { get; private set; }
    public ClaimsPrincipal? Principal { get; private set; }
    public AuthenticationProperties? Propiedades { get; private set; }
    public string? EsquemaSignOut { get; private set; }

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        => Task.FromResult(AuthenticateResult.NoResult());

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
    {
        EsquemaSignIn = scheme;
        Principal = principal;
        Propiedades = properties;
        return Task.CompletedTask;
    }

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        EsquemaSignOut = scheme;
        return Task.CompletedTask;
    }
}
