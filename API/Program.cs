using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Dapper;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la conexión a MySQL
string connectionString = "Server=localhost;Database=PizzeriaDB;User=5to_agbd;Password=Trigg3rs!;";
builder.Services.AddTransient(sp => new MySqlConnection(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => { options.RouteTemplate = "/openapi/{documentName}.json"; });
    app.MapScalarApiReference();
}

// GET: Listar pedidos activos
app.MapGet("/pedidos", async (MySqlConnection db) => {
    var sql = "SELECT * FROM Pedidos WHERE Activo = 1";
    var pedidos = await db.QueryAsync<Pedido>(sql);
    return Results.Ok(pedidos);
});

// POST: Crear un nuevo pedido
app.MapPost("/pedidos", async (Pedido nuevo, MySqlConnection db) => {
    var sql = @"INSERT INTO Pedidos (Cliente, DetallePizza, ActorAsignado, Estado, Activo) 
                VALUES (@Cliente, @DetallePizza, 'Cocina', 'Pendiente', 1); 
                SELECT LAST_INSERT_ID();";
    
    var id = await db.QuerySingleAsync<int>(sql, nuevo);
    nuevo.Id = id;
    
    // Aquí se emitiría un evento por Socket hacia el módulo de "Cocina"
    return Results.Created($"/pedidos/{id}", nuevo);
});

// DELETE: Borrado lógico del pedido (Cambia Activo de 1 a 0)
app.MapDelete("/pedidos/{id}", async (int id, MySqlConnection db) => {
    var sql = "UPDATE Pedidos SET Activo = 0 WHERE Id = @Id";
    var affectedRows = await db.ExecuteAsync(sql, new { Id = id });
    
    return affectedRows > 0 ? Results.NoContent() : Results.NotFound();
});

app.Run();

// Modelo
public class Pedido
{
    public int Id { get; set; }
    public string? Cliente { get; set; }
    public string? DetallePizza { get; set; }
    public string? ActorAsignado { get; set; }
    public string? Estado { get; set; }
    public int Activo { get; set; }
}