using API.Tests.Ayudas;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace API.Tests.Funcionales;

/// <summary>
/// ACTIVIDAD 4 — Pruebas funcionales completas Vista → Controlador → Servicio → BD → Vista.
/// Se usa la aplicación real (Razor + cookies + antiforgery + EF) y un "navegador" HTTP que abre los
/// formularios, los envía con su token y sigue las redirecciones. Solo cambian dos cosas respecto de producción:
/// la BD es en memoria y la cocina es un listener falso en el puerto 5050.
/// </summary>
public class FlujosWebTests
{
    // ================================================================ FUNCIONAL 1
    // Registro → formulario de pedido → envío → comprobante, con la cocina recibiendo el aviso.

    [Fact]
    public async Task Funcional1_RegistroPedidoYComprobante_RecorreTodoElFlujoDeDatos()
    {
        using var cocina = new CocinaFalsa();
        using var app = new AppFactory();
        await app.SembrarMenuAsync();
        var http = app.NuevoCliente();

        // 1) VISTA → CONTROLADOR: el usuario se registra desde el formulario y queda con sesión iniciada
        var registro = await http.EnviarFormularioAsync("/Registro", "/Registro",
            ("Nombre", "Ana"), ("Email", "ana@mail.com"), ("Password", "clave1234"), ("ConfirmarPassword", "clave1234"));
        Assert.Equal(HttpStatusCode.Redirect, registro.StatusCode);
        Assert.Equal("/", registro.RutaDeRedireccion());
        Assert.Contains(registro.Cookies(), c => c.StartsWith("Pizzeria.Auth="));

        // 2) BD → VISTA: el formulario muestra la carta que está en la base de datos
        var formulario = await http.GetAsync("/Pedido/Agregar");
        Assert.Equal(HttpStatusCode.OK, formulario.StatusCode);
        var htmlFormulario = await formulario.LeerHtmlAsync();
        Assert.Contains("Muzzarella", htmlFormulario);
        Assert.Contains("Calabresa", htmlFormulario);
        Assert.Contains("Pepperoni Especial", htmlFormulario);

        // 3) VISTA → CONTROLADOR → SERVICIO → BD: se envía el pedido (2 Muzzarella + 1 Calabresa)
        var envio = await http.EnviarFormularioAsync("/Pedido/Agregar", "/Pedido/Agregar",
            ("Form.ClienteNombre", "Ana Pérez"), ("Form.ClienteDireccion", "Av. San Martín 1234"),
            ("Form.Items[0].Variedad", "Muzzarella"), ("Form.Items[0].Cantidad", "2"),
            ("Form.Items[1].Variedad", "Calabresa"), ("Form.Items[1].Cantidad", "1"));
        Assert.Equal(HttpStatusCode.Redirect, envio.StatusCode);
        var coincidencia = Regex.Match(envio.RutaDeRedireccion(), @"^/Pedido/Confirmacion/(\d+)$");
        Assert.True(coincidencia.Success, $"Redirigió a '{envio.RutaDeRedireccion()}' en vez de a la confirmación.");
        var id = int.Parse(coincidencia.Groups[1].Value);

        // 4) BD → VISTA: el comprobante muestra los datos reales del pedido guardado
        var comprobante = await http.GetAsync($"/Pedido/Confirmacion/{id}");
        Assert.Equal(HttpStatusCode.OK, comprobante.StatusCode);
        var html = await comprobante.LeerHtmlAsync();
        Assert.Contains($"Pedido #{id} recibido", html);
        Assert.Contains("Ana Pérez", html);
        Assert.Contains("Av. San Martín 1234", html);
        Assert.Contains("Muzzarella", html);
        Assert.Contains("Calabresa", html);
        Assert.Contains("Espera de confirmación", html);
        Assert.Matches(@"\$17[.,]800", html);   // 2 × 5.500 + 1 × 6.800 (el separador depende de la cultura del equipo)

        // 5) Lo guardado en la base coincide con lo que se envió
        var pedido = await app.LeerBdAsync(db => db.Pedidos
            .Include(p => p.Cliente).Include(p => p.Detalles).ThenInclude(d => d.Pizza)
            .SingleAsync(p => p.Id == id));
        Assert.Equal("Ana Pérez", pedido.Cliente!.Nombre);
        Assert.Equal("Av. San Martín 1234", pedido.Cliente.Direccion);
        Assert.True(pedido.Activo);
        Assert.Equal(2, pedido.Detalles.Single(d => d.Pizza!.Variedad == "Muzzarella").Cantidad);
        Assert.Equal(1, pedido.Detalles.Single(d => d.Pizza!.Variedad == "Calabresa").Cantidad);

        // 6) SERVICIO → COCINA: por el socket llegó el aviso del pedido nuevo
        Assert.Equal($"NUEVO_PEDIDO:{id}", await cocina.EsperarMensajeAsync());
    }

    // ================================================================ FUNCIONAL 2
    // Intento de pedir sin sesión → login → vuelta automática al formulario con la pizza elegida en la carta.

