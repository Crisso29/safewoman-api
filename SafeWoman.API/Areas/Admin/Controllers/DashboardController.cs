using SafeWoman.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeWoman.Application.Services;

namespace SafeWoman.API.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookies", Roles = "Administrador")]
public class DashboardController : Controller
{
    private readonly IAdminService _adminService;
    public DashboardController(IAdminService adminService) => _adminService = adminService;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var dto = await _adminService.GetDashboardAsync(ct);
        return View(dto);
    }
}


