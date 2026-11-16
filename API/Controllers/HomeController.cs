using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaBackend.Data;

namespace PizzeriaBackend.Controllers;

// LANDING: las pizzas salen de la tabla Pizzas de la DB.
// Ya NO llama a RepositorioPizzas (el hardcode se elimina en la Parte 5).
// Se deja anónimo a propósito: la carta es pública, ver el menú no exige login.
public class HomeController(PizzeriaDb db) : Controller
{
    // GET / — pasa List<Pizza> de la DB a la vista del landing
    public async Task<IActionResult> Index()
        => View(await db.Pizzas.OrderBy(p => p.Id).ToListAsync());
}