using API.Tests.Ayudas;
using Microsoft.EntityFrameworkCore;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace API.Tests.Resiliencia;

/// <summary>
/// ACTIVIDAD 3 — Manejo de errores y pruebas de resiliencia.
/// Se simulan: entradas inválidas del usuario, fallos de validación, caída de un servicio (la cocina),
/// requests forjados y recursos inexistentes. En todos los casos se verifica que la aplicación
/// responda con un mensaje claro y SIN romper el flujo de uso (nunca un 500 ni una pantalla en blanco).
/// </summary>
public class ErroresYResilienciaTests
{
    private const string MensajeCocinaCaida = "Error de red interno (Cocina no responde)";

    // ================================================================ Caída de la cocina (servicio externo)

    [Fact]
    public async Task Servicio_CocinaCaida_GuardaElPedidoDeTodosModosConEstadoDeError()
    {
        CocinaFalsa.ExigirCocinaCaida();   // nadie escucha en el 5050 → el socket falla
        var bd = new BdEnMemoria();
        using (var semilla = bd.Crear())
        {
            semilla.Pizzas.Add(new Pizza { Variedad = "Muzzarella", Precio = 5500m });
            await semilla.SaveChangesAsync();
        }

        using var db = bd.Crear();
        var pedido = await new PedidoService(db).CrearAsync(new PedidoDTO
        {
            ClienteNombre = "Ana", ClienteDireccion = "Calle 1", PizzaVariedad = "Muzzarella", PizzaPrecio = 5500m, Cantidad = 1
        });

        Assert.Equal(MensajeCocinaCaida, pedido.Estado);

        // …y el estado de error quedó PERSISTIDO: el cliente lo puede ver después.
        using var verificacion = bd.Crear();
        var guardado = await verificacion.Pedidos.SingleAsync();
        Assert.Equal(MensajeCocinaCaida, guardado.Estado);
        Assert.True(guardado.Activo);
    }

    [Fact]
    public async Task Servicio_LaCocinaVuelve_ElSiguientePedidoYaSeNotificaBien()
    {
        var bd = new BdEnMemoria();
        using (var semilla = bd.Crear())
        {
            semilla.Pizzas.Add(new Pizza { Variedad = "Muzzarella", Precio = 5500m });
            await semilla.SaveChangesAsync();
        }
        var dto = new PedidoDTO
        {
            ClienteNombre = "Ana", ClienteDireccion = "Calle 1", PizzaVariedad = "Muzzarella", PizzaPrecio = 5500m, Cantidad = 1
        };

        // 1) cocina caída
        CocinaFalsa.ExigirCocinaCaida();
        using (var db = bd.Crear())
            Assert.Equal(MensajeCocinaCaida, (await new PedidoService(db).CrearAsync(dto)).Estado);

        // 2) la cocina "se reinicia"
        using var cocina = new CocinaFalsa();
        using (var db = bd.Crear())
        {
            var pedido = await new PedidoService(db).CrearAsync(dto);

            Assert.Equal("Espera de confirmación", pedido.Estado);
            Assert.Equal($"NUEVO_PEDIDO:{pedido.Id}", await cocina.EsperarMensajeAsync());
        }
    }

    [Fact]
    public async Task Web_CocinaCaida_LaConfirmacionSeMuestraIgualYAvisaDelProblema()
    {
        CocinaFalsa.ExigirCocinaCaida();
        using var app = new AppFactory();
        await app.SembrarMenuAsync();
        var http = await app.ClienteLogueadoAsync();

        var envio = await http.EnviarFormularioAsync("/Pedido/Agregar", "/Pedido/Agregar",
            ("Form.ClienteNombre", "Ana"), ("Form.ClienteDireccion", "Calle 1"),
            ("Form.Items[0].Variedad", "Muzzarella"), ("Form.Items[0].Cantidad", "1"));

        // El flujo NO se rompe: igual redirige a la confirmación…
        Assert.Equal(HttpStatusCode.Redirect, envio.StatusCode);
        var confirmacion = await http.GetAsync(envio.RutaDeRedireccion());

        // …y la interfaz muestra el estado de error en vez de fallar.
        Assert.Equal(HttpStatusCode.OK, confirmacion.StatusCode);
        Assert.Contains(MensajeCocinaCaida, await confirmacion.LeerHtmlAsync());
    }

    // ================================================================ Entradas inválidas: Login

