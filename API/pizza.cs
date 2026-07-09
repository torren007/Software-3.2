namespace PizzeriaBackend.Models;

public class Pizza
{
    public int Id { get; set; }
    public string Variedad { get; set; } = string.Empty;
    public decimal Precio { get; set; }
}