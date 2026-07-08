using SafeWoman.Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SafeWoman.API.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookies", Roles = "Administrador")]
public class AlertasController : Controller
{
    private readonly IAdminService _adminService;
    public AlertasController(IAdminService adminService) => _adminService = adminService;

    public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var alertas = await _adminService.ListarAlertasAsync(page, pageSize, ct);
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View(alertas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtenderAlerta(int id, string? returnTo = null, CancellationToken ct = default)
    {
        var idAdmin = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _adminService.AtenderAlertaAsync(id, idAdmin, ct);
        TempData["Mensaje"] = "Alerta marcada como atendida.";
        return returnTo == "Dashboard"
            ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
            : RedirectToAction("Index");
    }
}
