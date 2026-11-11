using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PowerGym.Data;
using PowerGym.Models;
namespace PowerGym.Controllers;
public class SociosController : Controller
{
    private readonly SocioDAO dao;
    public SociosController(SocioDAO d)=>dao=d;

    public async Task<IActionResult> Index(string? buscar)
    {
        ViewBag.Buscar=buscar??"";
        return View(await dao.Listar(buscar));
    }

    public IActionResult Create()=>View(new Socio());

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Socio socio)
    {
        if(!ModelState.IsValid)return View(socio);
        try {
            await dao.Crear(socio);
            TempData["Mensaje"]="Socio registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch(PostgresException ex) when(ex.SqlState==PostgresErrorCodes.UniqueViolation) {
            ModelState.AddModelError("Documento","El documento ya está registrado.");
            return View(socio);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var s=await dao.Obtener(id);
        return s==null?NotFound():View(s);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Socio socio)
    {
        if(!ModelState.IsValid)return View(socio);
        try {
            if(!await dao.Editar(socio))return NotFound();
            TempData["Mensaje"]="Socio actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch(PostgresException ex) when(ex.SqlState==PostgresErrorCodes.UniqueViolation) {
            ModelState.AddModelError("Documento","El documento ya está registrado.");
            return View(socio);
        }
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        await dao.Desactivar(id);
        TempData["Mensaje"]="Socio desactivado correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
