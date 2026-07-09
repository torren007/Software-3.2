using Microsoft.EntityFrameworkCore;
using PizzeriaBackend.Models;

namespace PizzeriaBackend.Data;

public class PizzeriaDb : DbContext
{
    public PizzeriaDb(DbContextOptions<PizzeriaDb> options)
        : base(options) { }

    // Representación abstracta de la tabla como una colección de objetos
    public DbSet<Pedido> Pedidos => Set<Pedido>();
}