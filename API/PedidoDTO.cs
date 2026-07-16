namespace PizzeriaBackend.Models;

public class PedidoDTO
{
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteDireccion { get; set; } = string.Empty;
    public string PizzaVariedad { get; set; } = string.Empty;
    public decimal PizzaPrecio { get; set; }
    public int Cantidad { get; set; }
}