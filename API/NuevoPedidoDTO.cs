using System.ComponentModel.DataAnnotations;

namespace PizzeriaBackend.Models;

// Pedido que arma el formulario web: varias pizzas.
// Ojo: NO lleva precio. El servidor lo toma de la tabla Pizzas.
// (PedidoDTO, con una sola pizza, sigue siendo el de la API para ClienteApp.)
public class NuevoPedidoDTO
{
    [Required(ErrorMessage = "Ingresá tu nombre.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede pasar de 150 caracteres.")]
    public string ClienteNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu dirección de entrega.")]
    [MaxLength(255, ErrorMessage = "La dirección no puede pasar de 255 caracteres.")]
    public string ClienteDireccion { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "Agregá al menos una pizza al pedido.")]
    public List<ItemPedidoDTO> Items { get; set; } = new();
}

// Una línea del pedido: "3 × Pepperoni Especial"
public class ItemPedidoDTO
{
    [Required, MaxLength(100)]
    public string Variedad { get; set; } = string.Empty;

    [Range(1, 50, ErrorMessage = "La cantidad tiene que estar entre 1 y 50.")]
    public int Cantidad { get; set; } = 1;
}
