using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PizzeriaBackend.Controllers;
using PizzeriaBackend.Data;
using PizzeriaBackend.Models;
using System.Net;
using Xunit;

namespace API.Tests.Ayudas;

/// <summary>
/// Levanta la aplicación REAL (Program.cs: MVC, cookies, endpoints, servicios) dentro del test,
/// sin tocar su código. Lo único que se cambia es la base de datos: en vez de MySQL se usa una
/// en memoria, distinta para cada instancia.
///
/// WebApplicationFactory necesita un tipo del proyecto para ubicar el ensamblado; como la clase
/// Program es interna (top-level statements), se usa HomeController, que es público.
/// </summary>
public class AppFactory : WebApplicationFactory<HomeController>
{
    private readonly string _nombreBd = "app-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Saca la configuración de MySQL (Pomelo) que registró Program.cs…
            var deLaBd = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GenericTypeArguments.Contains(typeof(PizzeriaDb))
                        && (d.ServiceType == typeof(DbContextOptions<PizzeriaDb>)
                            || d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))))
                .ToList();
            foreach (var descriptor in deLaBd) services.Remove(descriptor);

            // …y pone la BD en memoria.
            services.AddDbContext<PizzeriaDb>(o => o.UseInMemoryDatabase(_nombreBd));
        });
    }

    /// <summary>Cliente HTTP "navegador": guarda cookies y NO sigue redirecciones solo (así se las puede verificar).</summary>
    public HttpClient NuevoCliente() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    // ---------- Acceso directo a la BD en memoria (para preparar datos y verificar resultados) ----------

    public async Task<T> LeerBdAsync<T>(Func<PizzeriaDb, Task<T>> consulta)
    {
        using var scope = Services.CreateScope();
        return await consulta(scope.ServiceProvider.GetRequiredService<PizzeriaDb>());
    }

    public async Task EscribirBdAsync(Func<PizzeriaDb, Task> accion)
    {
        using var scope = Services.CreateScope();
        await accion(scope.ServiceProvider.GetRequiredService<PizzeriaDb>());
    }

    public Task SembrarMenuAsync() => EscribirBdAsync(async db =>
    {
        db.Pizzas.AddRange(
            new Pizza { Variedad = "Muzzarella", Precio = 5500m, Descripcion = "La clásica" },
            new Pizza { Variedad = "Calabresa", Precio = 6800m },
            new Pizza { Variedad = "Pepperoni Especial", Precio = 7200m });
        await db.SaveChangesAsync();
    });

    public async Task SembrarUsuarioAsync(string email, string password, string nombre = "Ana", bool activo = true)
    {
        await EscribirBdAsync(async db =>
        {
            db.Usuarios.Add(new Usuario
            {
                Nombre = nombre,
                Email = email,
                PasswordHash = Hash.Sha256Hex(password),
                Activo = activo
            });
            await db.SaveChangesAsync();
        });
    }

    /// <summary>Crea el usuario y lo loguea de verdad, pasando por el formulario /Login (cookie incluida).</summary>
    public async Task<HttpClient> ClienteLogueadoAsync(
        string email = "ana@mail.com", string password = "clave1234", string nombre = "Ana")
    {
        await SembrarUsuarioAsync(email, password, nombre);
        var http = NuevoCliente();

        var login = await http.EnviarFormularioAsync("/Login", "/Login", ("Email", email), ("Password", password));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return http;
    }
}
