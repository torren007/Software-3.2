using Microsoft.AspNetCore.Mvc;
using Torren3.Models;

namespace Torren3.Controllers
{
    public class HomeController : Controller
    {
        // El verbo GET está implícito por defecto
        public IActionResult Index()
        {
            // Solicitamos los datos al Modelo
            var pizzas = RepositorioPizzas.ObtenerDestacadas();
            
            // Retornamos la vista pasándole la colección de pizzas
            return View(pizzas);
        }
    }
}