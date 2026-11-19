# Tests — Pizzería Torren (Software 3.4: "MVC: La aplicación cobra vida")

Proyecto de pruebas **xUnit** que cubre las actividades de la etapa de **Producción** del plan.
**No modifica ni una línea del código de la aplicación**: todo vive en esta carpeta.

## Cómo correrlos

Requisitos: SDK de .NET 10 y acceso a NuGet (la primera vez restaura paquetes).

```bash
dotnet test Tests/API.Tests.csproj
```

> ⚠️ **Cerrá `CocinaApp` antes de correr los tests.** Los tests levantan una "cocina falsa" en el puerto
> 5050 (el mismo que usa la app). Si ya hay algo escuchando ahí, los tests fallan con un mensaje claro
> en vez de mandarle pedidos de prueba a la cocina real.

Para que el proyecto también aparezca en Visual Studio (no se tocó el `.sln` a propósito):

```bash
dotnet sln Software-3.2.sln add Tests/API.Tests.csproj
```

## Cómo se prueba sin tocar el código

| Problema | Solución dentro de los tests |
|---|---|
| `Program` es interno y la cadena de MySQL está escrita en el código | `WebApplicationFactory<HomeController>` levanta la app **real**; solo se reemplaza el `DbContext` de MySQL por uno **en memoria** (`Ayudas/AppFactory.cs`) |
| `PedidoService` abre un socket TCP real a `127.0.0.1:5050` | `Ayudas/CocinaFalsa.cs` escucha en ese puerto: recibe `NUEVO_PEDIDO:{id}` (cocina viva) o se la deja apagada (cocina caída) |
| Las vistas Razor codifican las tildes (`&#xED;`) | `LeerHtmlAsync()` decodifica el HTML antes de comparar mensajes |
| Los formularios exigen token antiforgery + cookie | `EnviarFormularioAsync()` abre la página, extrae el token y hace el POST como un navegador |
| Los tests comparten el puerto 5050 | Se ejecutan de a uno (`DisableTestParallelization`) |

## Mapa consigna → archivos

| Consigna (Producción) | Qué se prueba | Archivo |
|---|---|---|
| **Act. 2** — unitarias del **Modelo** | Reglas de validación de DTO/ViewModels | `Unitarias/ModelosValidacionTests.cs` |
| | Registro, login (hash SHA-256, usuario inactivo) y emisión/borrado de la cookie | `Unitarias/AutenticacionServiceTests.cs` |
| | Lógica de pedidos: precio de la BD, cliente reutilizado, pizza nueva, notificación a cocina | `Unitarias/PedidoServiceTests.cs` |
| **Act. 2** — unitarias del **Controlador** | Qué devuelve cada acción (vista / redirección / 404) con servicios de mentira | `Unitarias/ControllersTests.cs` |
| **Act. 3** — errores y resiliencia | Entradas inválidas (login, registro, pedido), cocina caída y recuperada, token antiforgery ausente, acceso sin sesión, 404, JSON malformado | `Resiliencia/ErroresYResilienciaTests.cs` |
| **Act. 4** — funcionales (≥ 2) | **F1** registro → formulario → pedido → comprobante → BD → cocina | `Funcionales/FlujosWebTests.cs` |
| | **F2** acceso sin sesión → login con `ReturnUrl` → vuelta al formulario con la pizza preseleccionada | `Funcionales/FlujosWebTests.cs` |
| | **F3** API REST: cliente crea pedido → cocina cambia estado → cliente consulta → baja lógica | `Funcionales/FlujoApiTests.cs` |
| | Cookies de sesión (HttpOnly, persistente, logout), open redirect, header según sesión | `Funcionales/FlujosWebTests.cs` |

## Clasificación de las pruebas

- **Unitarias** (`Unitarias/`): una pieza aislada. Modelo con validadores y BD en memoria; controladores con
  servicios falsos (`Ayudas/Fakes.cs`).
- **De integración / resiliencia** (`Resiliencia/`): varias capas juntas (HTTP + Razor + EF + socket) forzando fallos.
- **Funcionales** (`Funcionales/`): recorren un caso de uso completo como lo haría el usuario, de la Vista a la BD y de vuelta.

## Qué no se prueba (y por qué)

- El **JavaScript** de las vistas (armado de la lista de pizzas en el navegador) necesita un navegador real
  (Selenium/Playwright). Los tests funcionales envían al servidor exactamente los campos que ese JS genera
  (`Form.Items[i].Variedad` / `.Cantidad`), así que se verifica el contrato Vista ↔ Backend.
- Los **endpoints mínimos** de `Program.cs` son lambdas (no se pueden instanciar sueltas): se prueban
  por HTTP en `Resiliencia/` y `Funcionales/FlujoApiTests.cs`.

## Hallazgos para el informe técnico

1. **`POST /pedidos` no valida `PedidoDTO`.** El DTO tiene `[Required]`/`[Range]`, pero el endpoint mínimo no las
   aplica; un body vacío crearía un cliente `""` y una pizza `""` de precio 0. Está documentado como test
   **omitido** (`Api_PostConDtoVacio_DeberiaRechazarseConValidacion`): al quitarle el `Skip` debería fallar hasta que se corrija.
2. `GET /pedidos`, `PUT /pedidos/{id}/estado` y `DELETE /pedidos/{id}` no exigen sesión (CocinaApp necesita el PUT sin cookie,
   pero GET y DELETE podrían protegerse).
3. La contraseña de MySQL está escrita en `API/Program.cs` (y quedó en el historial de Git): conviene rotarla y usar `user-secrets`.