    [Theory]
    [InlineData("no-es-un-email", "clave1234", "El email no tiene un formato válido.")]
    [InlineData("", "clave1234", "Ingresá tu email.")]
    [InlineData("ana@mail.com", "", "Ingresá tu contraseña.")]
    public async Task Login_ConDatosInvalidos_MuestraElMensajeYNoIniciaSesion(string email, string password, string mensaje)
    {
        using var app = new AppFactory();
        var http = app.NuevoCliente();

        var respuesta = await http.EnviarFormularioAsync("/Login", "/Login", ("Email", email), ("Password", password));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);   // vuelve al formulario, no se rompe
        Assert.Contains(mensaje, await respuesta.LeerHtmlAsync());
        Assert.DoesNotContain(respuesta.Cookies(), c => c.StartsWith("Pizzeria.Auth="));
    }

    [Theory]
    [InlineData("ana@mail.com", "contraseña-equivocada")]   // existe el email, falla la contraseña
    [InlineData("nadie@mail.com", "clave1234")]             // no existe el email
    public async Task Login_ConCredencialesIncorrectas_DaElMismoMensajeGenericoEnAmbosCasos(string email, string password)
    {
        using var app = new AppFactory();
        await app.SembrarUsuarioAsync("ana@mail.com", "clave1234");
        var http = app.NuevoCliente();

        var respuesta = await http.EnviarFormularioAsync("/Login", "/Login", ("Email", email), ("Password", password));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Email o contraseña incorrectos.", await respuesta.LeerHtmlAsync());
        Assert.DoesNotContain(respuesta.Cookies(), c => c.StartsWith("Pizzeria.Auth="));
    }

    [Fact]
    public async Task Login_UsuarioDadoDeBaja_NoPuedeEntrarAunqueLaClaveSeaCorrecta()
    {
        using var app = new AppFactory();
        await app.SembrarUsuarioAsync("ana@mail.com", "clave1234", activo: false);

        var respuesta = await app.NuevoCliente().EnviarFormularioAsync("/Login", "/Login",
            ("Email", "ana@mail.com"), ("Password", "clave1234"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Email o contraseña incorrectos.", await respuesta.LeerHtmlAsync());
    }

    // ================================================================ Entradas inválidas: Registro

    [Theory]
    [InlineData("Ana", "ana@mail.com", "clave1234", "otra-clave-99", "Las contraseñas no coinciden.")]
    [InlineData("Ana", "ana@mail.com", "corta", "corta", "La contraseña tiene que tener al menos 8 caracteres.")]
    [InlineData("Ana", "mail-roto", "clave1234", "clave1234", "El email no tiene un formato válido.")]
    [InlineData("", "ana@mail.com", "clave1234", "clave1234", "Ingresá tu nombre.")]
    public async Task Registro_ConDatosInvalidos_MuestraElMensajeYNoCreaLaCuenta(
        string nombre, string email, string password, string confirmar, string mensaje)
    {
        using var app = new AppFactory();

        var respuesta = await app.NuevoCliente().EnviarFormularioAsync("/Registro", "/Registro",
            ("Nombre", nombre), ("Email", email), ("Password", password), ("ConfirmarPassword", confirmar));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(mensaje, await respuesta.LeerHtmlAsync());
        Assert.Equal(0, await app.LeerBdAsync(db => db.Usuarios.CountAsync()));
    }

    [Fact]
    public async Task Registro_ConEmailYaRegistrado_MuestraElErrorYNoDuplica()
    {
        using var app = new AppFactory();
        await app.SembrarUsuarioAsync("ana@mail.com", "clave1234");

        var respuesta = await app.NuevoCliente().EnviarFormularioAsync("/Registro", "/Registro",
            ("Nombre", "Otra Ana"), ("Email", "ana@mail.com"), ("Password", "otraClave99"), ("ConfirmarPassword", "otraClave99"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Ya existe una cuenta con ese email.", await respuesta.LeerHtmlAsync());
        Assert.Equal(1, await app.LeerBdAsync(db => db.Usuarios.CountAsync()));
    }

    // ================================================================ Entradas inválidas: formulario de pedido

    [Theory]
    [InlineData("", "Calle 1", "Muzzarella", "1", "Ingresá tu nombre.")]
    [InlineData("Ana", "", "Muzzarella", "1", "Ingresá tu dirección de entrega.")]
    [InlineData("Ana", "Calle 1", "Muzzarella", "0", "La cantidad tiene que estar entre 1 y 50.")]
    [InlineData("Ana", "Calle 1", "Muzzarella", "51", "La cantidad tiene que estar entre 1 y 50.")]
    [InlineData("Ana", "Calle 1", "Inexistente", "1", "Alguna de las pizzas ya no está en la carta.")]
    public async Task Pedido_ConDatosInvalidos_MuestraElErrorEnLaPaginaYNoGuardaNada(
        string nombre, string direccion, string variedad, string cantidad, string mensaje)
    {
        using var cocina = new CocinaFalsa();
        using var app = new AppFactory();
        await app.SembrarMenuAsync();
        var http = await app.ClienteLogueadoAsync();

        var respuesta = await http.EnviarFormularioAsync("/Pedido/Agregar", "/Pedido/Agregar",
            ("Form.ClienteNombre", nombre), ("Form.ClienteDireccion", direccion),
            ("Form.Items[0].Variedad", variedad), ("Form.Items[0].Cantidad", cantidad));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);   // se vuelve a mostrar el formulario
        Assert.Contains(mensaje, await respuesta.LeerHtmlAsync());
        Assert.Equal(0, await app.LeerBdAsync(db => db.Pedidos.CountAsync()));
        Assert.Null(await cocina.EsperarMensajeAsync(TimeSpan.FromMilliseconds(300)));
    }

    [Fact]
    public async Task Pedido_SinNingunaPizza_PideAgregarAlMenosUna()
    {
        using var cocina = new CocinaFalsa();
        using var app = new AppFactory();
        await app.SembrarMenuAsync();
        var http = await app.ClienteLogueadoAsync();

        var respuesta = await http.EnviarFormularioAsync("/Pedido/Agregar", "/Pedido/Agregar",
            ("Form.ClienteNombre", "Ana"), ("Form.ClienteDireccion", "Calle 1"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Agregá al menos una pizza al pedido.", await respuesta.LeerHtmlAsync());
        Assert.Equal(0, await app.LeerBdAsync(db => db.Pedidos.CountAsync()));
    }

    [Fact]
    public async Task Pedido_ElFormularioInvalidoConservaLoQueElUsuarioHabiaEscrito()
    {
        using var cocina = new CocinaFalsa();
        using var app = new AppFactory();
        await app.SembrarMenuAsync();
        var http = await app.ClienteLogueadoAsync();

        var respuesta = await http.EnviarFormularioAsync("/Pedido/Agregar", "/Pedido/Agregar",
            ("Form.ClienteNombre", "Ana Pérez"), ("Form.ClienteDireccion", ""),   // falta la dirección
            ("Form.Items[0].Variedad", "Calabresa"), ("Form.Items[0].Cantidad", "3"));

        var html = await respuesta.LeerHtmlAsync();
        Assert.Contains("Ana Pérez", html);                       // el nombre no se pierde
        Assert.Matches(new Regex("\"variedad\":\\s*\"Calabresa\""), html);   // ni las pizzas ya armadas (INICIAL del JS)
    }

    // ================================================================ Seguridad / requests forjados

    [Fact]
    public async Task Pedido_PostSinTokenAntiforgery_SeRechazaConElErrorDelCliente()
    {
        using var cocina = new CocinaFalsa();
        using var app = new AppFactory();
        await app.SembrarMenuAsync();
        var http = await app.ClienteLogueadoAsync();

        var respuesta = await http.PostAsync("/Pedido/Agregar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Form.ClienteNombre"] = "Ana",
            ["Form.ClienteDireccion"] = "Calle 1",
            ["Form.Items[0].Variedad"] = "Muzzarella",
            ["Form.Items[0].Cantidad"] = "1"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, await app.LeerBdAsync(db => db.Pedidos.CountAsync()));
    }

    [Theory]
    [InlineData("/Pedido/Agregar")]
    [InlineData("/Pedido/Agregar?variedad=Calabresa")]
    [InlineData("/Pedido/Confirmacion/1")]
    public async Task SinSesion_LasPaginasPrivadasRedirigenAlLoginConservandoElDestino(string ruta)
    {
        using var app = new AppFactory();

        var respuesta = await app.NuevoCliente().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);   // 302, no 401 ni 500
        var destino = respuesta.RutaDeRedireccion();
        Assert.StartsWith("/Login?ReturnUrl=", destino);
        Assert.Equal(ruta, Uri.UnescapeDataString(destino["/Login?ReturnUrl=".Length..]));
    }

    [Fact]
    public async Task Confirmacion_DePedidoInexistente_Devuelve404EnVezDeRomperse()
    {
        using var app = new AppFactory();
        var http = await app.ClienteLogueadoAsync();

        var respuesta = await http.GetAsync("/Pedido/Confirmacion/99999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    // ================================================================ API REST (ClienteApp / CocinaApp)

    [Fact]
    public async Task Api_PostConJsonMalformado_Devuelve400()
    {
        using var app = new AppFactory();

        var respuesta = await app.NuevoCliente().PostAsync("/pedidos",
            new StringContent("{ esto no es json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Api_ActualizarEstadoDeUnPedidoInexistente_Devuelve404()
    {
        using var app = new AppFactory();

        var respuesta = await app.NuevoCliente().PutAsync("/pedidos/999/estado?nuevoEstado=Listo", null);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Api_BorrarUnPedidoInexistente_Devuelve404()
    {
        using var app = new AppFactory();

        var respuesta = await app.NuevoCliente().DeleteAsync("/pedidos/999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    /// <summary>
    /// HALLAZGO documentado (el repo está cerrado, por eso no se corrige): POST /pedidos recibe un PedidoDTO
    /// con DataAnnotations, pero el endpoint mínimo de Program.cs no las valida (no hay AddValidation ni
    /// TryValidateObject), así que un body vacío crearía un cliente "" y una pizza "" de precio 0.
    /// Para activar el test: quitar el Skip. Debería fallar hasta que la API valide el DTO y devuelva 400.
    /// </summary>
    [Fact(Skip = "HALLAZGO: según el código, POST /pedidos no valida PedidoDTO. Ver comentario del test y Tests/README.md.")]
    public async Task Api_PostConDtoVacio_DeberiaRechazarseConValidacion()
    {
        using var cocina = new CocinaFalsa();
        using var app = new AppFactory();

        var respuesta = await app.NuevoCliente().PostAsJsonAsync("/pedidos", new PedidoDTO());

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, await app.LeerBdAsync(db => db.Pedidos.CountAsync()));
    }
}
