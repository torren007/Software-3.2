using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using PizzeriaBackend.Data;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = "Server=localhost;Database=5to_PizzeriaDB;User=5to_agbd;Password=Trigg3rs!;";
builder.Services.AddDbContext<PizzeriaDb>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// NUEVO: login por cookies
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Pizzeria.Auth";
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

// NUEVO: servicios de dominio (controladores de un puño)
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(o => o.RouteTemplate = "/openapi/{documentName}.json");
    app.MapScalarApiReference();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // NUEVO: lee la cookie antes de autorizar
app.UseAuthorization();

app.MapGet("/pedidos", async (PizzeriaDb db) =>
    await db.Pedidos.Where(p => p.Activo).ToListAsync());

// El endpoint de API ahora usa el mismo servicio que el MVC
app.MapPost("/pedidos", async (PedidoDTO dto, IPedidoService pedidos) =>
{
    try
    {
        var pedido = await pedidos.CrearAsync(dto);
        return Results.Created($"/pedidos/{pedido.Id}", pedido);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR FATAL] {ex.Message}");
        return Results.Problem("Error interno del servidor.");
    }
});

app.MapPut("/pedidos/{id}/estado", async (int id, string nuevoEstado, PizzeriaDb db) =>
{
    var pedido = await db.Pedidos.FindAsync(id);
    if (pedido is null) return Results.NotFound();
    pedido.Estado = nuevoEstado;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/pedidos/{id}", async (int id, PizzeriaDb db) =>
{
    var pedido = await db.Pedidos.FindAsync(id);
    if (pedido is null) return Results.NotFound();
    pedido.Activo = false;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();