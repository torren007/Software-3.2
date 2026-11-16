// API/pizza.cs — ahora conoce las columnas nuevas
namespace PizzeriaBackend.Models;

public class Pizza
{
    public int Id { get; set; }
    public string Variedad { get; set; } = string.Empty; // VARCHAR(100) de la DB
    public string? Descripcion { get; set; }             // NUEVO
    public decimal Precio { get; set; }
    public string? ImagenUrl { get; set; }               // NUEVO
}