using System.Text.Json.Serialization; // <-- NUEVO: Necesario para usar JsonIgnore

namespace PizzeriaBackend.Models;

public class DetallePedido
{
    public int Id { get; set; }
    
    public int PedidoId { get; set; }
    
    [JsonIgnore]
    public Pedido? Pedido { get; set; }
    
    public int PizzaId { get; set; }
    public Pizza? Pizza { get; set; }
    
    public int Cantidad { get; set; }
}