    [Fact]
    public async Task Funcional2_LoginConReturnUrl_VuelveAlFormularioConLaPizzaPreseleccionada()
    {
        using var app = new AppFactory();
        await app.SembrarMenuAsync();
        await app.SembrarUsuarioAsync("ana@mail.com", "clave1234");
        var http = app.NuevoCliente();

        // 1) El "+" del landing apunta a /Pedido/Agregar?variedad=Calabresa: sin sesión, [Authorize] manda al login
        var sinSesion = await http.GetAsync("/Pedido/Agregar?variedad=Calabresa");
        Assert.Equal(HttpStatusCode.Redirect, sinSesion.StatusCode);
        var rutaLogin = sinSesion.RutaDeRedireccion();
        Assert.StartsWith("/Login?ReturnUrl=", rutaLogin);

        // 2) La vista de login recuerda a dónde se iba
        var paginaLogin = await http.GetAsync(rutaLogin);
        Assert.Equal(HttpStatusCode.OK, paginaLogin.StatusCode);
        Assert.Contains("Iniciá sesión para continuar con tu pedido.", await paginaLogin.LeerHtmlAsync());

        // 3) Se envía el login con el ReturnUrl que viajó en el formulario
        var destino = Uri.UnescapeDataString(rutaLogin["/Login?ReturnUrl=".Length..]);
        var login = await http.EnviarFormularioAsync(rutaLogin, "/Login",
            ("Email", "ana@mail.com"), ("Password", "clave1234"), ("ReturnUrl", destino));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/Pedido/Agregar?variedad=Calabresa", login.RutaDeRedireccion());

        // 4) Ya con la cookie, se llega al formulario y la card de Calabresa viene marcada
        var formulario = await http.GetAsync(login.RutaDeRedireccion());
        Assert.Equal(HttpStatusCode.OK, formulario.StatusCode);
        Assert.Matches(new Regex("class=\"pizza-card seleccionada\"[^>]*data-variedad=\"Calabresa\""), await formulario.Content.ReadAsStringAsync());
    }

    // ================================================================ Cookies de sesión

    [Fact]
    public async Task Cookie_LoginNormal_EmiteCookieDeSesionHttpOnlySinFechaDeVencimiento()
    {
        using var app = new AppFactory();
        await app.SembrarUsuarioAsync("ana@mail.com", "clave1234");

        var login = await app.NuevoCliente().EnviarFormularioAsync("/Login", "/Login",
            ("Email", "ana@mail.com"), ("Password", "clave1234"));

        var cookie = Assert.Single(login.Cookies(), c => c.StartsWith("Pizzeria.Auth="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);   // muere al cerrar el navegador
    }

    [Fact]
    public async Task Cookie_MantenermeLogeado_EmiteCookiePersistenteConVencimiento()
    {
        using var app = new AppFactory();
        await app.SembrarUsuarioAsync("ana@mail.com", "clave1234");

        var login = await app.NuevoCliente().EnviarFormularioAsync("/Login", "/Login",
            ("Email", "ana@mail.com"), ("Password", "clave1234"), ("Persistente", "true"));

        var cookie = Assert.Single(login.Cookies(), c => c.StartsWith("Pizzeria.Auth="));
        Assert.Contains("expires=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cookie_Salir_BorraLaSesionYLasPaginasPrivadasVuelvenAPedirLogin()
    {
        using var app = new AppFactory();
        var http = await app.ClienteLogueadoAsync();
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/Pedido/Agregar")).StatusCode);   // con sesión entra

        // El botón "Salir" del header es un <form method=post> en la página de inicio
        var salir = await http.EnviarFormularioAsync("/", "/Login/Salir");

        Assert.Equal(HttpStatusCode.Redirect, salir.StatusCode);
        Assert.Equal("/", salir.RutaDeRedireccion());
        var cookie = Assert.Single(salir.Cookies(), c => c.StartsWith("Pizzeria.Auth="));
        Assert.Contains("1970", cookie);   // la cookie se manda vencida: el navegador la descarta
        Assert.Equal(HttpStatusCode.Redirect, (await http.GetAsync("/Pedido/Agregar")).StatusCode);
    }

    [Theory]
    [InlineData("https://sitio-malicioso.com/robar")]
    [InlineData("//sitio-malicioso.com")]
    public async Task Login_ConReturnUrlExterno_NoRedirigeAFueraDelSitio(string returnUrlMalicioso)
    {
        using var app = new AppFactory();
        await app.SembrarUsuarioAsync("ana@mail.com", "clave1234");

        var login = await app.NuevoCliente().EnviarFormularioAsync("/Login", "/Login",
            ("Email", "ana@mail.com"), ("Password", "clave1234"), ("ReturnUrl", returnUrlMalicioso));

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.RutaDeRedireccion());   // protección contra open redirect
    }

    // ================================================================ Vistas que dependen de la sesión y de la BD

    [Fact]
    public async Task Home_EsPublica_MuestraLaCartaDeLaBaseYElHeaderCambiaConLaSesion()
    {
        using var app = new AppFactory();
        await app.SembrarMenuAsync();
        var http = app.NuevoCliente();

        // Anónimo: ve la carta (viene de la BD) y el link para iniciar sesión
        var anonimo = await http.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, anonimo.StatusCode);
        var htmlAnonimo = await anonimo.LeerHtmlAsync();
        Assert.Contains("Muzzarella", htmlAnonimo);
        Assert.Contains("Pepperoni Especial", htmlAnonimo);
        Assert.Contains("Iniciar sesión", htmlAnonimo);
        Assert.DoesNotContain("Hola, Ana", htmlAnonimo);

        // Logueado: el header saluda por el nombre y ofrece "Salir"
        await app.SembrarUsuarioAsync("ana@mail.com", "clave1234", nombre: "Ana");
        await http.EnviarFormularioAsync("/Login", "/Login", ("Email", "ana@mail.com"), ("Password", "clave1234"));
        var htmlLogueado = await (await http.GetAsync("/")).LeerHtmlAsync();
        Assert.Contains("Hola, Ana", htmlLogueado);
        Assert.Contains("Salir", htmlLogueado);
        Assert.DoesNotContain("Iniciar sesión", htmlLogueado);
    }

    [Fact]
    public async Task Home_ConLaCartaVacia_NoSeRompe()
    {
        using var app = new AppFactory();

        var respuesta = await app.NuevoCliente().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
