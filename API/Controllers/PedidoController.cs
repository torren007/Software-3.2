using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaBackend.Data;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;

namespace PizzeriaBackend.Controllers;

// [Authorize] a nivel de clase: sin cookie "Pizzeria.Auth", cualquiera de las
// tres acciones te redirige a /Login?ReturnUrl=/Pedido/Agregar... y después
// del login volvés justo acá. Pedir es privado; mirar la carta (Home) no.
[Authorize]
public class PedidoController(PizzeriaDb db, IPedidoService pedidos) : Controller
{
    // GET /Pedido/Agregar — el botón "+" del landing llega con ?variedad=Calabresa
    // y preselecciona esa card en el formulario (Parte 5).
    [HttpGet]
    public async Task<IActionResult> Agregar(string? variedad)
        => View(await ArmarAsync(new(), variedad));

    // POST /Pedido/Agregar — delega en el servicio (Parte 3): guarda en la DB
    // y dispara el socket a CocinaApp. Sin lógica de EF ni TCP adentro.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(AgregarVM vm)
    {
        if (!ModelState.IsValid) return View(await ArmarAsync(vm, null));

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
        Pizzas = await db.Pizzas.OrderBy(p => p.Id).ToListAsync(),
        Form = form,
        VariedadPrevia = variedad
    };
}