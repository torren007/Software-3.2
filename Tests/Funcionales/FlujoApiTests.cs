using API.Tests.Ayudas;
using PizzeriaBackend.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace API.Tests.Funcionales;

/// <summary>
/// ACTIVIDAD 4 — Funcional 3: el circuito de la API REST que usan ClienteApp y CocinaApp.
/// JSON del cliente → API → BD → socket a la cocina → la cocina actualiza el estado por HTTP → el cliente lo consulta.
/// </summary>
public class FlujoApiTests
{
    [Fact]
    public async Task Funcional3_ClienteCreaPedido_CocinaCambiaElEstado_ClienteLoConsultaYLoDaDeBaja()
    {
        using var cocina = new CocinaFalsa();
        using var app = new AppFactory();
        var cliente = app.NuevoCliente();

        // 1) ClienteApp → POST /pedidos (JSON)
        var creado = await cliente.PostAsJsonAsync("/pedidos", new PedidoDTO
        {
            ClienteNombre = "Ana",
            ClienteDireccion = "Calle 1",
            PizzaVariedad = "Muzzarella con pepperoni",
            PizzaPrecio = 8500m,
            Cantidad = 1
        });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var cuerpo = await creado.Content.ReadFromJsonAsync<JsonElement>();
        var id = cuerpo.GetProperty("id").GetInt32();
        Assert.Equal($"/pedidos/{id}", creado.Headers.Location!.OriginalString);
        Assert.Equal("Espera de confirmación", cuerpo.GetProperty("estado").GetString());
        Assert.Equal("Ana", cuerpo.GetProperty("cliente").GetProperty("nombre").GetString());
        Assert.Equal("Muzzarella con pepperoni", cuerpo.GetProperty("detalles")[0].GetProperty("pizza").GetProperty("variedad").GetString());

        // 2) La API le avisó a la cocina por el socket
        Assert.Equal($"NUEVO_PEDIDO:{id}", await cocina.EsperarMensajeAsync());

        // 3) CocinaApp → PUT /pedidos/{id}/estado (sin sesión: es un servicio interno)
        var clienteCocina = app.NuevoCliente();
        var estado = await clienteCocina.PutAsync($"/pedidos/{id}/estado?nuevoEstado={Uri.EscapeDataString("En preparación")}", null);
        Assert.Equal(HttpStatusCode.NoContent, estado.StatusCode);

        // 4) El cliente consulta y ve el estado nuevo
        var lista = await cliente.GetFromJsonAsync<JsonElement>("/pedidos");
        var pedido = lista.EnumerateArray().Single(p => p.GetProperty("id").GetInt32() == id);
        Assert.Equal("En preparación", pedido.GetProperty("estado").GetString());

        // 5) Borrado lógico: deja de aparecer en el listado de activos
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/pedidos/{id}")).StatusCode);
        var despues = await cliente.GetFromJsonAsync<JsonElement>("/pedidos");
        Assert.DoesNotContain(despues.EnumerateArray(), p => p.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task Api_ElListadoSoloMuestraLosPedidosActivos()
    {
        using var cocina = new CocinaFalsa();
        using var app = new AppFactory();
        var http = app.NuevoCliente();
        var dto = new PedidoDTO { ClienteNombre = "Ana", ClienteDireccion = "Calle 1", PizzaVariedad = "Muzzarella", PizzaPrecio = 5500m, Cantidad = 1 };

        var primero = (await (await http.PostAsJsonAsync("/pedidos", dto)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var segundo = (await (await http.PostAsJsonAsync("/pedidos", dto)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await http.DeleteAsync($"/pedidos/{primero}");

        var lista = await http.GetFromJsonAsync<JsonElement>("/pedidos");
        var ids = lista.EnumerateArray().Select(p => p.GetProperty("id").GetInt32()).ToList();
        Assert.Equal(new[] { segundo }, ids);
    }
}
