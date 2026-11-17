using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PowerGym.Data;
using PowerGym.Models;

namespace PowerGym.Controllers;

public class MembresiasController : Controller
{
    private readonly MembresiaDAO dao;

    public MembresiasController(MembresiaDAO d) => dao = d;

    public async Task<IActionResult> Index()
    {
        await CargarListasAsync();

        ViewBag.NuevaMembresia = new Membresia
        {
            FechaInicio = DateTime.Today
        };

        ViewBag.AbrirModal = false;

        return View(await dao.ListarAsync());
    }

    private async Task CargarListasAsync()
    {
        var socios = await dao.ObtenerSociosAsync();
        var planes = await dao.ObtenerPlanesAsync();

        ViewBag.Socios = socios.Select(s => new SelectListItem
        {
            Value = s.IdSocio.ToString(),
            Text = $"{s.Nombres} {s.Apellidos} - {s.Documento}"
        });

        ViewBag.Planes = planes.Select(p => new SelectListItem
        {
            Value = p.IdPlan.ToString(),
            Text = $"{p.Nombre} - Bs. {p.Precio} ({p.DuracionDias} días)"
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await CargarListasAsync();

        return View(new Membresia
        {
            FechaInicio = DateTime.Today
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Membresia m)
    {
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            ViewBag.NuevaMembresia = m;
            ViewBag.AbrirModal = true;

            return View("Index", await dao.ListarAsync());
        }

        var creado = await dao.CrearAsync(m);

        if (!creado)
        {
            ModelState.AddModelError(
                string.Empty,
                "No pudimos registrar la membresía. Verifica que el socio y el plan estén activos."
            );

            await CargarListasAsync();
            ViewBag.NuevaMembresia = m;
            ViewBag.AbrirModal = true;

            return View("Index", await dao.ListarAsync());
        }

        TempData["Mensaje"] = "Membresía registrada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        await dao.CancelarAsync(id);

        TempData["Mensaje"] = "Membresía cancelada.";

        return RedirectToAction(nameof(Index));
    }
}
