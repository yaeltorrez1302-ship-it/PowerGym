using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PowerGym.Data;
using PowerGym.Services;

namespace PowerGym.Controllers;

public sealed class AccesoController : Controller
{
    private readonly VendedorDAO _usuarios;
    private readonly ILogger<AccesoController> _logger;

    public AccesoController(VendedorDAO usuarios, ILogger<AccesoController> logger)
    {
        _usuarios = usuarios;
        _logger = logger;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Entrar(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/Ventas";
        return View();
    }

    // OP-5: autenticar sin exponer detalles tecnicos ni las contrasenas.
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Entrar(string? usuario, string? clave, string? returnUrl)
    {
        ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/Ventas";
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(clave))
        {
            ViewBag.ErrorAcceso = "Completa el usuario y la contraseña para continuar.";
            return View();
        }

        VendedorSesion? vendedor;
        try
        {
            vendedor = await _usuarios.AutenticarAsync(usuario, clave);
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "OP-5 fallo PostgreSQL al autenticar vendedor");
            ViewBag.ErrorAcceso = MensajesErrorUx.AccesoTemporal;
            return View();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "OP-5 conexion no disponible al autenticar vendedor");
            ViewBag.ErrorAcceso = MensajesErrorUx.AccesoTemporal;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OP-5 error inesperado al autenticar vendedor");
            ViewBag.ErrorAcceso = MensajesErrorUx.Inesperado;
            return View();
        }

        if (vendedor is null)
        {
            ViewBag.ErrorAcceso = "Usuario o contraseña incorrectos. Revisa tus datos e inténtalo nuevamente.";
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
