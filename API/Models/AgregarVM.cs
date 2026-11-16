// API/AgregarVM.cs (NUEVO)
namespace PizzeriaBackend.Models;

public class AgregarVM
{
    public List<Pizza> Pizzas { get; set; } = new();
    public PedidoDTO Form { get; set; } = new();
    public string? VariedadPrevia { get; set; } // para preseleccionar desde el botón del landing
}