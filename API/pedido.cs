namespace PizzeriaBackend.Models;

public class Pedido
{
    public int Id { get; set; }
    
    // Relaciones Orientadas a Objetos
    public Cliente? Cliente { get; set; }
    public Pizza? Pizza { get; set; }
    
    public string? ActorAsignado { get; set; }
    public string? Estado { get; set; }
    public bool Activo { get; set; }
}