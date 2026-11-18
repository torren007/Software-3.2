using Microsoft.AspNetCore.Mvc;
using PizzeriaBackend.Services;

namespace PizzeriaBackend.Controllers;

// LANDING: las pizzas salen de la tabla Pizzas (vía PedidoService), no de un repositorio hardcodeado.
// Se deja anónimo a propósito: la carta es pública, ver el menú no exige login.
public class HomeController(IPedidoService pedidos) : Controller
{
    // GET / — pasa List<Pizza> de la DB a la vista del landing
    public async Task<IActionResult> Index()
        => View(await pedidos.ObtenerMenuAsync());
}
