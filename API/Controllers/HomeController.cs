// API/Controllers/HomeController.cs — el landing lee de la DB, ya no del hardcode
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaBackend.Data;

namespace Torren3.Controllers;

public class HomeController(PizzeriaDb db) : Controller
{
    // GET / — pizzas de la tabla Pizzas, no de RepositorioPizzas
    public async Task<IActionResult> Index()
        => View(await db.Pizzas.OrderBy(p => p.Id).ToListAsync());
}   