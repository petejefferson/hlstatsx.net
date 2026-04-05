using System.Security.Claims;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers.Admin;

[Route("Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IServerService _serverService;
    private readonly IPlayerRepository _playerRepo;
    private readonly IClanRepository _clanRepo;
    private readonly IConfiguration _config;

    public AdminController(IAdminService adminService, IServerService serverService,
        IPlayerRepository playerRepo, IClanRepository clanRepo, IConfiguration config)
    {
        _adminService = adminService;
        _serverService = serverService;
        _playerRepo = playerRepo;
        _clanRepo = clanRepo;
        _config = config;
    }

    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl) =>
        User.Identity?.IsAuthenticated == true
            ? RedirectToAction(nameof(Dashboard))
            : View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _adminService.AuthenticateAsync(model.Username, model.Password, ct);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, "Admin"),
            new("AccLevel", user.AccLevel.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return LocalRedirect(model.ReturnUrl ?? "/Admin/Dashboard");
    }

    [HttpPost("Logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("Dashboard")]
    [Authorize]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var game = _config["HLStatsX:DefaultGame"] ?? "cstrike";
        var servers = await _serverService.GetServersAsync(game, ct);
        var playersCount = await _playerRepo.GetTotalCountAsync(game, ct);
        var clansCount = await _clanRepo.GetTotalCountAsync(game, ct);
        var options = await _adminService.GetOptionsAsync(ct);

        var model = new AdminDashboardViewModel(
            playersCount,
            clansCount,
            servers.Count(s => s.IsActive),
            servers,
            options);

        return View(model);
    }

    [HttpGet("Users")]
    [Authorize]
    public async Task<IActionResult> Users(CancellationToken ct)
    {
        var users = await _adminService.GetUsersAsync(ct);
        return View(new AdminUserListViewModel(users));
    }

    [HttpGet("Users/Create")]
    [Authorize]
    public IActionResult CreateUser() => View(new CreateAdminUserViewModel());

    [HttpPost("Users/Create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateAdminUserViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new HLStatsX.NET.Core.Entities.AdminUser
        {
            Username = model.Username,
            AccLevel = model.AccLevel
        };

        await _adminService.CreateUserAsync(user, model.Password, ct);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost("Users/Delete/{username}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string username, CancellationToken ct)
    {
        await _adminService.DeleteUserAsync(username, ct);
        return RedirectToAction(nameof(Users));
    }

    [HttpGet("Servers")]
    [Authorize]
    public async Task<IActionResult> Servers(CancellationToken ct)
    {
        var servers = await _serverService.GetServersAsync(ct: ct);
        return View(servers);
    }

    [HttpPost("Servers/Delete/{id:int}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteServer(int id, CancellationToken ct)
    {
        await _serverService.DeleteServerAsync(id, ct);
        return RedirectToAction(nameof(Servers));
    }

    public IActionResult AccessDenied() => View();
}
