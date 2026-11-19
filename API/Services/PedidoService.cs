using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PizzeriaBackend.Data;
using PizzeriaBackend.Models;

namespace PizzeriaBackend.Services;

// Lógica de negocio de pedidos. Vive acá y no en el controlador
// ni inline en Program.cs, para que ninguno de los dos pase de un puño.
public interface IPedidoService
{
    // API (ClienteApp): UNA pizza; si la variedad no existe, la crea
    Task<Pedido> CrearAsync(PedidoDTO dto);

    // Formulario web: VARIAS pizzas, todas de la carta. Devuelve null si alguna ya no existe
    Task<Pedido?> CrearDesdeMenuAsync(NuevoPedidoDTO dto);

    Task<Pedido?> ObtenerAsync(int id);

    // Carta completa desde la tabla Pizzas (la usan el landing y el formulario)
    Task<List<Pizza>> ObtenerMenuAsync();
}

public class PedidoService(PizzeriaDb db) : IPedidoService
{
    public async Task<Pedido> CrearAsync(PedidoDTO dto)
    {
        // Las variedades nuevas (ej: "Muzzarella Grande" de ClienteApp) entran con
        // Descripcion e ImagenUrl en NULL: por eso la DB las definió así.
        var pizza = await db.Pizzas.FirstOrDefaultAsync(p => p.Variedad == dto.PizzaVariedad)
                    ?? new Pizza { Variedad = dto.PizzaVariedad, Precio = dto.PizzaPrecio };

        return await GuardarAsync(dto.ClienteNombre, dto.ClienteDireccion,
            new() { new() { Pizza = pizza, Cantidad = dto.Cantidad } });
    }

    public async Task<Pedido?> CrearDesdeMenuAsync(NuevoPedidoDTO dto)
    {
        var menu = await db.Pizzas.ToListAsync();   // tabla chica: se trae entera
        var detalles = new List<DetallePedido>();

        foreach (var item in dto.Items)
        {
            var pizza = menu.FirstOrDefault(p =>
                string.Equals(p.Variedad, item.Variedad, StringComparison.OrdinalIgnoreCase));
            if (pizza is null) return null;          // alguien tocó el formulario o la sacaron de la carta

            detalles.Add(new DetallePedido { Pizza = pizza, Cantidad = item.Cantidad });
        }

        return await GuardarAsync(dto.ClienteNombre, dto.ClienteDireccion, detalles);
    }

    // Parte común: busca o crea el cliente, guarda el pedido y avisa a la cocina
    private async Task<Pedido> GuardarAsync(string nombre, string direccion, List<DetallePedido> detalles)
    {
        // Mismo criterio que el código original: el cliente se busca por nombre
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Nombre == nombre)
                      ?? new Cliente { Nombre = nombre, Direccion = direccion };

        // EF resuelve todas las FK en un solo SaveChanges (una transacción)
        var pedido = new Pedido
        {
            Cliente = cliente,
            ActorAsignado = "Cocina",
            Estado = "Espera de confirmación",
            Activo = true,
            Detalles = detalles
        };

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();

        Console.WriteLine($"[API] Pedido #{pedido.Id} registrado en BD. Notificando a cocina...");
        await NotificarCocinaAsync(pedido);
        return pedido;
    }

    public async Task<Pedido?> ObtenerAsync(int id)
        => await db.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Detalles).ThenInclude(d => d.Pizza)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Pizza>> ObtenerMenuAsync()
        => await db.Pizzas.OrderBy(p => p.Id).ToListAsync();

    // Socket TCP hacia CocinaApp (127.0.0.1:5050). Si falla, el pedido
    // queda asentado con estado de error para que el cliente lo vea.
    private async Task NotificarCocinaAsync(Pedido pedido)
    {
        try
        {
            using var tcp = new TcpClient("127.0.0.1", 5050);
            using var stream = tcp.GetStream();
            await stream.WriteAsync(Encoding.UTF8.GetBytes($"NUEVO_PEDIDO:{pedido.Id}"));
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[ALERTA] Cocina desconectada: {ex.Message}");
            pedido.Estado = "Error de red interno (Cocina no responde)";
            await db.SaveChangesAsync();
        }
    }
}
