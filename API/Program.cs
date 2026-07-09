using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using PizzeriaBackend.Models;
using PizzeriaBackend.Data;
using System.Net.Sockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// CONFIGURACIÓN DE LA BASE DE DATOS (Abstracción)
var connectionString = "Server=localhost;Database=PizzeriaDB;User=5to_agbd;Password=Trigg3rs!;";

// Inyectamos el DbContext
builder.Services.AddDbContext<PizzeriaDb>(options => 
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Para manejar excepciones en entorno de desarrollo
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => { options.RouteTemplate = "/openapi/{documentName}.json"; });
    app.MapScalarApiReference();
}

// GET: Listar pedidos activos
app.MapGet("/pedidos", async (PizzeriaDb db) => 
    await db.Pedidos.Where(p => p.Activo).ToListAsync()
);

// POST: Crear un nuevo pedido
// POST: Crear un nuevo pedido
app.MapPost("/pedidos", async (Pedido nuevo, PizzeriaDb db, ILogger<Program> logger) => 
{
    try 
    {
        // 1. Asignación inicial de estados
        nuevo.ActorAsignado = "Cocina";
        nuevo.Estado = "Espera de confirmación"; 
        nuevo.Activo = true;
        
        db.Pedidos.Add(nuevo);
        await db.SaveChangesAsync();

        logger.LogInformation($"[API] Pedido #{nuevo.Id} guardado en BD. Notificando a cocina...");

        // 2. Integración de servicios internos mediante Sockets (Actividad 3)
        try
        {
            using var tcpClient = new TcpClient("127.0.0.1", 5000); // Se conecta a la cocina
            using var stream = tcpClient.GetStream();
            var mensaje = Encoding.UTF8.GetBytes($"NUEVO_PEDIDO:{nuevo.Id}");
            
            await stream.WriteAsync(mensaje); // Envía el mensaje por el socket
            logger.LogInformation("[API] Mensaje enviado a la cocina por Socket exitosamente.");
        }
        catch (SocketException ex)
        {
            // Manejo de errores asincrónicos (Actividad 4)
            logger.LogWarning($"[API/FALLO SIMULADO] El pedido se creó, pero la Cocina está desconectada. Error: {ex.Message}");
            nuevo.Estado = "Error de red interno (Cocina no responde)";
            await db.SaveChangesAsync();
        }

        return Results.Created($"/pedidos/{nuevo.Id}", nuevo);
    }
    catch (Exception ex)
    {
        logger.LogError($"[API/ERROR CRÍTICO] Ocurrió un error al guardar: {ex.Message}");
        return Results.Problem("Ocurrió un error interno en el servidor.");
    }
}); 

// PUT: Actualizar estado del pedido
app.MapPut("/pedidos/{id}/estado", async (int id, string nuevoEstado, PizzeriaDb db) => 
{
    var pedido = await db.Pedidos.FindAsync(id);
    if (pedido is null) return Results.NotFound();

    pedido.Estado = nuevoEstado;
    await db.SaveChangesAsync();

    return Results.NoContent();
});

// DELETE: Borrado lógico del pedido
app.MapDelete("/pedidos/{id}", async (int id, PizzeriaDb db) => 
{
    if (await db.Pedidos.FindAsync(id) is Pedido pedido)
    {
        pedido.Activo = false; 
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();