namespace PizzeriaBackend.Models;

// View model del formulario de pedido: une en una sola vista
// el menú (desde la DB) + el formulario + la pizza preseleccionada
// que viene del botón "+" del landing.
public class AgregarVM
{
    // Las pizzas que muestra el catálogo: salen de db.Pizzas
    public List<Pizza> Pizzas { get; set; } = new();

    // El formulario que se postea (mismos campos que el POST /pedidos)
    public PedidoDTO Form { get; set; } = new();

    // ?variedad=... del landing: marca la card correspondiente
    // como checked al abrir el formulario.
    public string? VariedadPrevia { get; set; }
}