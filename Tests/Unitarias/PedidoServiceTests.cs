using API.Tests.Ayudas;
using Microsoft.EntityFrameworkCore;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;
using Xunit;

namespace API.Tests.Unitarias;

/// <summary>
/// ACTIVIDAD 2 — Modelo/lógica de negocio de PedidoService, con base en memoria
/// y una cocina falsa escuchando en el puerto 5050.
/// </summary>
public class PedidoServiceTests
{
    private static async Task SembrarCartaAsync(BdEnMemoria bd)
    {
        using var db = bd.Crear();
        db.Pizzas.AddRange(
            new Pizza { Variedad = "Muzzarella", Precio = 5500m },
            new Pizza { Variedad = "Calabresa", Precio = 6800m });
        await db.SaveChangesAsync();
    }

    private static PedidoDTO Dto(string cliente = "Ana", string variedad = "Muzzarella", decimal precio = 5500m, int cantidad = 1) => new()
    {
        ClienteNombre = cliente,
        ClienteDireccion = "Calle 1",
        PizzaVariedad = variedad,
        PizzaPrecio = precio,
        Cantidad = cantidad
    };

    private static NuevoPedidoDTO Formulario(params (string Variedad, int Cantidad)[] items) => new()
    {
        ClienteNombre = "Ana",
        ClienteDireccion = "Calle 1",
        Items = items.Select(i => new ItemPedidoDTO { Variedad = i.Variedad, Cantidad = i.Cantidad }).ToList()
    };

    // ---------------------------------------------------------------- carta

    [Fact]
    public async Task ObtenerMenu_DevuelveLaCartaDeLaBaseOrdenadaPorId()
    {
        var bd = new BdEnMemoria();
        await SembrarCartaAsync(bd);
        using var db = bd.Crear();

        var menu = await new PedidoService(db).ObtenerMenuAsync();

        Assert.Equal(new[] { "Muzzarella", "Calabresa" }, menu.Select(p => p.Variedad));
    }

    [Fact]
    public async Task ObtenerMenu_ConLaCartaVacia_DevuelveListaVacia()
    {
        using var db = new BdEnMemoria().Crear();

        Assert.Empty(await new PedidoService(db).ObtenerMenuAsync());
    }

    // ---------------------------------------------------------------- CrearAsync (API / ClienteApp: una pizza)

    [Fact]
    public async Task Crear_PizzaDeLaCarta_UsaElPrecioDeLaBaseYNoElQueMandaElCliente()
    {
        using var cocina = new CocinaFalsa();
        var bd = new BdEnMemoria();
        await SembrarCartaAsync(bd);
        using var db = bd.Crear();

        var pedido = await new PedidoService(db).CrearAsync(Dto(variedad: "Muzzarella", precio: 1m));

        Assert.Equal(5500m, pedido.Detalles.Single().Pizza!.Precio);
        using var verificacion = bd.Crear();
        Assert.Equal(2, await verificacion.Pizzas.CountAsync());   // no se duplicó la pizza
    }

    [Fact]
    public async Task Crear_PizzaQueNoEstaEnLaCarta_LaAgregaConElPrecioEnviado()
    {
        using var cocina = new CocinaFalsa();
        var bd = new BdEnMemoria();
        await SembrarCartaAsync(bd);
        using var db = bd.Crear();

        await new PedidoService(db).CrearAsync(Dto(variedad: "Muzzarella Grande", precio: 8500m));

        using var verificacion = bd.Crear();
        var nueva = await verificacion.Pizzas.SingleAsync(p => p.Variedad == "Muzzarella Grande");
        Assert.Equal(8500m, nueva.Precio);
    }

    [Fact]
    public async Task Crear_PedidoNuevo_QuedaActivoConElEstadoInicialYAsignadoACocina()
    {
        using var cocina = new CocinaFalsa();
        var bd = new BdEnMemoria();
        await SembrarCartaAsync(bd);
        using var db = bd.Crear();

        var pedido = await new PedidoService(db).CrearAsync(Dto(cantidad: 3));

        Assert.True(pedido.Id > 0);
        Assert.True(pedido.Activo);
        Assert.Equal("Espera de confirmación", pedido.Estado);
        Assert.Equal("Cocina", pedido.ActorAsignado);
        Assert.Equal(3, pedido.Detalles.Single().Cantidad);
    }

