using System.ComponentModel.DataAnnotations;
using PizzeriaBackend.Models;
using Xunit;

namespace API.Tests.Unitarias;

/// <summary>
/// ACTIVIDAD 2 — Modelo: las reglas de validación (DataAnnotations) de los DTO y ViewModels.
/// Son la primera barrera contra entradas inválidas; el controlador solo mira ModelState.IsValid.
/// </summary>
public class ModelosValidacionTests
{
    private static List<ValidationResult> Validar(object modelo)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, validateAllProperties: true);
        return resultados;
    }

    private static bool TieneMensaje(IEnumerable<ValidationResult> resultados, string mensaje)
        => resultados.Any(r => r.ErrorMessage == mensaje);

    // ---------------------------------------------------------------- PedidoDTO (API / ClienteApp)

    private static PedidoDTO PedidoDtoValido() => new()
    {
        ClienteNombre = "Ana",
        ClienteDireccion = "Calle 1",
        PizzaVariedad = "Muzzarella",
        PizzaPrecio = 5500m,
        Cantidad = 2
    };

    [Fact]
    public void PedidoDTO_Valido_NoTieneErrores()
        => Assert.Empty(Validar(PedidoDtoValido()));

    [Fact]
    public void PedidoDTO_SinNombre_PideElNombre()
    {
        var dto = PedidoDtoValido();
        dto.ClienteNombre = "";

        Assert.True(TieneMensaje(Validar(dto), "Ingresá tu nombre."));
    }

    [Fact]
    public void PedidoDTO_NombreDeMasDe150Caracteres_EsInvalido()
    {
        var dto = PedidoDtoValido();
        dto.ClienteNombre = new string('a', 151);

        Assert.True(TieneMensaje(Validar(dto), "El nombre no puede pasar de 150 caracteres."));
    }

    [Fact]
    public void PedidoDTO_SinDireccion_PideLaDireccion()
    {
        var dto = PedidoDtoValido();
        dto.ClienteDireccion = "";

        Assert.True(TieneMensaje(Validar(dto), "Ingresá tu dirección de entrega."));
    }

    [Fact]
    public void PedidoDTO_SinPizza_PideElegirUna()
    {
        var dto = PedidoDtoValido();
        dto.PizzaVariedad = "";

        Assert.True(TieneMensaje(Validar(dto), "Elegí una pizza de la carta."));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5500)]
    public void PedidoDTO_PrecioCeroONegativo_EsInvalido(double precio)
    {
        var dto = PedidoDtoValido();
        dto.PizzaPrecio = (decimal)precio;

        Assert.True(TieneMensaje(Validar(dto), "El precio no es válido."));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(51)]
    public void PedidoDTO_CantidadFueraDeRango_EsInvalida(int cantidad)
    {
        var dto = PedidoDtoValido();
        dto.Cantidad = cantidad;

        Assert.True(TieneMensaje(Validar(dto), "La cantidad tiene que estar entre 1 y 50."));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void PedidoDTO_CantidadEnLosLimites_EsValida(int cantidad)
    {
        var dto = PedidoDtoValido();
        dto.Cantidad = cantidad;

        Assert.Empty(Validar(dto));
    }

    // ---------------------------------------------------------------- NuevoPedidoDTO (formulario web)

    [Fact]
    public void NuevoPedidoDTO_SinPizzas_EsInvalido()
    {
        var dto = new NuevoPedidoDTO { ClienteNombre = "Ana", ClienteDireccion = "Calle 1" };

        Assert.True(TieneMensaje(Validar(dto), "Agregá al menos una pizza al pedido."));
    }

    [Fact]
    public void NuevoPedidoDTO_ConUnaPizza_EsValido()
    {
        var dto = new NuevoPedidoDTO
        {
            ClienteNombre = "Ana",
            ClienteDireccion = "Calle 1",
            Items = { new ItemPedidoDTO { Variedad = "Muzzarella", Cantidad = 1 } }
        };

        Assert.Empty(Validar(dto));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void ItemPedidoDTO_CantidadFueraDeRango_EsInvalida(int cantidad)
    {
        var item = new ItemPedidoDTO { Variedad = "Muzzarella", Cantidad = cantidad };

        Assert.True(TieneMensaje(Validar(item), "La cantidad tiene que estar entre 1 y 50."));
    }

    [Fact]
    public void ItemPedidoDTO_SinVariedad_EsInvalido()
        => Assert.NotEmpty(Validar(new ItemPedidoDTO { Variedad = "", Cantidad = 1 }));

    // ---------------------------------------------------------------- RegistroViewModel

    private static RegistroViewModel RegistroValido() => new()
    {
        Nombre = "Ana",
        Email = "ana@mail.com",
        Password = "clave1234",
        ConfirmarPassword = "clave1234"
    };

    [Fact]
    public void Registro_Valido_NoTieneErrores()
        => Assert.Empty(Validar(RegistroValido()));

    [Fact]
    public void Registro_ContraseniasDistintas_EsInvalido()
    {
        var vm = RegistroValido();
        vm.ConfirmarPassword = "otraClave99";

        Assert.True(TieneMensaje(Validar(vm), "Las contraseñas no coinciden."));
    }

    [Fact]
    public void Registro_ContraseniaDeMenosDe8Caracteres_EsInvalida()
    {
        var vm = RegistroValido();
        vm.Password = vm.ConfirmarPassword = "1234567";

        Assert.True(TieneMensaje(Validar(vm), "La contraseña tiene que tener al menos 8 caracteres."));
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("ana@")]
    [InlineData("@mail.com")]
    public void Registro_EmailMalFormado_EsInvalido(string email)
    {
        var vm = RegistroValido();
        vm.Email = email;

        Assert.True(TieneMensaje(Validar(vm), "El email no tiene un formato válido."));
    }

    [Fact]
    public void Registro_SinNombre_EsInvalido()
    {
        var vm = RegistroValido();
        vm.Nombre = "";

        Assert.True(TieneMensaje(Validar(vm), "Ingresá tu nombre."));
    }

    [Fact]
    public void Registro_NoTieneCampoRol()
    {
        // El rol nunca puede venir del formulario: nadie se registra como Admin desde la web.
        Assert.Null(typeof(RegistroViewModel).GetProperty("Rol"));
    }

    // ---------------------------------------------------------------- LoginViewModel / Usuario

    [Fact]
    public void Login_Valido_NoTieneErrores()
        => Assert.Empty(Validar(new LoginViewModel { Email = "ana@mail.com", Password = "x" }));

    [Fact]
    public void Login_EmailMalFormado_EsInvalido()
        => Assert.True(TieneMensaje(
            Validar(new LoginViewModel { Email = "nada", Password = "x" }),
            "El email no tiene un formato válido."));

    [Fact]
    public void Login_SinContrasenia_EsInvalido()
        => Assert.True(TieneMensaje(
            Validar(new LoginViewModel { Email = "ana@mail.com", Password = "" }),
            "Ingresá tu contraseña."));

    [Fact]
    public void Login_PorDefectoLaSesionNoEsPersistente()
        => Assert.False(new LoginViewModel().Persistente);

    [Fact]
    public void Usuario_PorDefectoEsClienteYEstaActivo()
    {
        var usuario = new Usuario();

        Assert.Equal("Cliente", usuario.Rol);
        Assert.True(usuario.Activo);
    }
}
