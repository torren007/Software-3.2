using Microsoft.EntityFrameworkCore;
using PizzeriaBackend.Data;

namespace API.Tests.Ayudas;

/// <summary>
/// Base de datos en memoria para las pruebas unitarias de los servicios.
/// Cada instancia es una BD distinta; Crear() devuelve contextos nuevos sobre la MISMA BD
/// (sirve para verificar que algo realmente quedó guardado, leyéndolo con otro contexto).
/// </summary>
public sealed class BdEnMemoria
{
    private readonly string _nombre = "unit-" + Guid.NewGuid();

    public PizzeriaDb Crear()
        => new(new DbContextOptionsBuilder<PizzeriaDb>().UseInMemoryDatabase(_nombre).Options);
}
