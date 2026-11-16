using System.ComponentModel.DataAnnotations;

namespace PizzeriaBackend.Models;

public class PedidoDTO
{
    // Antes los campos venían sin validar: cualquier POST vacío
    // creaba un cliente con nombre "" y dirección "" en la DB.

    [Required(ErrorMessage = "Ingresá tu nombre.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede pasar de 150 caracteres.")]
    public string ClienteNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu dirección de entrega.")]
    [MaxLength(255, ErrorMessage = "La dirección no puede pasar de 255 caracteres.")]
    public string ClienteDireccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí una pizza de la carta.")]
    [MaxLength(100)]
    public string PizzaVariedad { get; set; } = string.Empty;

    // El precio lo manda el formulario, pero el backend igual lo
    // re-valida: no acepta negativos ni cero.
    [Range(0.01, 9999999, ErrorMessage = "El precio no es válido.")]
    public decimal PizzaPrecio { get; set; }

    [Range(1, 50, ErrorMessage = "La cantidad tiene que estar entre 1 y 50.")]
    public int Cantidad { get; set; } = 1;
}