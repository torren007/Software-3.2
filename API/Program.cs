using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using PizzeriaBackend.Models;
using PizzeriaBackend.Data;
using System.Net.Sockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// CONFIGURACIÓN DE LA BASE DE DATOS (Abstracción)
var connectionString = "Server=localhost;Database=PizzeriaDB;User=5to_agbd;Password=Trigg3rs!;";

// Inyectamos el DbContext usando Pomelo para interactuar de forma abstracta en C#
builder.Services.AddDbContext<PizzeriaDb>(options => 
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Filtro de excepciones en entorno de desarrollo (consigna del PDF)
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => { options.RouteTemplate = "/openapi/{documentName}.json"; });
    app.MapScalarApiReference();
}

// GET: Listar pedidos activos mediante LINQ (sin escribir comandos SQL)
app.MapGet("/pedidos", async (PizzeriaDb db) => 
    await db.Pedidos.Where(p => p.Activo).ToListAsync()
);

// POST: Crear un nuevo pedido e integrar comunicación por Sockets
app.MapPost("/pedidos", async (Pedido nuevo, PizzeriaDb db) => 
{
    try 
    {
        // Lógica de negocio inicial
        nuevo.ActorAsignado = "Cocina";
        nuevo.Estado = "Espera de confirmación"; 
        nuevo.Activo = true;
        
        db.Pedidos.Add(nuevo);
        await db.SaveChangesAsync();

        Console.WriteLine($"[API] Pedido #{nuevo.Id} registrado en BD. Conectando a CocinaApp...");

        // Bloque de Programación Distribuida (Socket TCP al puerto 5050)
        try
        {
            using var tcpClient = new TcpClient("127.0.0.1", 5050);
            using var stream = tcpClient.GetStream();
            var mensaje = Encoding.UTF8.GetBytes($"NUEVO_PEDIDO:{nuevo.Id}");
            
            await stream.WriteAsync(mensaje);
            Console.WriteLine("[API] Socket enviado con éxito.");
        }
        catch (SocketException ex)
        {
            // Tratamiento de fallos en la red (Actividad 4)
            Console.WriteLine($"[ALERTA] Pedido creado, pero la Cocina está desconectada. Detalle: {ex.Message}");
            nuevo.Estado = "Error de conexión (Cocina apagada)";
            await db.SaveChangesAsync();
        }

        return Results.Created($"/pedidos/{nuevo.Id}", nuevo);
    }
    catch (Exception ex)
    {
        // Captura de fallos de base de datos o mapeo interno (Error 500 controlado)
        Console.WriteLine($"[ERROR FATAL EN API] {ex.Message}");
        return Results.Problem("Error interno al procesar la comanda de la pizza.");
    }
});

// PUT: Actualizar estado del pedido (Llamado de forma distribuida por la Cocina)
app.MapPut("/pedidos/{id}/estado", async (int id, string nuevoEstado, PizzeriaDb db) => 
{
    var pedido = await db.Pedidos.FindAsync(id);
    if (pedido is null) return Results.NotFound();

    pedido.Estado = nuevoEstado;
    await db.SaveChangesAsync();

    return Results.NoContent();
});

// DELETE: Borrado lógico interactuando únicamente con el objeto de C#
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