using System.Text;
using System.Text.Json;

Console.WriteLine("=== APP CLIENTE (¡Tengo Hambre!) ===");
Console.WriteLine("Presiona ENTER para pedir una Muzzarella...");
Console.ReadLine();

using HttpClient client = new HttpClient();

// Armamos el pedido usando los modelos relacionales
var nuevoPedido = new {
    Cliente = new { Nombre = "Estudiante ET12", Direccion = "Av. Siempre Viva 123" },
    Pizza = new { Variedad = "Muzzarella", Precio = 8500.00 }
};

string json = JsonSerializer.Serialize(nuevoPedido);
var content = new StringContent(json, Encoding.UTF8, "application/json");

Console.WriteLine("Enviando pedido a la API por red...");

try 
{
    var response = await client.PostAsync("http://localhost:5180/pedidos", content);
    
    if (response.IsSuccessStatusCode) 
    {
        string responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("\n¡PEDIDO RECIBIDO POR LA PIZZERÍA!");
        Console.WriteLine($"Ticket devuelto por el backend:\n{responseBody}");
    } 
    else 
    {
        Console.WriteLine($"El servidor rechazó el pedido (Código {response.StatusCode}).");
    }
} 
catch (Exception ex) 
{
    // Manejo de excepción de red de lado del cliente
    Console.WriteLine($"Error crítico de red: El backend está apagado o no responde. ({ex.Message})");
}

Console.ReadLine();