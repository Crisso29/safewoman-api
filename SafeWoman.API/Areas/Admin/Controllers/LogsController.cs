using SafeWoman.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeWoman.Application.Services;

namespace SafeWoman.API.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookies", Roles = "Administrador")]
public class LogsController : Controller
{
    private readonly IAdminService _adminService;
    public LogsController(IAdminService adminService) => _adminService = adminService;

    public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default)
    {
        var logs = await _adminService.ListarLogsAsync(page, pageSize, ct);
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View(logs);
    }
}


