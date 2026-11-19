using API.Tests.Ayudas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PizzeriaBackend.Controllers;
using PizzeriaBackend.Models;
using Xunit;

namespace API.Tests.Unitarias;

/// <summary>
/// ACTIVIDAD 2 — Controlador: cada acción se prueba aislada, con servicios de mentira (sin BD, sin HTTP).
/// Se verifica QUÉ devuelve el controlador (vista, redirección, 404) y QUÉ le pide al servicio.
/// Los caminos que dependen de Url.IsLocalUrl (login/registro exitosos) se prueban de punta a punta
/// en Funcionales/, donde el pipeline real provee el IUrlHelper.
/// </summary>
public class ControllersTests
{
    // ================================================================ HomeController

    [Fact]
    public async Task Home_Index_PasaLaCartaDelServicioALaVista()
    {
        var servicio = new PedidoServiceFalso
        {
            Menu = new List<Pizza> { new() { Id = 1, Variedad = "Muzzarella", Precio = 5500m } }
        };

        var resultado = await new HomeController(servicio).Index();

        var vista = Assert.IsType<ViewResult>(resultado);
        var modelo = Assert.IsType<List<Pizza>>(vista.Model);
        Assert.Equal("Muzzarella", Assert.Single(modelo).Variedad);
    }

    // ================================================================ PedidoController

    [Fact]
    public async Task Pedido_AgregarGet_ArmaElViewModelConLaCartaYLaPizzaPreseleccionada()
    {
        var servicio = new PedidoServiceFalso { Menu = new List<Pizza> { new() { Variedad = "Calabresa" } } };

        var resultado = await new PedidoController(servicio).Agregar("Calabresa");

        var vm = Assert.IsType<AgregarVM>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Single(vm.Pizzas);
        Assert.Equal("Calabresa", vm.VariedadPrevia);
    }

    [Fact]
    public async Task Pedido_AgregarPost_ConDatosInvalidos_VuelveALaVistaSinCrearElPedido()
    {
        var servicio = new PedidoServiceFalso();
        var controlador = new PedidoController(servicio);
        controlador.ModelState.AddModelError("Form.ClienteNombre", "Ingresá tu nombre.");

        var resultado = await controlador.Agregar(new AgregarVM());

        Assert.IsType<ViewResult>(resultado);
        Assert.Equal(0, servicio.LlamadasCrearDesdeMenu);
    }

    [Fact]
    public async Task Pedido_AgregarPost_Valido_RedirigeALaConfirmacionDelPedidoCreado()
    {
        var servicio = new PedidoServiceFalso { PedidoAGuardar = new Pedido { Id = 7 } };
        var vm = new AgregarVM { Form = new NuevoPedidoDTO { ClienteNombre = "Ana", ClienteDireccion = "Calle 1" } };

        var resultado = await new PedidoController(servicio).Agregar(vm);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Confirmacion", redireccion.ActionName);
        Assert.Equal(7, redireccion.RouteValues!["id"]);
        Assert.Equal(1, servicio.LlamadasCrearDesdeMenu);
        Assert.Same(vm.Form, servicio.UltimoFormulario);
    }

    [Fact]
    public async Task Pedido_AgregarPost_ConPizzaQueYaNoEstaEnLaCarta_MuestraUnErrorClaro()
    {
        var servicio = new PedidoServiceFalso { PedidoAGuardar = null };   // el servicio devuelve null
        var controlador = new PedidoController(servicio);

        var resultado = await controlador.Agregar(new AgregarVM());

        Assert.IsType<ViewResult>(resultado);
        var error = Assert.Single(controlador.ModelState[string.Empty]!.Errors);
        Assert.Equal("Alguna de las pizzas ya no está en la carta.", error.ErrorMessage);
    }

    [Fact]
    public async Task Pedido_Confirmacion_PedidoExistente_MuestraElComprobante()
    {
        var servicio = new PedidoServiceFalso { PedidoExistente = new Pedido { Id = 5 } };

        var resultado = await new PedidoController(servicio).Confirmacion(5);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Equal(5, Assert.IsType<Pedido>(vista.Model).Id);
    }

