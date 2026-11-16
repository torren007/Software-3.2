namespace Torren3.Models
{
    public static class RepositorioPizzas
    {
        // Simulamos un acceso a datos que luego se conectará a PizzeriaDb
        private static List<Pizza> pizzasDestacadas = new List<Pizza>
        {
            new Pizza { Id = 1, Nombre = "Margarita Clásica", Descripcion = "Salsa de tomate, mozzarella y albahaca fresca.", Precio = 5500m, ImagenUrl = "/img/margarita.jpg" },
            new Pizza { Id = 2, Nombre = "Pepperoni Especial", Descripcion = "Doble pepperoni con un toque de miel picante.", Precio = 6200m, ImagenUrl = "/img/pepperoni.jpg" },
            new Pizza { Id = 3, Nombre = "Cuatro Quesos", Descripcion = "Mozzarella, gorgonzola, parmesano y provolone.", Precio = 6800m, ImagenUrl = "/img/cuatroquesos.jpg" }
        };

        public static IEnumerable<Pizza> ObtenerDestacadas() => pizzasDestacadas;
    }
}