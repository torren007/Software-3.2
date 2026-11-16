using Microsoft.AspNetCore.Mvc;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;

namespace PizzeriaBackend.Controllers;

// LOGIN: valida contra la tabla Usuarios (Parte 1/2) y emite la cookie
// "Pizzeria.Auth" por medio de AutenticacionService (Parte 3).
public class LoginController(IAutenticacionService auth) : Controller
{
    // GET /Login — o /Login?returnUrl=... cuando [Authorize] te redirigió
    [HttpGet]
    public IActionResult Index(string? returnUrl)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    // POST /Login — valida credenciales y te devuelve a donde pediste ir
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = await auth.ValidarAsync(vm.Email, vm.Password);
        if (usuario is null)
        {
            // Mensaje genérico a propósito: no revela si falló el email o la pass
            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            return View(vm);
        }

        await auth.EntrarAsync(HttpContext, usuario, vm.Persistente);

        // IsLocalUrl rechaza returnUrls externos (ataque de "open redirect").
        // El "!" es seguro: si IsLocalUrl dio true, ReturnUrl no es null.
        var destino = Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl! : "/";
        return LocalRedirect(destino);
    }

    // POST /Login/Salir — borra la cookie (lo usa el botón "Salir" del header)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await auth.SalirAsync(HttpContext);
        return RedirectToAction(nameof(Index));
    }
}