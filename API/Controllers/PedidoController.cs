// API/Controllers/PedidoController.cs — REESCRIBE
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaBackend.Data;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;

namespace PizzeriaBackend.Controllers;

[Authorize] // "Login otra vez": sin cookie, te manda a /Login con ReturnUrl
public class PedidoController(PizzeriaDb db, IPedidoService pedidos) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Agregar(string? variedad)
        => View(await ArmarVmAsync(new(), variedad));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(AgregarVM vm)
    {
        if (!ModelState.IsValid) return View(await ArmarVmAsync(vm.Form, null));

        var pedido = await pedidos.CrearAsync(vm.Form);
        return RedirectToAction(nameof(Confirmacion), new { id = pedido.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmacion(int id)
        => await pedidos.ObtenerAsync(id) is { } pedido ? View(pedido) : NotFound();

    private async Task<AgregarVM> ArmarVmAsync(PedidoDTO form, string? variedad)
        => new()
        {
            Pizzas = await db.Pizzas.OrderBy(p => p.Id).ToListAsync(),
            Form = form,
            VariedadPrevia = variedad
        };
}