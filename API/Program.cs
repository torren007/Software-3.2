using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using PizzeriaBackend.Data;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. MVC (controladores + vistas)
builder.Services.AddControllersWithViews();

// 2. BASE DE DATOS (Pomelo / MySQL) — igual que antes
var connectionString = "Server=localhost;Database=5to_PizzeriaDB;User=5to_agbd;Password=Trigg3rs!;";
builder.Services.AddDbContext<PizzeriaDb>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 3. NUEVO: AUTENTICACIÓN POR COOKIES
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Pizzeria.Auth";
        options.Cookie.HttpOnly = true;     // el JS del navegador no puede leerla (anti-XSS)
        options.LoginPath = "/Login";       // [Authorize] sin sesión → te manda acá con ReturnUrl
        options.AccessDeniedPath = "/Login";// rol insuficiente → también acá
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;   // renueva la sesión mientras seguís activo
    });

// 4. NUEVO: SERVICIOS DE DOMINIO (la lógica que antes vivía inline en el MapPost)
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();

// 5. Herramientas de desarrollo (Swagger + Scalar) — igual que antes
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(o => o.RouteTemplate = "/openapi/{documentName}.json");
    app.MapScalarApiReference();
}

// 6. MIDDLEWARE — el orden importa: StaticFiles → Routing → Auth → Authorization
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();   // NUEVO: lee la cookie antes de autorizar
app.UseAuthorization();

// ---------------------------------------------------------------------
// ENDPOINTS MINIMALES (API para ClienteApp, CocinaApp y el landing)
// ---------------------------------------------------------------------

// Listado de pedidos activos (lo consume el ticket y el seguimiento del landing)
app.MapGet("/pedidos", async (PizzeriaDb db) =>
    await db.Pedidos.Where(p => p.Activo).ToListAsync());

// POST: delega en el servicio. Ya no tiene 60 líneas de EF + sockets adentro.
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

// PUT: la CocinaApp llama acá para pasar el estado a "En preparación"
app.MapPut("/pedidos/{id}/estado", async (int id, string nuevoEstado, PizzeriaDb db) =>
{
    var pedido = await db.Pedidos.FindAsync(id);
    if (pedido is null) return Results.NotFound();

    pedido.Estado = nuevoEstado;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// DELETE: borrado lógico (Activo = 0)
app.MapDelete("/pedidos/{id}", async (int id, PizzeriaDb db) =>
{
    var pedido = await db.Pedidos.FindAsync(id);
    if (pedido is null) return Results.NotFound();

    pedido.Activo = false;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// 7. RUTA MVC POR DEFECTO
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();