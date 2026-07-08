using SafeWoman.Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeWoman.Application.Services;

namespace SafeWoman.API.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookies", Roles = "Administrador")]
public class VictimasController : Controller
{
    private readonly IAdminService _adminService;
    public VictimasController(IAdminService adminService) => _adminService = adminService;

    public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var victimas = await _adminService.ListarVictimasAsync(page, pageSize, ct);
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View(victimas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, bool activar, CancellationToken ct)
    {
        var idAdmin = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (activar) await _adminService.ActivarVictimaAsync(id, idAdmin, ct);
        else         await _adminService.DesactivarVictimaAsync(id, idAdmin, ct);
        TempData["Mensaje"] = activar ? "Cuenta activada." : "Cuenta desactivada.";
        return RedirectToAction("Index");
    }
}


