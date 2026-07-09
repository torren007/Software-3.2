using System.Net.Sockets;
using System.Text;

Console.WriteLine("=== MÓDULO INTERNO: COCINA ===");

// 1. Iniciar servidor Socket en el puerto 5000
TcpListener server = new TcpListener(System.Net.IPAddress.Parse("127.0.0.1"), 5000);
server.Start();
Console.WriteLine("Cocina en línea. Esperando comandas por Socket...");

while (true)
{
    // Espera a que la API se conecte
    using TcpClient client = await server.AcceptTcpClientAsync();
    using NetworkStream stream = client.GetStream();
    
    byte[] buffer = new byte[1024];
    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
    string mensaje = Encoding.UTF8.GetString(buffer, 0, bytesRead);

    Console.WriteLine($"\n[SOCKET] Señal recibida: {mensaje}");

    if (mensaje.StartsWith("NUEVO_PEDIDO"))
    {
        string id = mensaje.Split(':')[1];
        Console.WriteLine($"[COCINA] Empezando a amasar el pedido #{id}...");
        
        // Simular tiempo de preparación asincrónico (5 segundos)
        await Task.Delay(5000); 

        // 2. La cocina avisa a la API que cambió el estado a "En preparación"
        using HttpClient httpClient = new HttpClient();
        try 
        {
            var response = await httpClient.PutAsync($"http://localhost:5180/pedidos/{id}/estado?nuevoEstado=En%20preparacion", null);
            if(response.IsSuccessStatusCode) 
            {
                Console.WriteLine($"[COCINA] Api notificada: Pedido #{id} actualizado a 'En preparación'.");
            }
        } 
        catch 
        {
            Console.WriteLine("[ERROR] La pizza está lista pero la API se cayó. No se pudo avisar.");
        }
    }
}