    [Fact]
    public async Task Pedido_Confirmacion_PedidoInexistente_Devuelve404()
    {
        var resultado = await new PedidoController(new PedidoServiceFalso()).Confirmacion(123);

        Assert.IsType<NotFoundResult>(resultado);
    }

    // ================================================================ LoginController

    [Fact]
    public void Login_Get_ConservaElReturnUrlEnElModelo()
    {
        var resultado = new LoginController(new AutenticacionServiceFalso()).Index("/Pedido/Agregar");

        var vm = Assert.IsType<LoginViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("/Pedido/Agregar", vm.ReturnUrl);
    }

    [Fact]
    public async Task Login_Post_ConModeloInvalido_VuelveALaVistaSinConsultarCredenciales()
    {
        var auth = new AutenticacionServiceFalso();
        var controlador = new LoginController(auth);
        controlador.ModelState.AddModelError("Email", "El email no tiene un formato válido.");
        var vm = new LoginViewModel { Email = "mal", Password = "x" };

        var resultado = await controlador.Index(vm);

        Assert.Same(vm, Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(0, auth.Entradas);
    }

    [Fact]
    public async Task Login_Post_ConCredencialesIncorrectas_MuestraUnMensajeGenericoYNoIniciaSesion()
    {
        var auth = new AutenticacionServiceFalso { UsuarioValido = null };
        var controlador = new LoginController(auth);

        var resultado = await controlador.Index(new LoginViewModel { Email = "ana@mail.com", Password = "mala" });

        Assert.IsType<ViewResult>(resultado);
        var error = Assert.Single(controlador.ModelState[string.Empty]!.Errors);
        Assert.Equal("Email o contraseña incorrectos.", error.ErrorMessage);   // no revela cuál de los dos falló
        Assert.Equal(0, auth.Entradas);
    }

    [Fact]
    public async Task Login_Salir_CierraLaSesionYVuelveAlInicio()
    {
        var auth = new AutenticacionServiceFalso();
        var controlador = new LoginController(auth)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var resultado = await controlador.Salir();

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redireccion.ActionName);
        Assert.Equal("Home", redireccion.ControllerName);
        Assert.Equal(1, auth.Salidas);
    }

    // ================================================================ RegistroController

    [Fact]
    public void Registro_Get_ConservaElReturnUrlEnElModelo()
    {
        var resultado = new RegistroController(new AutenticacionServiceFalso()).Index("/Pedido/Agregar");

        var vm = Assert.IsType<RegistroViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal("/Pedido/Agregar", vm.ReturnUrl);
    }

    [Fact]
    public async Task Registro_Post_ConModeloInvalido_VuelveALaVistaSinCrearLaCuenta()
    {
        var auth = new AutenticacionServiceFalso();
        var controlador = new RegistroController(auth);
        controlador.ModelState.AddModelError("Password", "La contraseña tiene que tener al menos 8 caracteres.");

        var resultado = await controlador.Index(new RegistroViewModel());

        Assert.IsType<ViewResult>(resultado);
        Assert.Equal(0, auth.Entradas);
    }

    [Fact]
    public async Task Registro_Post_ConEmailYaRegistrado_MarcaElErrorEnElCampoEmail()
    {
        var auth = new AutenticacionServiceFalso { UsuarioRegistrado = null };   // el servicio dice "ya existe"
        var controlador = new RegistroController(auth);
        var vm = new RegistroViewModel { Nombre = "Ana", Email = "ana@mail.com", Password = "clave1234", ConfirmarPassword = "clave1234" };

        var resultado = await controlador.Index(vm);

        Assert.IsType<ViewResult>(resultado);
        var error = Assert.Single(controlador.ModelState["Email"]!.Errors);
        Assert.Equal("Ya existe una cuenta con ese email.", error.ErrorMessage);
        Assert.Equal(0, auth.Entradas);
    }
}
