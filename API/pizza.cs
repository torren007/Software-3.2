namespace PizzeriaBackend.Models;

public class Pizza
{
    public int Id { get; set; }

    // VARCHAR(100) de la tabla Pizzas. Es la columna real de la DB,
    // por eso conserva el nombre "Variedad" y no "Nombre".
    public string Variedad { get; set; } = string.Empty;

    // NUEVO: VARCHAR(255) NULL. El "?" coincide con el NULL de la DB.
    public string? Descripcion { get; set; }

    // DECIMAL(10, 2)
    public decimal Precio { get; set; }

    // NUEVO: VARCHAR(255) NULL
    public string? ImagenUrl { get; set; }
}