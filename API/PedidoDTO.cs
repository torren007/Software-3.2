// API/PedidoDTO.cs — REESCRIBE (agrega validación de servidor)
using System.ComponentModel.DataAnnotations;

namespace PizzeriaBackend.Models;

public class PedidoDTO
{
    [Required] public string ClienteNombre { get; set; } = string.Empty;
    [Required] public string ClienteDireccion { get; set; } = string.Empty;
    [Required] public string PizzaVariedad { get; set; } = string.Empty;
    public decimal PizzaPrecio { get; set; }
    [Range(1, 50)] public int Cantidad { get; set; } = 1;
}