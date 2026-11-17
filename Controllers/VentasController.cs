using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PowerGym.Data;
using PowerGym.Models;

namespace PowerGym.Controllers;

public sealed class VentasController : Controller
{
    private readonly VentaDAO _dao;
    private readonly ILogger<VentasController> _logger;

    public VentasController(VentaDAO dao, ILogger<VentasController> logger)
    {
        _dao = dao;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? idVenta = null)
    {
        ViewBag.VentaConfirmada = idVenta.HasValue
            ? await _dao.ObtenerVentaAsync(idVenta.Value) : null;
        return View(await _dao.ListarProductosAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(string? cliente, string? detallesJson)
    {
        List<LineaVentaInput> lineas;
        try
        {
            lineas = JsonSerializer.Deserialize<List<LineaVentaInput>>(
                detallesJson ?? "[]", new JsonSerializerOptions
                { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch (JsonException)
        {
            return await MostrarErrorAsync("No pudimos leer el detalle. Vuelve a agregar los artículos.", cliente, new());
        }

        if (lineas.Count == 0)
            return await MostrarErrorAsync("Agrega al menos un artículo antes de confirmar la venta.", cliente, lineas);
        if (lineas.Any(x => x.IdProducto <= 0 || x.Cantidad <= 0 || x.Cantidad > 1000))
            return await MostrarErrorAsync("Revisa las cantidades: cada artículo debe tener entre 1 y 1000 unidades.", cliente, lineas);

        try
        {
            var venta = await _dao.RegistrarVentaAsync(cliente, lineas);
            TempData["VentaExitosa"] = $"¡Venta #{venta.Id} registrada con éxito por Bs. {venta.Total:N2}!";
            // PRG: evita duplicar la operación al actualizar el navegador.
            return RedirectToAction(nameof(Index), new { idVenta = venta.Id });
        }
        catch (ArgumentException ex)
        {
            return await MostrarErrorAsync(ex.Message, cliente, lineas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el registro transaccional de una venta");
            return await MostrarErrorAsync(
                "No se pudo guardar la venta. No se registró parcialmente; inténtalo de nuevo.",
                cliente, lineas);
        }
    }

    private async Task<IActionResult> MostrarErrorAsync(
        string mensaje, string? cliente, List<LineaVentaInput> lineas)
    {
        ViewBag.ErrorVenta = mensaje;
        ViewBag.ClienteInicial = cliente ?? "Consumidor Final";
        // Solo se serializan identificadores y cantidades validados por el servidor.
        ViewBag.CarritoInicial = JsonSerializer.Serialize(lineas);
        return View("Index", await _dao.ListarProductosAsync());
    }
}
