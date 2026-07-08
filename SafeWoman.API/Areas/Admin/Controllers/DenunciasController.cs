using SafeWoman.Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeWoman.Application.Services;
using SafeWoman.Domain.Enums;

namespace SafeWoman.API.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookies", Roles = "Administrador")]
public class DenunciasController : Controller
{
    private readonly IAdminService _adminService;
    public DenunciasController(IAdminService adminService) => _adminService = adminService;

    public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var formales  = await _adminService.ListarDenunciasAsync(page, pageSize, ct);
        var anonimas  = await _adminService.ListarDenunciasAnonimasAsync(page, pageSize, ct);
        ViewBag.Anonimas = anonimas;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View(formales);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string estado, bool anonima = false, CancellationToken ct = default)
    {
        if (!Enum.TryParse<EstadoDenuncia>(estado, ignoreCase: true, out var nuevoEst))
        {
            TempData["Error"] = $"Estado '{estado}' no es válido. Use: Pendiente, EnProceso, Atendida, Archivada.";
            return RedirectToAction("Index");
        }

        var idAdmin = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (anonima)
            await _adminService.CambiarEstadoDenunciaAnonimaAsync(id, nuevoEst, idAdmin, ct);
        else
            await _adminService.CambiarEstadoDenunciaAsync(id, nuevoEst, idAdmin, ct);

        var etiqueta = nuevoEst switch
        {
            EstadoDenuncia.Pendiente  => "Pendiente",
            EstadoDenuncia.EnProceso  => "En proceso",
            EstadoDenuncia.Atendida   => "Atendida",
            EstadoDenuncia.Archivada  => "Archivada",
            _                         => nuevoEst.ToString()
        };
        TempData["Mensaje"] = $"Estado actualizado a «{etiqueta}».";
        return RedirectToAction("Index");
    }

    // Huellas / Dispositivos
    public async Task<IActionResult> Dispositivos([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var huellas = await _adminService.ListarHuellasAsync(page, pageSize, ct);
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View(huellas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleHuella(int id, bool bloquear, string? returnTo = null, CancellationToken ct = default)
    {
        var idAdmin = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (bloquear) await _adminService.BloquearHuellaAsync(id, idAdmin, ct);
        else          await _adminService.DesbloquearHuellaAsync(id, idAdmin, ct);
        TempData["Mensaje"] = bloquear ? "Dispositivo bloqueado." : "Dispositivo desbloqueado.";
        return returnTo == "Index"
            ? RedirectToAction("Index")
            : RedirectToAction("Dispositivos");
    }
}


