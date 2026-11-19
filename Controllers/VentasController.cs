using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerGym.Data;
using PowerGym.Models;
using PowerGym.Services;

namespace PowerGym.Controllers;

[Authorize]
public sealed class VentasController : Controller
{
    private readonly VentaDAO _dao;
    private readonly ILogger<VentasController> _logger;
    public VentasController(VentaDAO dao, ILogger<VentasController> logger)
    {
        _dao = dao;
        _logger = logger;
    }

    [HttpGet, Authorize(Roles = "VENDEDOR")]
    public async Task<IActionResult> Index(int? idVenta = null)
    {
        ViewBag.VentaConfirmada = idVenta.HasValue
            ? await _dao.ObtenerVentaAsync(idVenta.Value) : null;
        return View(await _dao.ListarProductosAsync());
    }

    [HttpPost, Authorize(Roles = "VENDEDOR"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(
        string? cliente, string? detallesJson, string? tipoDescuento, string? valorDescuento)
    {
        List<LineaVentaInput> lineas;
        try
        {
            if ((detallesJson?.Length ?? 0) > 64000)
                throw new JsonException();
            lineas = JsonSerializer.Deserialize<List<LineaVentaInput>>(
                detallesJson ?? "[]", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch (JsonException)
        {
            return await MostrarErrorAsync("No pudimos leer el detalle. Vuelve a agregar los artículos.", cliente,
                new(), tipoDescuento, valorDescuento);
        }

        if (lineas.Count == 0)
            return await MostrarErrorAsync("Agrega al menos un artículo antes de confirmar la venta.", cliente,
                lineas, tipoDescuento, valorDescuento);
        if (lineas.Count > 100 || lineas.Any(x => x.IdProducto <= 0 || x.Cantidad <= 0 || x.Cantidad > 1000))
            return await MostrarErrorAsync("Revisa las cantidades: cada artículo debe tener entre 1 y 1000 unidades.",
                cliente, lineas, tipoDescuento, valorDescuento);

        if (!decimal.TryParse(valorDescuento ?? "0", NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var valor) &&
            !decimal.TryParse(valorDescuento ?? "0", NumberStyles.AllowDecimalPoint,
                CultureInfo.GetCultureInfo("es-BO"), out valor))
            return await MostrarErrorAsync("El descuento debe ser un número válido.", cliente,
                lineas, tipoDescuento, valorDescuento);

        try
        {
            var idVendedor = VendedorActual.ObtenerId(User);
            var venta = await _dao.RegistrarVentaAsync(cliente, lineas, tipoDescuento, valor, idVendedor);
            TempData["VentaExitosa"] = $"¡Venta #{venta.Id} registrada con éxito por Bs. {venta.Total:N2}!";
            return RedirectToAction(nameof(Index), new { idVenta = venta.Id });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Descuento o venta rechazados para usuario {Vendedor}: {Motivo}",
                User.Identity?.Name, ex.Message);
            return await MostrarErrorAsync(ex.Message, cliente, lineas, tipoDescuento, valorDescuento);
        }
        catch (UnauthorizedAccessException)
        {
            return Challenge();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el registro transaccional de una venta");
            return await MostrarErrorAsync(
                "No se pudo guardar la venta. No se registró parcialmente; inténtalo de nuevo.",
                cliente, lineas, tipoDescuento, valorDescuento);
        }
    }

    [HttpGet, Authorize(Roles = "AUDITOR")]
    public async Task<IActionResult> Auditoria() => View(await _dao.ListarAuditoriaAsync());

    private async Task<IActionResult> MostrarErrorAsync(
        string mensaje, string? cliente, List<LineaVentaInput> lineas,
        string? tipoDescuento, string? valorDescuento)
    {
        ViewBag.ErrorVenta = mensaje;
        ViewBag.ClienteInicial = cliente ?? "Consumidor Final";
        ViewBag.CarritoInicial = JsonSerializer.Serialize(lineas);
        ViewBag.TipoDescuentoInicial = tipoDescuento;
        ViewBag.ValorDescuentoInicial = valorDescuento;
        return View("Index", await _dao.ListarProductosAsync());
    }
}
