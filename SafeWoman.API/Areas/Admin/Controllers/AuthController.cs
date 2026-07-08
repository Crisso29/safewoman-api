using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SafeWoman.Application.DTOs.Admin;
using SafeWoman.Application.Interfaces;
using SafeWoman.Application.Services;
using SafeWoman.Domain.Enums;

namespace SafeWoman.API.Areas.Admin.Controllers;

[Area("Admin")]
public class AuthController : Controller
{
    private readonly AdminAuthService _authService;
    private readonly IAdminService    _adminService;

    public AuthController(AdminAuthService authService, IAdminService adminService)
    {
        _authService  = authService;
        _adminService = adminService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(AdminLoginRequest req, string? returnUrl = null, CancellationToken ct = default)
    {
        var admin = await _authService.LoginAsync(req.Email, req.Password, ct);
        if (admin is null)
        {
            ModelState.AddModelError("", "Email o contraseña incorrectos.");
            return View(req);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.IdAdmin.ToString()),
            new(ClaimTypes.Name,           admin.Nombre),
            new(ClaimTypes.Email,          admin.Email),
            new(ClaimTypes.Role,           "Administrador")
        };

        await HttpContext.SignInAsync("AdminCookies",
            new ClaimsPrincipal(new ClaimsIdentity(claims, "AdminCookies")),
            new AuthenticationProperties { IsPersistent = true });

        await _adminService.RegistrarLogAsync(
            admin.IdAdmin, AccionAuditoria.LoginAdmin,
            "ADMINISTRADOR", admin.IdAdmin, $"Login: {admin.Email}", ct);

        // Nunca hardcodear rutas: si returnUrl es local válido lo respetamos, si
        // no, el helper genera la URL correcta desde la tabla de rutas (así, si
        // cambia el path base del panel, no hay que tocar el controller).
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "AdminCookies")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var idAdmin = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : (int?)null;

        if (idAdmin.HasValue)
            await _adminService.RegistrarLogAsync(
                idAdmin, AccionAuditoria.LogoutAdmin,
                "ADMINISTRADOR", idAdmin, null, ct);

        await HttpContext.SignOutAsync("AdminCookies");

        // Redirige explícitamente al área Admin para no resolver al controlador de la API
        return RedirectToAction("Login", "Auth", new { area = "Admin" });
    }

    public IActionResult Denegado() => View();
}
