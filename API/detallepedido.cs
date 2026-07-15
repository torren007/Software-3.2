namespace PizzeriaBackend.Models;

public class DetallePedido
{
    public int Id { get; set; }
    
    // Relación con el Pedido
    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }
    
    // Relación con la Pizza
    public int PizzaId { get; set; }
    public Pizza? Pizza { get; set; }
    
    public int Cantidad { get; set; }
}