using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;

namespace PizzeriaBackend.Controllers;

// [Authorize] a nivel de clase: sin cookie "Pizzeria.Auth", cualquiera de las
// acciones te redirige a /Login?ReturnUrl=/Pedido/Agregar... y después
// del login volvés justo acá. Pedir es privado; mirar la carta (Home) no.
// El controlador NO toca EF ni sockets: todo pasa por IPedidoService.
[Authorize]
public class PedidoController(IPedidoService pedidos) : Controller
{
    // GET /Pedido/Agregar — el "+" del landing llega con ?variedad=Calabresa
    // y preselecciona esa card en el formulario.
    [HttpGet]
    public async Task<IActionResult> Agregar(string? variedad)
        => View(await ArmarAsync(new(), variedad));

    // POST /Pedido/Agregar — guarda en la DB y avisa a la cocina (todo en el servicio)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(AgregarVM vm)
    {
        if (!ModelState.IsValid) return View(await ArmarAsync(vm.Form, null));

        var pedido = await pedidos.CrearAsync(vm.Form);
        return RedirectToAction(nameof(Confirmacion), new { id = pedido.Id });
    }

    // GET /Pedido/Confirmacion/5 — comprobante con el pedido real de la DB
    [HttpGet]
    public async Task<IActionResult> Confirmacion(int id)
        => await pedidos.ObtenerAsync(id) is { } pedido ? View(pedido) : NotFound();

    // Arma el view model: menú desde la DB + formulario + preselección
    private async Task<AgregarVM> ArmarAsync(PedidoDTO form, string? variedad) => new()
    {
        Pizzas = await pedidos.ObtenerMenuAsync(),
        Form = form,
        VariedadPrevia = variedad
    };
}