    [Fact]
    public async Task Crear_DosPedidosDelMismoCliente_NoDuplicaAlCliente()
    {
        using var cocina = new CocinaFalsa();
        var bd = new BdEnMemoria();
        await SembrarCartaAsync(bd);
        using var db = bd.Crear();
        var servicio = new PedidoService(db);

        await servicio.CrearAsync(Dto(cliente: "Ana"));
        await servicio.CrearAsync(Dto(cliente: "Ana"));

        using var verificacion = bd.Crear();
        Assert.Equal(1, await verificacion.Clientes.CountAsync());
        Assert.Equal(2, await verificacion.Pedidos.CountAsync());
    }

    [Fact]
    public async Task Crear_AvisaALaCocinaPorSocketConElIdDelPedido()
    {
        using var cocina = new CocinaFalsa();
        var bd = new BdEnMemoria();
        await SembrarCartaAsync(bd);
        using var db = bd.Crear();

        var pedido = await new PedidoService(db).CrearAsync(Dto());

        Assert.Equal($"NUEVO_PEDIDO:{pedido.Id}", await cocina.EsperarMensajeAsync());
    }

    // ---------------------------------------------------------------- CrearDesdeMenuAsync (formulario web: varias pizzas)

    [Fact]
    public async Task CrearDesdeMenu_VariasPizzas_GuardaUnDetallePorCadaItem()
    {
        using var cocina = new CocinaFalsa();
        var bd = new BdEnMemoria();
        await SembrarCartaAsync(bd);
        using var db = bd.Crear();

        var pedido = await new PedidoService(db).CrearDesdeMenuAsync(Formulario(("Muzzarella", 2), ("Calabresa", 1)));

        Assert.NotNull(pedido);
        Assert.Equal(2, pedido!.Detalles.Count);
        Assert.Equal(2, pedido.Detalles.Single(d => d.Pizza!.Variedad == "Muzzarella").Cantidad);
        Assert.Equal(1, pedido.Detalles.Single(d => d.Pizza!.Variedad == "Calabresa").Cantidad);
    }

    [Fact]
    public async Task CrearDesdeMenu_LaVariedadSeComparaSinDistinguirMayusculas()
    {
        using var cocina = new CocinaFalsa();
        var bd = new BdEnMemoria();
        await SembrarCartaAsync(bd);
        using var db = bd.Crear();

        var pedido = await new PedidoService(db).CrearDesdeMenuAsync(Formulario(("calabresa", 1)));

        Assert.NotNull(pedido);
        Assert.Equal("Calabresa", pedido!.Detalles.Single().Pizza!.Variedad);
    }

    [Fact]
    public async Task CrearDesdeMenu_ConUnaPizzaQueYaNoEstaEnLaCarta_DevuelveNullYNoGuardaNada()
    {
        using var cocina = new CocinaFalsa();
        var bd = new BdEnMemoria();
        await SembrarCartaAsync(bd);
        using var db = bd.Crear();

        var pedido = await new PedidoService(db).CrearDesdeMenuAsync(Formulario(("Muzzarella", 1), ("Inexistente", 1)));

        Assert.Null(pedido);
        using var verificacion = bd.Crear();
        Assert.Equal(0, await verificacion.Pedidos.CountAsync());
        Assert.Equal(0, await verificacion.Clientes.CountAsync());
        Assert.Null(await cocina.EsperarMensajeAsync(TimeSpan.FromMilliseconds(300)));   // tampoco se avisó a cocina
    }

    // ---------------------------------------------------------------- ObtenerAsync

    [Fact]
    public async Task Obtener_TraeElPedidoConSuClienteYSusPizzas()
    {
        using var cocina = new CocinaFalsa();
        var bd = new BdEnMemoria();
        await SembrarCartaAsync(bd);
        int id;
        using (var db = bd.Crear())
            id = (await new PedidoService(db).CrearAsync(Dto(cliente: "Ana"))).Id;

        using var otroContexto = bd.Crear();   // contexto nuevo: nada viene "de caché"
        var pedido = await new PedidoService(otroContexto).ObtenerAsync(id);

        Assert.NotNull(pedido);
        Assert.Equal("Ana", pedido!.Cliente!.Nombre);
        Assert.Equal("Muzzarella", pedido.Detalles.Single().Pizza!.Variedad);
    }

    [Fact]
    public async Task Obtener_PedidoInexistente_DevuelveNull()
    {
        using var db = new BdEnMemoria().Crear();

        Assert.Null(await new PedidoService(db).ObtenerAsync(999));
    }
}
