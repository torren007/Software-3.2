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
    Task<Pedido> CrearAsync(PedidoDTO dto);
    Task<Pedido?> ObtenerAsync(int id);

    // Carta completa desde la tabla Pizzas (la usan el landing y el formulario)
    Task<List<Pizza>> ObtenerMenuAsync();
}

public class PedidoService(PizzeriaDb db) : IPedidoService
{
    public async Task<Pedido> CrearAsync(PedidoDTO dto)
    {
        // 1. Busca o crea el cliente (mismo criterio que el código original: por nombre)
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Nombre == dto.ClienteNombre)
                      ?? new Cliente { Nombre = dto.ClienteNombre, Direccion = dto.ClienteDireccion };

        // 2. Busca o crea la pizza en el menú. Las variedades nuevas
        //    (ej: "Muzzarella Grande" de ClienteApp) entran con
        //    Descripcion e ImagenUrl en NULL: por eso la DB las definió así.
        var pizza = await db.Pizzas.FirstOrDefaultAsync(p => p.Variedad == dto.PizzaVariedad)
                    ?? new Pizza { Variedad = dto.PizzaVariedad, Precio = dto.PizzaPrecio };

        // 3. Arma el pedido relacional: EF resuelve las FK en un solo SaveChanges
        var pedido = new Pedido
        {
            Cliente = cliente,
            ActorAsignado = "Cocina",
            Estado = "Espera de confirmación",
            Activo = true,
            Detalles = new List<DetallePedido>
            {
                new() { Pizza = pizza, Cantidad = dto.Cantidad }
            }
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