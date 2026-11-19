using API.Tests.Ayudas;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;
using System.Security.Claims;
using Xunit;

namespace API.Tests.Unitarias;

/// <summary>ACTIVIDAD 2 — Modelo/lógica de negocio: registro, validación de credenciales y emisión de la cookie.</summary>
public class AutenticacionServiceTests
{
    private static async Task SembrarAsync(BdEnMemoria bd, string email = "ana@mail.com", string password = "clave1234",
        bool activo = true, string nombre = "Ana")
    {
        using var db = bd.Crear();
        db.Usuarios.Add(new Usuario { Nombre = nombre, Email = email, PasswordHash = Hash.Sha256Hex(password), Activo = activo });
        await db.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- hash

    [Fact]
    public void Hash_CoincideConElSha2DeMySqlDelSqlDeSemilla()
    {
        // Valor de SHA2('admin1234', 256): el usuario admin@pizzeria.com del pizzeria.sql tiene que poder entrar.
        Assert.Equal("ac9689e2272427085e35b9d3e3e8bed88cb3434828b43b86fc0596cad4c6e270", Hash.Sha256Hex("admin1234"));
    }

    // ---------------------------------------------------------------- RegistrarAsync

    [Fact]
    public async Task Registrar_UsuarioNuevo_QuedaComoClienteConLaContraseniaHasheada()
    {
        var bd = new BdEnMemoria();
        using var db = bd.Crear();

        var usuario = await new AutenticacionService(db).RegistrarAsync("Ana", "ana@mail.com", "clave1234");

        Assert.NotNull(usuario);
        Assert.Equal("Cliente", usuario!.Rol);
        Assert.True(usuario.Activo);
        Assert.Equal(Hash.Sha256Hex("clave1234"), usuario.PasswordHash);
        Assert.DoesNotContain("clave1234", usuario.PasswordHash);

        using var otroContexto = bd.Crear();
        Assert.Equal(1, await otroContexto.Usuarios.CountAsync());
    }

    [Fact]
    public async Task Registrar_RecortaEspaciosDelNombreYDelEmail()
    {
        using var db = new BdEnMemoria().Crear();

        var usuario = await new AutenticacionService(db).RegistrarAsync("  Ana Pérez ", "  ana@mail.com ", "clave1234");

        Assert.Equal("Ana Pérez", usuario!.Nombre);
        Assert.Equal("ana@mail.com", usuario.Email);
    }

    [Fact]
    public async Task Registrar_EmailRepetido_DevuelveNullYNoDuplica()
    {
        var bd = new BdEnMemoria();
        await SembrarAsync(bd, email: "ana@mail.com");
        using var db = bd.Crear();

        var repetido = await new AutenticacionService(db).RegistrarAsync("Otra Ana", "ana@mail.com", "otraClave99");

        Assert.Null(repetido);
        using var verificacion = bd.Crear();
        Assert.Equal(1, await verificacion.Usuarios.CountAsync());
    }

    // ---------------------------------------------------------------- ValidarAsync

    [Fact]
    public async Task Validar_CredencialesCorrectas_DevuelveElUsuario()
    {
        var bd = new BdEnMemoria();
        await SembrarAsync(bd);
        using var db = bd.Crear();

        var usuario = await new AutenticacionService(db).ValidarAsync("ana@mail.com", "clave1234");

        Assert.NotNull(usuario);
        Assert.Equal("Ana", usuario!.Nombre);
    }

    [Fact]
    public async Task Validar_ContraseniaIncorrecta_DevuelveNull()
    {
        var bd = new BdEnMemoria();
        await SembrarAsync(bd);
        using var db = bd.Crear();

        Assert.Null(await new AutenticacionService(db).ValidarAsync("ana@mail.com", "mala-clave"));
    }

    [Fact]
    public async Task Validar_EmailInexistente_DevuelveNull()
    {
        var bd = new BdEnMemoria();
        await SembrarAsync(bd);
        using var db = bd.Crear();

        Assert.Null(await new AutenticacionService(db).ValidarAsync("nadie@mail.com", "clave1234"));
    }

    [Fact]
    public async Task Validar_UsuarioDadoDeBaja_NoPuedeEntrar()
    {
        var bd = new BdEnMemoria();
        await SembrarAsync(bd, activo: false);
        using var db = bd.Crear();

        Assert.Null(await new AutenticacionService(db).ValidarAsync("ana@mail.com", "clave1234"));
    }

    // ---------------------------------------------------------------- cookie (EntrarAsync / SalirAsync)

    private static (DefaultHttpContext Http, AuthenticationServiceFalso Auth) ContextoConAuthFalsa()
    {
        var auth = new AuthenticationServiceFalso();
        var http = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IAuthenticationService>(auth)
                .BuildServiceProvider()
        };
        return (http, auth);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Entrar_EmiteLaCookieConLosClaimsDelUsuario(bool persistente)
    {
        using var db = new BdEnMemoria().Crear();
        var (http, auth) = ContextoConAuthFalsa();
        var usuario = new Usuario { Nombre = "Ana", Email = "ana@mail.com", Rol = "Cliente" };

        await new AutenticacionService(db).EntrarAsync(http, usuario, persistente);

        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, auth.EsquemaSignIn);
        Assert.Equal("Ana", auth.Principal!.Identity!.Name);
        Assert.Equal("ana@mail.com", auth.Principal.FindFirst(ClaimTypes.Email)!.Value);
        Assert.Equal("Cliente", auth.Principal.FindFirst(ClaimTypes.Role)!.Value);
        Assert.Equal(persistente, auth.Propiedades!.IsPersistent);
    }

    [Fact]
    public async Task Salir_BorraLaCookieDeSesion()
    {
        using var db = new BdEnMemoria().Crear();
        var (http, auth) = ContextoConAuthFalsa();

        await new AutenticacionService(db).SalirAsync(http);

        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, auth.EsquemaSignOut);
    }
}
