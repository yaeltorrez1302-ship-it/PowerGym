using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
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

    // OP-1: consulta de productos y recuperacion de la venta previa.
    [HttpGet, Authorize(Roles = "VENDEDOR")]
    public async Task<IActionResult> Index(int? idVenta = null)
    {
        try
        {
            ViewBag.VentaConfirmada = idVenta.GetValueOrDefault() > 0
                ? await _dao.ObtenerVentaAsync(idVenta!.Value) : null;
            return View(await _dao.ListarProductosAsync());
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "OP-1 error PostgreSQL al cargar ventas; SQLSTATE {Estado}", ex.SqlState);
            ViewBag.ErrorVenta = MensajesErrorUx.Restriccion(ex.SqlState);
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "OP-1 error de conexion al cargar ventas");
            ViewBag.ErrorVenta = MensajesErrorUx.Conexion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OP-1 error inesperado al cargar ventas");
            ViewBag.ErrorVenta = MensajesErrorUx.Inesperado;
        }
        ViewBag.ErrorCatalogo = true;
        return View(Array.Empty<ProductoVenta>());
    }

    // OP-2: registrar venta maestro-detalle; OP-3: validar descuento en backend.
    [HttpPost, Authorize(Roles = "VENDEDOR"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(
        string? cliente, string? detallesJson, string? tipoDescuento, string? valorDescuento)
    {
        List<LineaVentaInput> lineas;
        try
        {
            if ((detallesJson?.Length ?? 0) > 64000)
                throw new JsonException("Detalle demasiado grande");
            lineas = JsonSerializer.Deserialize<List<LineaVentaInput>>(
                detallesJson ?? "[]",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OP-2 detalle de venta con JSON invalido");
            return await MostrarErrorAsync(
                "No pudimos leer el detalle. Vuelve a agregar los artículos.", cliente,
                new(), tipoDescuento, valorDescuento);
        }

        if (lineas.Count == 0)
            return await MostrarErrorAsync(
                "Agrega al menos un artículo antes de confirmar la venta.", cliente,
                lineas, tipoDescuento, valorDescuento);

        if (lineas.Count > 100 || lineas.Any(x => x is null || x.IdProducto <= 0 || x.Cantidad <= 0 || x.Cantidad > 1000))
            return await MostrarErrorAsync(
                "Revisa las cantidades: cada artículo debe tener entre 1 y 1000 unidades.",
                cliente, lineas, tipoDescuento, valorDescuento);

        if (!decimal.TryParse(valorDescuento ?? "0", NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var valor) &&
            !decimal.TryParse(valorDescuento ?? "0", NumberStyles.AllowDecimalPoint,
                CultureInfo.GetCultureInfo("es-BO"), out valor))
            return await MostrarErrorAsync(
                "El descuento debe ser un número válido.", cliente,
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
            _logger.LogWarning("OP-2/3 validacion rechazada para {Vendedor}: {Motivo}",
                User.Identity?.Name, ex.Message);
            return await MostrarErrorAsync(ex.Message, cliente, lineas, tipoDescuento, valorDescuento);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "OP-2 identidad de vendedor no autorizada");
            return Challenge();
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "OP-2 error PostgreSQL al registrar venta; SQLSTATE {Estado}", ex.SqlState);
            return await MostrarErrorAsync(MensajesErrorUx.Restriccion(ex.SqlState),
                cliente, lineas, tipoDescuento, valorDescuento);
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "OP-2 error de conexion al registrar venta");
            return await MostrarErrorAsync(MensajesErrorUx.Conexion,
                cliente, lineas, tipoDescuento, valorDescuento);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OP-2 error inesperado al registrar venta");
            return await MostrarErrorAsync(MensajesErrorUx.Inesperado,
                cliente, lineas, tipoDescuento, valorDescuento);
        }
    }

    // OP-4: consulta de auditoria sin exponer errores SQL al usuario.
    [HttpGet, Authorize(Roles = "AUDITOR")]
    public async Task<IActionResult> Auditoria()
    {
        try
        {
            return View(await _dao.ListarAuditoriaAsync());
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "OP-4 error PostgreSQL al consultar auditoria; SQLSTATE {Estado}", ex.SqlState);
            ViewBag.ErrorAuditoria = MensajesErrorUx.Restriccion(ex.SqlState);
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "OP-4 error de conexion al consultar auditoria");
            ViewBag.ErrorAuditoria = MensajesErrorUx.Conexion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OP-4 error inesperado al consultar auditoria");
            ViewBag.ErrorAuditoria = MensajesErrorUx.Inesperado;
        }
        return View(Array.Empty<AuditoriaDescuentoFila>());
    }

    private async Task<IActionResult> MostrarErrorAsync(
        string mensaje, string? cliente, List<LineaVentaInput> lineas,
        string? tipoDescuento, string? valorDescuento)
    {
        ViewBag.ErrorVenta = mensaje;
        ViewBag.ClienteInicial = cliente ?? "Consumidor Final";
        ViewBag.CarritoInicial = JsonSerializer.Serialize(lineas);
        ViewBag.TipoDescuentoInicial = tipoDescuento;
        ViewBag.ValorDescuentoInicial = valorDescuento;
        // Si falla la base de datos mientras intentamos mostrar un error,
        // no se debe producir una segunda excepcion sin manejar.
        try
        {
            return View("Index", await _dao.ListarProductosAsync());
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "OP-1 error PostgreSQL al recargar catalogo tras validacion");
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "OP-1 conexion no disponible al recargar catalogo");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OP-1 error inesperado al recargar catalogo");
        }
        ViewBag.ErrorVenta = MensajesErrorUx.Conexion;
        ViewBag.ErrorCatalogo = true;
        return View("Index", Array.Empty<ProductoVenta>());
    }
}
