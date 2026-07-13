namespace PizzeriaBackend.Models;

public class Pedido
{
    public int Id { get; set; }
    
    // Volvemos a usar "string" plano para emparejar con el JSON del cliente y tu VARCHAR de SQL
    public string? Cliente { get; set; }
    public string? DetallePizza { get; set; }
    
    public string? ActorAsignado { get; set; }
    public string? Estado { get; set; }
    public bool Activo { get; set; }
}