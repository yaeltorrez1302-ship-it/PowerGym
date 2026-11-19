using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerGym.Data;

namespace PowerGym.Controllers;

public sealed class AccesoController : Controller
{
    private readonly VendedorDAO _usuarios;
    public AccesoController(VendedorDAO usuarios) => _usuarios = usuarios;

    [HttpGet, AllowAnonymous]
    public IActionResult Entrar(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/Ventas";
        return View();
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Entrar(string? usuario, string? clave, string? returnUrl)
    {
        ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/Ventas";
        var vendedor = await _usuarios.AutenticarAsync(usuario, clave);
        if (vendedor is null)
        {
            ViewBag.ErrorAcceso = "Usuario o contraseña incorrectos.";
            return View();
        }
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, vendedor.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, vendedor.Nombre),
            new Claim(ClaimTypes.Role, vendedor.Rol)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
        var destino = (string)ViewBag.ReturnUrl;
        if (vendedor.Rol == "AUDITOR" && destino.Equals("/Ventas", StringComparison.OrdinalIgnoreCase))
            destino = "/Ventas/Auditoria";
        return LocalRedirect(destino);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Entrar));
    }

    [HttpGet]
    public IActionResult Denegado() => Content("No tienes permiso para consultar esta sección.");
}
