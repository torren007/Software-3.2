using Microsoft.AspNetCore.Mvc;
using PizzeriaBackend.Models; // Tu namespace real

namespace Torren3.Controllers
{
    public class PedidoController : Controller
    {
        private readonly PizzeriaDb _db;

        public PedidoController(PizzeriaDb db)
        {
            _db = db;
        }

        // Verbo GET: Preparamos el formulario con los datos de la pizza seleccionada.
        [HttpGet]
        public IActionResult Agregar(string variedad, decimal precio) 
        {
            var pedido = new PedidoDTO 
            { 
                PizzaVariedad = variedad,
                PizzaPrecio = precio,
                Cantidad = 1 
            };
            return View(pedido);
        }

        // Verbo POST: Guardamos el pedido real.
        [HttpPost]
        public IActionResult Agregar(PedidoDTO pedidoNuevo)
        {
            if (ModelState.IsValid)
            {
                // Persistencia utilizando tu PizzeriaDb
                _db.Pedidos.Add(pedidoNuevo);
                _db.SaveChanges();

                return View("Confirmacion", pedidoNuevo);
            }

            return View(pedidoNuevo);
        }
    }
}