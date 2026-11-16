// API/Services/PedidoService.cs (NUEVO)
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PizzeriaBackend.Data;
using PizzeriaBackend.Models;

namespace PizzeriaBackend.Services;

public interface IPedidoService
{
    Task<Pedido> CrearAsync(PedidoDTO dto);
    Task<Pedido?> ObtenerAsync(int id);
}

public class PedidoService(PizzeriaDb db) : IPedidoService
{
    public async Task<Pedido> CrearAsync(PedidoDTO dto)
    {
        // Busca o crea el cliente
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Nombre == dto.ClienteNombre)
                      ?? new Cliente { Nombre = dto.ClienteNombre, Direccion = dto.ClienteDireccion };

        // Busca o crea la pizza en el menú (ahora vienen de la DB, pero el
        // POST /pedidos de ClienteApp sigue permitiendo crear variedades nuevas)
        var pizza = await db.Pizzas.FirstOrDefaultAsync(p => p.Variedad == dto.PizzaVariedad)
                    ?? new Pizza { Variedad = dto.PizzaVariedad, Precio = dto.PizzaPrecio };

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

    // Socket TCP hacia CocinaApp (puerto 5050). Si falla, queda asentado en el pedido.
    private async Task NotificarCocinaAsync(Pedido pedido)
    {
        try
        {
            using var tcp = new TcpClient("127.0.0.1", 5050);
            await tcp.GetStream().WriteAsync(Encoding.UTF8.GetBytes($"NUEVO_PEDIDO:{pedido.Id}"));
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[ALERTA] Cocina desconectada: {ex.Message}");
            pedido.Estado = "Error de red interno (Cocina no responde)";
            await db.SaveChangesAsync();
        }
    }
}