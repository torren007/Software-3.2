// API/Controllers/LoginController.cs (NUEVO)
using Microsoft.AspNetCore.Mvc;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;

namespace PizzeriaBackend.Controllers;

public class LoginController(IAutenticacionService auth) : Controller
{
    [HttpGet]
    public IActionResult Index(string? returnUrl)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = await auth.ValidarAsync(vm.Email, vm.Password);
        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            return View(vm);
        }

        await auth.EntrarAsync(HttpContext, usuario, vm.Persistente);
        // IsLocalUrl evita el ataque de "open redirect"
        return LocalRedirect(Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl : "/");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await auth.SalirAsync(HttpContext);
        return RedirectToAction(nameof(Index));
    }
}