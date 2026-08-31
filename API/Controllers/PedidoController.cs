using Microsoft.AspNetCore.Mvc;
using PizzeriaBackend.Data;
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
                // 1. Buscamos la Pizza en la base de datos para obtener su Id real
                var pizza = _db.Pizzas.FirstOrDefault(p => p.Variedad == pedidoNuevo.PizzaVariedad);
                
                if (pizza == null) 
                {
                    ModelState.AddModelError("", "La pizza seleccionada no existe.");
                    return View(pedidoNuevo);
                }

                // 2. Creamos (o buscamos) el Cliente
                // Nota: Asumo que tu clase Cliente tiene propiedades 'Nombre' y 'Direccion'. 
                var cliente = new Cliente 
                {
                    Nombre = pedidoNuevo.ClienteNombre,
                    Direccion = pedidoNuevo.ClienteDireccion
                };

                // 3. Armamos la entidad Pedido relacional
                var entidadPedido = new Pedido
                {
                    Cliente = cliente,            // Entity Framework insertará el cliente automáticamente
                    Estado = "Pendiente",         // Estado inicial
                    Activo = true,
                    Detalles = new List<DetallePedido>
                    {
                        new DetallePedido 
                        {
                            // Nota: Asumo que DetallePedido tiene 'PizzaId' y 'Cantidad'
                            PizzaId = pizza.Id, 
                            Cantidad = pedidoNuevo.Cantidad
                        }
                    }
                };

                // 4. Guardamos todo el "árbol" de objetos en la base de datos
                _db.Pedidos.Add(entidadPedido);
                _db.SaveChanges();

                return View("Confirmacion", pedidoNuevo);
            }

            return View(pedidoNuevo);
        }
    }
}