using Microsoft.AspNetCore.Mvc;
using PizzeriaBackend.Models;
using PizzeriaBackend.Services;

namespace PizzeriaBackend.Controllers;

// REGISTRO: crea una cuenta de cliente y deja la sesión iniciada.
// La lógica (hash, email repetido, rol) vive en AutenticacionService.
public class RegistroController(IAutenticacionService auth) : Controller
{
    // GET /Registro — o /Registro?returnUrl=... si venías del login
    [HttpGet]
    public IActionResult Index(string? returnUrl)
        => View(new RegistroViewModel { ReturnUrl = returnUrl });

    // POST /Registro — crea la cuenta, inicia sesión y te manda a donde ibas
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RegistroViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = await auth.RegistrarAsync(vm.Nombre, vm.Email, vm.Password);
        if (usuario is null)
        {
            ModelState.AddModelError(nameof(vm.Email), "Ya existe una cuenta con ese email.");
            return View(vm);
        }

        await auth.EntrarAsync(HttpContext, usuario, persistente: false);
        return LocalRedirect(Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl! : "/");
    }
}
