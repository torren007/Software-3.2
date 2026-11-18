# Software-3.2

1. Normalizar la BD
    1. Tabla Pizza
    1. Tabla Cliente
    1. Tabla DetallePedido
1. Agregar DER y Clases

Para correrlo:

dotnet add package MySql.Data
dotnet add package Dapper
dotnet add package Scalar.AspNetCore

http://localhost:5180/scalar/v1

http://localhost:5180/pedidos

## Cómo levantar todo

1. Ejecutar `pizzeria.sql` en MySQL (**borra y recrea** la base `5to_PizzeriaDB`).
2. `dotnet run --project API` → http://localhost:5180
3. (Opcional) `dotnet run --project CocinaApp` para que la cocina pase los pedidos a "En preparación".
4. Usuario de prueba: `admin@pizzeria.com` / `admin1234`

Flujo: landing (`/`) → botón **+** → `/Pedido/Agregar?variedad=...` → si no hay sesión, `/Login?ReturnUrl=...` → vuelve al formulario → `/Pedido/Confirmacion/{id}`.

## Estructura del front

| Archivo | Para qué |
|---|---|
| `Views/Shared/_Layout.cshtml` | Layout único. Carga `base.css` + `header.css` y renderiza `@section Styles` / `Scripts` |
| `Views/Shared/_Header.cshtml` | Header con "Hacer pedido", "Iniciar sesión" / "Hola, X" + "Salir" |
| `wwwroot/css/base.css` | Tokens de color, tipografía, utilidades |
| `wwwroot/css/header.css` | Estilos del header |
| `wwwroot/css/pizza-cards.css` | Cards del formulario + carta del landing |
| `wwwroot/css/Home-Index.css`, `Pedido-*.css`, `Login-Index.css` | Solo lo propio de cada vista |
