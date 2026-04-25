using System.Security.Claims;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers.Admin;

[Route("Admin")]
[Authorize]
public class AdminController : Controller
{
    private readonly IAdminService _admin;
    private readonly IServerService _serverService;
    private readonly IPlayerRepository _playerRepo;
    private readonly IClanRepository _clanRepo;
    private readonly IGameRepository _gameRepo;
    private readonly IConfiguration _config;

    public AdminController(IAdminService admin, IServerService serverService,
        IPlayerRepository playerRepo, IClanRepository clanRepo,
        IGameRepository gameRepo, IConfiguration config)
    {
        _admin = admin;
        _serverService = serverService;
        _playerRepo = playerRepo;
        _clanRepo = clanRepo;
        _gameRepo = gameRepo;
        _config = config;
    }

    private string DefaultGame => _config["HLStatsX:DefaultGame"] ?? "cstrike";

    // ── Auth ────────────────────────────────────────────────────────────────

    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl) =>
        User.Identity?.IsAuthenticated == true
            ? RedirectToAction(nameof(Dashboard))
            : View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _admin.AuthenticateAsync(model.Username, model.Password, ct);
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    // ── Games list endpoint for sidebar ─────────────────────────────────────

    [HttpGet("GamesList")]
    public async Task<IActionResult> GamesList(CancellationToken ct)
    {
        var games = await _gameRepo.GetAllAsync(ct);
        return Json(games.Select(g => new { g.Code, g.Name }));
    }

    // ── Dashboard ───────────────────────────────────────────────────────────

    [HttpGet("Dashboard")]
    [HttpGet("")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var game = DefaultGame;
        var servers = await _serverService.GetServersAsync(game, ct);
        var playersCount = await _playerRepo.GetTotalCountAsync(game, ct);
        var clansCount = await _clanRepo.GetTotalCountAsync(game, ct);
        var options = await _admin.GetOptionsAsync(ct);
        var model = new AdminDashboardViewModel(playersCount, clansCount, servers.Count(s => s.IsActive), servers, options);
        ViewData["Title"] = "Dashboard";
        return View(model);
    }

    // ── Options ─────────────────────────────────────────────────────────────

    [HttpGet("Options")]
    public async Task<IActionResult> Options(CancellationToken ct)
    {
        var opts = await _admin.GetOptionsAsync(ct);
        ViewData["Title"] = "Site Options";
        return View(opts);
    }

    [HttpPost("Options")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Options(CancellationToken ct, [FromForm] IFormCollection form)
    {
        // Exclude the antiforgery token and any other non-option form fields
        var dict = form
            .Where(x => x.Key != "__RequestVerificationToken")
            .ToDictionary(x => x.Key, x => x.Value.ToString());
        await _admin.SaveOptionsAsync(dict, ct);
        TempData["Success"] = "Options saved.";
        return RedirectToAction(nameof(Options));
    }

    // ── Admin users ─────────────────────────────────────────────────────────

    [HttpGet("Users")]
    public async Task<IActionResult> Users(CancellationToken ct)
    {
        var users = await _admin.GetUsersAsync(ct);
        ViewData["Title"] = "Admin Users";
        return View(new AdminUserListViewModel(users));
    }

    [HttpGet("Users/Create")]
    public IActionResult CreateUser()
    {
        ViewData["Title"] = "Create Admin User";
        return View(new CreateAdminUserViewModel());
    }

    [HttpPost("Users/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateAdminUserViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new AdminUser { Username = model.Username, AccLevel = model.AccLevel };
        await _admin.CreateUserAsync(user, model.Password, ct);
        TempData["Success"] = $"User '{model.Username}' created.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet("Users/Edit/{username}")]
    public async Task<IActionResult> EditUser(string username, CancellationToken ct)
    {
        var users = await _admin.GetUsersAsync(ct);
        var user = users.FirstOrDefault(u => u.Username == username);
        if (user is null) return NotFound();
        ViewData["Title"] = $"Edit User: {username}";
        return View(new EditAdminUserViewModel { Username = user.Username, AccLevel = user.AccLevel });
    }

    [HttpPost("Users/Edit/{username}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(string username, EditAdminUserViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var users = await _admin.GetUsersAsync(ct);
        var user = users.FirstOrDefault(u => u.Username == username);
        if (user is null) return NotFound();
        user.AccLevel = model.AccLevel;
        await _admin.UpdateUserAsync(user, model.NewPassword, ct);
        TempData["Success"] = $"User '{username}' updated.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost("Users/Delete/{username}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string username, CancellationToken ct)
    {
        await _admin.DeleteUserAsync(username, ct);
        TempData["Success"] = $"User '{username}' deleted.";
        return RedirectToAction(nameof(Users));
    }

    // ── Games ────────────────────────────────────────────────────────────────

    [HttpGet("Games")]
    public async Task<IActionResult> Games(CancellationToken ct)
    {
        var games = await _gameRepo.GetAllAsync(ct);
        ViewData["Title"] = "Games";
        return View(games);
    }

    [HttpGet("Games/Create")]
    public async Task<IActionResult> CreateGame(CancellationToken ct)
    {
        ViewData["Title"] = "Add Game";
        ViewData["SupportedGames"] = await _admin.GetSupportedGamesAsync(ct);
        return View(new GameFormViewModel());
    }

    [HttpPost("Games/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGame(GameFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["SupportedGames"] = await _admin.GetSupportedGamesAsync(ct); return View(model); }
        var game = new Game { Code = model.Code, Name = model.Name, RealGame = model.RealGame, Hidden = model.Hidden ? "1" : "0" };
        await _admin.AddGameAsync(game, ct);
        TempData["Success"] = $"Game '{model.Code}' created.";
        return RedirectToAction(nameof(Games));
    }

    [HttpGet("Games/Edit/{code}")]
    public async Task<IActionResult> EditGame(string code, CancellationToken ct)
    {
        var game = await _gameRepo.GetByCodeAsync(code, ct);
        if (game is null) return NotFound();
        ViewData["Title"] = $"Edit Game: {code}";
        ViewData["SupportedGames"] = await _admin.GetSupportedGamesAsync(ct);
        return View(new GameFormViewModel { Code = game.Code, Name = game.Name, RealGame = game.RealGame, Hidden = game.IsHidden });
    }

    [HttpPost("Games/Edit/{code}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGame(string code, GameFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["SupportedGames"] = await _admin.GetSupportedGamesAsync(ct); return View(model); }
        var game = new Game { Code = code, Name = model.Name, RealGame = model.RealGame, Hidden = model.Hidden ? "1" : "0" };
        await _admin.UpdateGameAsync(game, ct);
        TempData["Success"] = $"Game '{code}' updated.";
        return RedirectToAction(nameof(Games));
    }

    [HttpPost("Games/Delete/{code}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGame(string code, CancellationToken ct)
    {
        await _admin.DeleteGameAsync(code, ct);
        TempData["Success"] = $"Game '{code}' and all its data deleted.";
        return RedirectToAction(nameof(Games));
    }

    // ── Clan tags ────────────────────────────────────────────────────────────

    [HttpGet("ClanTags")]
    public async Task<IActionResult> ClanTags(CancellationToken ct)
    {
        ViewData["Title"] = "Clan Tags";
        return View(await _admin.GetClanTagsAsync(ct));
    }

    [HttpGet("ClanTags/Create")]
    public IActionResult CreateClanTag() { ViewData["Title"] = "Add Clan Tag"; return View(new ClanTag()); }

    [HttpPost("ClanTags/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClanTag(ClanTag model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        await _admin.AddClanTagAsync(model, ct);
        TempData["Success"] = "Clan tag added.";
        return RedirectToAction(nameof(ClanTags));
    }

    [HttpGet("ClanTags/Edit/{id:int}")]
    public async Task<IActionResult> EditClanTag(int id, CancellationToken ct)
    {
        var tag = await _admin.GetClanTagByIdAsync(id, ct);
        if (tag is null) return NotFound();
        ViewData["Title"] = "Edit Clan Tag";
        return View(tag);
    }

    [HttpPost("ClanTags/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditClanTag(int id, ClanTag model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        model.Id = id;
        await _admin.UpdateClanTagAsync(model, ct);
        TempData["Success"] = "Clan tag updated.";
        return RedirectToAction(nameof(ClanTags));
    }

    [HttpPost("ClanTags/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClanTag(int id, CancellationToken ct)
    {
        await _admin.DeleteClanTagAsync(id, ct);
        TempData["Success"] = "Clan tag deleted.";
        return RedirectToAction(nameof(ClanTags));
    }

    // ── Host groups ──────────────────────────────────────────────────────────

    [HttpGet("HostGroups")]
    public async Task<IActionResult> HostGroups(CancellationToken ct)
    {
        ViewData["Title"] = "Host Groups";
        return View(await _admin.GetHostGroupsAsync(ct));
    }

    [HttpGet("HostGroups/Create")]
    public IActionResult CreateHostGroup() { ViewData["Title"] = "Add Host Group"; return View(new HostGroup()); }

    [HttpPost("HostGroups/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateHostGroup(HostGroup model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        await _admin.AddHostGroupAsync(model, ct);
        TempData["Success"] = "Host group added.";
        return RedirectToAction(nameof(HostGroups));
    }

    [HttpGet("HostGroups/Edit/{id:int}")]
    public async Task<IActionResult> EditHostGroup(int id, CancellationToken ct)
    {
        var g = await _admin.GetHostGroupByIdAsync(id, ct);
        if (g is null) return NotFound();
        ViewData["Title"] = "Edit Host Group";
        return View(g);
    }

    [HttpPost("HostGroups/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditHostGroup(int id, HostGroup model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        model.Id = id;
        await _admin.UpdateHostGroupAsync(model, ct);
        TempData["Success"] = "Host group updated.";
        return RedirectToAction(nameof(HostGroups));
    }

    [HttpPost("HostGroups/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHostGroup(int id, CancellationToken ct)
    {
        await _admin.DeleteHostGroupAsync(id, ct);
        TempData["Success"] = "Host group deleted.";
        return RedirectToAction(nameof(HostGroups));
    }

    // ── Servers ──────────────────────────────────────────────────────────────

    [HttpGet("Servers")]
    public async Task<IActionResult> Servers(string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        var servers = await _serverService.GetServersAsync(game, ct);
        ViewData["Title"] = "Servers";
        ViewData["AdminGame"] = game;
        return View(new GameScopedViewModel<IReadOnlyList<Server>>(servers, game));
    }

    [HttpGet("Servers/Create")]
    public async Task<IActionResult> CreateServer(string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        ViewData["Title"] = "Add Server";
        ViewData["AdminGame"] = game;
        ViewData["Games"] = await _gameRepo.GetAllAsync(ct);
        return View(new ServerFormViewModel { Game = game });
    }

    [HttpPost("Servers/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateServer(ServerFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["Games"] = await _gameRepo.GetAllAsync(ct); return View(model); }
        var server = new Server
        {
            Game = model.Game, Name = model.Name, Address = model.Address,
            Port = model.Port, PublicAddress = model.PublicAddress ?? string.Empty,
            RconPassword = model.RconPassword ?? string.Empty, SortOrder = model.SortOrder
        };
        await _admin.AddServerAsync(server, ct);
        TempData["Success"] = $"Server '{model.Name}' added.";
        return RedirectToAction(nameof(Servers), new { game = model.Game });
    }

    [HttpGet("Servers/Edit/{id:int}")]
    public async Task<IActionResult> EditServer(int id, CancellationToken ct)
    {
        var s = await _admin.GetServerByIdAsync(id, ct);
        if (s is null) return NotFound();
        ViewData["Title"] = $"Edit Server: {s.Name}";
        ViewData["AdminGame"] = s.Game;
        ViewData["Games"] = await _gameRepo.GetAllAsync(ct);
        return View(new ServerFormViewModel
        {
            Game = s.Game, Name = s.Name, Address = s.Address, Port = s.Port,
            PublicAddress = s.PublicAddress, RconPassword = s.RconPassword, SortOrder = s.SortOrder
        });
    }

    [HttpPost("Servers/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditServer(int id, ServerFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["Games"] = await _gameRepo.GetAllAsync(ct); return View(model); }
        var s = await _admin.GetServerByIdAsync(id, ct);
        if (s is null) return NotFound();
        s.Name = model.Name; s.Address = model.Address; s.Port = model.Port;
        s.PublicAddress = model.PublicAddress ?? string.Empty;
        s.RconPassword = model.RconPassword ?? string.Empty; s.SortOrder = model.SortOrder;
        await _admin.UpdateServerAsync(s, ct);
        TempData["Success"] = $"Server '{model.Name}' updated.";
        return RedirectToAction(nameof(Servers), new { game = model.Game });
    }

    [HttpGet("Servers/Settings/{id:int}")]
    public async Task<IActionResult> ServerSettings(int id, CancellationToken ct)
    {
        var s = await _admin.GetServerByIdAsync(id, ct);
        if (s is null) return NotFound();
        var config = await _admin.GetServerConfigAsync(id, ct);
        var allServers = await _serverService.GetServersAsync(s.Game, ct);
        ViewData["Title"] = $"Server Config: {s.Name}";
        ViewData["AdminGame"] = s.Game;
        return View(new ServerSettingsViewModel(s, config, allServers));
    }

    [HttpPost("Servers/Settings/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveServerSettings(int id, CancellationToken ct, [FromForm] IFormCollection form)
    {
        var s = await _admin.GetServerByIdAsync(id, ct);
        if (s is null) return NotFound();
        foreach (var key in form.Keys.Where(k => k.StartsWith("cfg_")))
        {
            var param = key[4..];
            await _admin.SetServerConfigAsync(id, param, form[key].ToString(), ct);
        }
        TempData["Success"] = "Server settings saved.";
        return RedirectToAction(nameof(ServerSettings), new { id });
    }

    [HttpPost("Servers/Settings/{id:int}/CopyFrom/{fromId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyServerSettings(int id, int fromId, CancellationToken ct)
    {
        await _admin.CopyServerConfigAsync(fromId, id, ct);
        TempData["Success"] = "Settings copied.";
        return RedirectToAction(nameof(ServerSettings), new { id });
    }

    [HttpPost("Servers/Settings/{id:int}/Reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetServerSettings(int id, CancellationToken ct)
    {
        var s = await _admin.GetServerByIdAsync(id, ct);
        if (s is null) return NotFound();
        await _admin.ResetServerConfigToDefaultsAsync(id, s.Game, ct);
        TempData["Success"] = "Settings reset to defaults.";
        return RedirectToAction(nameof(ServerSettings), new { id });
    }

    [HttpPost("Servers/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteServer(int id, string? game, CancellationToken ct)
    {
        await _admin.DeleteServerAsync(id, ct);
        TempData["Success"] = "Server deleted.";
        return RedirectToAction(nameof(Servers), new { game = game ?? DefaultGame });
    }

    // ── Actions ──────────────────────────────────────────────────────────────

    [HttpGet("Actions")]
    public async Task<IActionResult> Actions(string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        var items = await _admin.GetActionsAsync(game, ct);
        ViewData["Title"] = "Actions"; ViewData["AdminGame"] = game;
        return View(new GameScopedViewModel<IReadOnlyList<GameAction>>(items, game));
    }

    [HttpGet("Actions/Create")]
    public async Task<IActionResult> CreateAction(string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        ViewData["Title"] = "Add Action"; ViewData["AdminGame"] = game;
        ViewData["Teams"] = await _admin.GetTeamsAsync(game, ct);
        return View(new GameAction { Game = game });
    }

    [HttpPost("Actions/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAction(GameAction model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["Teams"] = await _admin.GetTeamsAsync(model.Game, ct); return View(model); }
        await _admin.AddActionAsync(model, ct);
        TempData["Success"] = $"Action '{model.Code}' added.";
        return RedirectToAction(nameof(Actions), new { game = model.Game });
    }

    [HttpGet("Actions/Edit/{id:int}")]
    public async Task<IActionResult> EditAction(int id, CancellationToken ct)
    {
        var item = await _admin.GetActionByIdAsync(id, ct);
        if (item is null) return NotFound();
        ViewData["Title"] = $"Edit Action: {item.Code}"; ViewData["AdminGame"] = item.Game;
        ViewData["Teams"] = await _admin.GetTeamsAsync(item.Game, ct);
        return View(item);
    }

    [HttpPost("Actions/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAction(int id, GameAction model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["Teams"] = await _admin.GetTeamsAsync(model.Game, ct); return View(model); }
        model.ActionId = id;
        await _admin.UpdateActionAsync(model, ct);
        TempData["Success"] = $"Action '{model.Code}' updated.";
        return RedirectToAction(nameof(Actions), new { game = model.Game });
    }

    [HttpPost("Actions/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAction(int id, string? game, CancellationToken ct)
    {
        await _admin.DeleteActionAsync(id, ct);
        TempData["Success"] = "Action deleted.";
        return RedirectToAction(nameof(Actions), new { game = game ?? DefaultGame });
    }

    // ── Teams ─────────────────────────────────────────────────────────────────

    [HttpGet("Teams")]
    public async Task<IActionResult> Teams(string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        ViewData["Title"] = "Teams"; ViewData["AdminGame"] = game;
        return View(new GameScopedViewModel<IReadOnlyList<Team>>(await _admin.GetTeamsAsync(game, ct), game));
    }

    [HttpGet("Teams/Create")]
    public IActionResult CreateTeam(string? game) { game ??= DefaultGame; ViewData["Title"] = "Add Team"; ViewData["AdminGame"] = game; return View(new Team { Game = game }); }

    [HttpPost("Teams/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTeam(Team model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        await _admin.AddTeamAsync(model, ct);
        TempData["Success"] = $"Team '{model.Code}' added.";
        return RedirectToAction(nameof(Teams), new { game = model.Game });
    }

    [HttpGet("Teams/Edit/{id:int}")]
    public async Task<IActionResult> EditTeam(int id, CancellationToken ct)
    {
        var item = await _admin.GetTeamByIdAsync(id, ct);
        if (item is null) return NotFound();
        ViewData["Title"] = $"Edit Team: {item.Code}"; ViewData["AdminGame"] = item.Game;
        return View(item);
    }

    [HttpPost("Teams/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTeam(int id, Team model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        model.TeamId = id;
        await _admin.UpdateTeamAsync(model, ct);
        TempData["Success"] = $"Team '{model.Code}' updated.";
        return RedirectToAction(nameof(Teams), new { game = model.Game });
    }

    [HttpPost("Teams/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTeam(int id, string? game, CancellationToken ct)
    {
        await _admin.DeleteTeamAsync(id, ct);
        TempData["Success"] = "Team deleted.";
        return RedirectToAction(nameof(Teams), new { game = game ?? DefaultGame });
    }

    // ── Roles ─────────────────────────────────────────────────────────────────

    [HttpGet("Roles")]
    public async Task<IActionResult> AdminRoles(string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        ViewData["Title"] = "Roles"; ViewData["AdminGame"] = game;
        return View("Roles", new GameScopedViewModel<IReadOnlyList<Role>>(await _admin.GetRolesAsync(game, ct), game));
    }

    [HttpGet("Roles/Create")]
    public IActionResult CreateRole(string? game) { game ??= DefaultGame; ViewData["Title"] = "Add Role"; ViewData["AdminGame"] = game; return View(new Role { Game = game }); }

    [HttpPost("Roles/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRole(Role model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        await _admin.AddRoleAsync(model, ct);
        TempData["Success"] = $"Role '{model.Code}' added.";
        return RedirectToAction(nameof(AdminRoles), new { game = model.Game });
    }

    [HttpGet("Roles/Edit/{id:int}")]
    public async Task<IActionResult> EditRole(int id, CancellationToken ct)
    {
        var item = await _admin.GetRoleByIdAsync(id, ct);
        if (item is null) return NotFound();
        ViewData["Title"] = $"Edit Role: {item.Code}"; ViewData["AdminGame"] = item.Game;
        return View(item);
    }

    [HttpPost("Roles/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRole(int id, Role model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        model.RoleId = id;
        await _admin.UpdateRoleAsync(model, ct);
        TempData["Success"] = $"Role '{model.Code}' updated.";
        return RedirectToAction(nameof(AdminRoles), new { game = model.Game });
    }

    [HttpPost("Roles/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRole(int id, string? game, CancellationToken ct)
    {
        await _admin.DeleteRoleAsync(id, ct);
        TempData["Success"] = "Role deleted.";
        return RedirectToAction(nameof(AdminRoles), new { game = game ?? DefaultGame });
    }

    // ── Weapons ───────────────────────────────────────────────────────────────

    [HttpGet("Weapons")]
    public async Task<IActionResult> AdminWeapons(string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        ViewData["Title"] = "Weapons"; ViewData["AdminGame"] = game;
        return View("Weapons", new GameScopedViewModel<IReadOnlyList<Weapon>>(await _admin.GetWeaponsAsync(game, ct), game));
    }

    [HttpGet("Weapons/Create")]
    public IActionResult CreateWeapon(string? game) { game ??= DefaultGame; ViewData["Title"] = "Add Weapon"; ViewData["AdminGame"] = game; return View(new Weapon { Game = game, Modifier = 1.0f }); }

    [HttpPost("Weapons/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWeapon(Weapon model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        await _admin.AddWeaponAsync(model, ct);
        TempData["Success"] = $"Weapon '{model.Code}' added.";
        return RedirectToAction(nameof(AdminWeapons), new { game = model.Game });
    }

    [HttpGet("Weapons/Edit/{id:int}")]
    public async Task<IActionResult> EditWeapon(int id, CancellationToken ct)
    {
        var item = await _admin.GetWeaponByIdAsync(id, ct);
        if (item is null) return NotFound();
        ViewData["Title"] = $"Edit Weapon: {item.Code}"; ViewData["AdminGame"] = item.Game;
        return View(item);
    }

    [HttpPost("Weapons/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditWeapon(int id, Weapon model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        model.WeaponId = id;
        await _admin.UpdateWeaponAsync(model, ct);
        TempData["Success"] = $"Weapon '{model.Code}' updated.";
        return RedirectToAction(nameof(AdminWeapons), new { game = model.Game });
    }

    [HttpPost("Weapons/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteWeapon(int id, string? game, CancellationToken ct)
    {
        await _admin.DeleteWeaponAsync(id, ct);
        TempData["Success"] = "Weapon deleted.";
        return RedirectToAction(nameof(AdminWeapons), new { game = game ?? DefaultGame });
    }

    // ── Ranks ─────────────────────────────────────────────────────────────────

    [HttpGet("Ranks")]
    public async Task<IActionResult> AdminRanks(string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        ViewData["Title"] = "Ranks"; ViewData["AdminGame"] = game;
        return View("Ranks", new GameScopedViewModel<IReadOnlyList<Rank>>(await _admin.GetRanksAsync(game, ct), game));
    }

    [HttpGet("Ranks/Create")]
    public IActionResult CreateRank(string? game) { game ??= DefaultGame; ViewData["Title"] = "Add Rank"; ViewData["AdminGame"] = game; return View(new Rank { Game = game }); }

    [HttpPost("Ranks/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRank(Rank model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        await _admin.AddRankAsync(model, ct);
        TempData["Success"] = $"Rank '{model.RankName}' added.";
        return RedirectToAction(nameof(AdminRanks), new { game = model.Game });
    }

    [HttpGet("Ranks/Edit/{id:int}")]
    public async Task<IActionResult> EditRank(int id, CancellationToken ct)
    {
        var item = await _admin.GetRankByIdAsync(id, ct);
        if (item is null) return NotFound();
        ViewData["Title"] = $"Edit Rank: {item.RankName}"; ViewData["AdminGame"] = item.Game;
        return View(item);
    }

    [HttpPost("Ranks/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRank(int id, Rank model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        model.RankId = id;
        await _admin.UpdateRankAsync(model, ct);
        TempData["Success"] = $"Rank '{model.RankName}' updated.";
        return RedirectToAction(nameof(AdminRanks), new { game = model.Game });
    }

    [HttpPost("Ranks/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRank(int id, string? game, CancellationToken ct)
    {
        await _admin.DeleteRankAsync(id, ct);
        TempData["Success"] = "Rank deleted.";
        return RedirectToAction(nameof(AdminRanks), new { game = game ?? DefaultGame });
    }

    // ── Ribbons ───────────────────────────────────────────────────────────────

    [HttpGet("Ribbons")]
    public async Task<IActionResult> AdminRibbons(string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        ViewData["Title"] = "Ribbons"; ViewData["AdminGame"] = game;
        return View("Ribbons", new GameScopedViewModel<IReadOnlyList<Ribbon>>(await _admin.GetRibbonsAsync(game, ct), game));
    }

    [HttpGet("Ribbons/Create")]
    public IActionResult CreateRibbon(string? game) { game ??= DefaultGame; ViewData["Title"] = "Add Ribbon"; ViewData["AdminGame"] = game; return View(new Ribbon { Game = game }); }

    [HttpPost("Ribbons/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRibbon(Ribbon model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        await _admin.AddRibbonAsync(model, ct);
        TempData["Success"] = $"Ribbon '{model.RibbonName}' added.";
        return RedirectToAction(nameof(AdminRibbons), new { game = model.Game });
    }

    [HttpGet("Ribbons/Edit/{id:int}")]
    public async Task<IActionResult> EditRibbon(int id, CancellationToken ct)
    {
        var item = await _admin.GetRibbonByIdAsync(id, ct);
        if (item is null) return NotFound();
        ViewData["Title"] = $"Edit Ribbon: {item.RibbonName}"; ViewData["AdminGame"] = item.Game;
        return View(item);
    }

    [HttpPost("Ribbons/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRibbon(int id, Ribbon model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        model.RibbonId = id;
        await _admin.UpdateRibbonAsync(model, ct);
        TempData["Success"] = $"Ribbon '{model.RibbonName}' updated.";
        return RedirectToAction(nameof(AdminRibbons), new { game = model.Game });
    }

    [HttpPost("Ribbons/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRibbon(int id, string? game, CancellationToken ct)
    {
        await _admin.DeleteRibbonAsync(id, ct);
        TempData["Success"] = "Ribbon deleted.";
        return RedirectToAction(nameof(AdminRibbons), new { game = game ?? DefaultGame });
    }

    // ── Ribbon triggers ───────────────────────────────────────────────────────

    [HttpGet("RibbonTriggers")]
    public async Task<IActionResult> RibbonTriggers(string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        ViewData["Title"] = "Ribbon Triggers"; ViewData["AdminGame"] = game;
        IReadOnlyList<RibbonTrigger> triggers;
        try
        {
            triggers = await _admin.GetRibbonTriggersAsync(game, ct);
        }
        catch (Exception ex) when (ex.Message.Contains("doesn't exist") || ex.Message.Contains("not exist"))
        {
            TempData["Error"] = "The hlstats_Ribbons_Trigger table does not exist in this database. Ribbon triggers are not available.";
            triggers = Array.Empty<RibbonTrigger>();
        }
        ViewData["Ribbons"] = await _admin.GetRibbonsAsync(game, ct);
        ViewData["Awards"] = await _admin.GetAwardsAsync(game, "W", ct);
        return View(new GameScopedViewModel<IReadOnlyList<RibbonTrigger>>(triggers, game));
    }

    [HttpGet("RibbonTriggers/Create")]
    public async Task<IActionResult> CreateRibbonTrigger(string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        ViewData["Title"] = "Add Ribbon Trigger"; ViewData["AdminGame"] = game;
        ViewData["Ribbons"] = await _admin.GetRibbonsAsync(game, ct);
        return View(new RibbonTrigger());
    }

    [HttpPost("RibbonTriggers/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRibbonTrigger(RibbonTrigger model, string game, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["Ribbons"] = await _admin.GetRibbonsAsync(game, ct); return View(model); }
        await _admin.AddRibbonTriggerAsync(model, ct);
        TempData["Success"] = "Ribbon trigger added.";
        return RedirectToAction(nameof(RibbonTriggers), new { game });
    }

    [HttpGet("RibbonTriggers/Edit/{id:int}")]
    public async Task<IActionResult> EditRibbonTrigger(int id, string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        var item = await _admin.GetRibbonTriggerByIdAsync(id, ct);
        if (item is null) return NotFound();
        ViewData["Title"] = "Edit Ribbon Trigger"; ViewData["AdminGame"] = game;
        ViewData["Ribbons"] = await _admin.GetRibbonsAsync(game, ct);
        return View(item);
    }

    [HttpPost("RibbonTriggers/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRibbonTrigger(int id, RibbonTrigger model, string game, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["Ribbons"] = await _admin.GetRibbonsAsync(game, ct); return View(model); }
        model.TriggerId = id;
        await _admin.UpdateRibbonTriggerAsync(model, ct);
        TempData["Success"] = "Ribbon trigger updated.";
        return RedirectToAction(nameof(RibbonTriggers), new { game });
    }

    [HttpPost("RibbonTriggers/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRibbonTrigger(int id, string? game, CancellationToken ct)
    {
        await _admin.DeleteRibbonTriggerAsync(id, ct);
        TempData["Success"] = "Ribbon trigger deleted.";
        return RedirectToAction(nameof(RibbonTriggers), new { game = game ?? DefaultGame });
    }

    // ── Awards ────────────────────────────────────────────────────────────────

    [HttpGet("Awards/{type}")]
    public async Task<IActionResult> Awards(string type, string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        var awards = await _admin.GetAwardsAsync(game, type, ct);
        ViewData["Title"] = $"Awards ({AwardTypeLabel(type)})";
        ViewData["AdminGame"] = game;
        ViewData["AwardType"] = type;
        ViewData["Weapons"] = await _admin.GetWeaponsAsync(game, ct);
        ViewData["Actions"] = await _admin.GetActionsAsync(game, ct);
        return View(new GameScopedViewModel<IReadOnlyList<Award>>(awards, game));
    }

    [HttpGet("Awards/{type}/Create")]
    public async Task<IActionResult> CreateAward(string type, string? game, CancellationToken ct)
    {
        game ??= DefaultGame;
        ViewData["Title"] = $"Add Award ({AwardTypeLabel(type)})";
        ViewData["AdminGame"] = game;
        ViewData["AwardType"] = type;
        ViewData["Weapons"] = await _admin.GetWeaponsAsync(game, ct);
        ViewData["Actions"] = await _admin.GetActionsAsync(game, ct);
        return View(new Award { Game = game, AwardType = type });
    }

    [HttpPost("Awards/{type}/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAward(string type, Award model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["AwardType"] = type; return View(model); }
        model.AwardType = type;
        await _admin.AddAwardAsync(model, ct);
        TempData["Success"] = $"Award '{model.Name}' added.";
        return RedirectToAction(nameof(Awards), new { type, game = model.Game });
    }

    [HttpGet("Awards/{type}/Edit/{id:int}")]
    public async Task<IActionResult> EditAward(string type, int id, CancellationToken ct)
    {
        var item = await _admin.GetAwardByIdAsync(id, ct);
        if (item is null) return NotFound();
        ViewData["Title"] = $"Edit Award: {item.Name}";
        ViewData["AdminGame"] = item.Game;
        ViewData["AwardType"] = type;
        ViewData["Weapons"] = await _admin.GetWeaponsAsync(item.Game, ct);
        ViewData["Actions"] = await _admin.GetActionsAsync(item.Game, ct);
        return View(item);
    }

    [HttpPost("Awards/{type}/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAward(string type, int id, Award model, CancellationToken ct)
    {
        if (!ModelState.IsValid) { ViewData["AwardType"] = type; return View(model); }
        model.AwardId = id;
        model.AwardType = type;
        await _admin.UpdateAwardAsync(model, ct);
        TempData["Success"] = $"Award '{model.Name}' updated.";
        return RedirectToAction(nameof(Awards), new { type, game = model.Game });
    }

    [HttpPost("Awards/{type}/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAward(string type, int id, string? game, CancellationToken ct)
    {
        await _admin.DeleteAwardAsync(id, ct);
        TempData["Success"] = "Award deleted.";
        return RedirectToAction(nameof(Awards), new { type, game = game ?? DefaultGame });
    }

    // ── Tools ─────────────────────────────────────────────────────────────────

    [HttpGet("Tools/EditDetails")]
    public IActionResult EditDetails(string? query, string? type)
    {
        ViewData["Title"] = "Edit Player/Clan";
        return View(new EditDetailsSearchViewModel { Query = query, Type = type });
    }

    [HttpGet("Tools/EditPlayer/{id:int}")]
    public async Task<IActionResult> EditPlayer(int id, CancellationToken ct)
    {
        var player = await _admin.GetPlayerForEditAsync(id, ct);
        if (player is null) return NotFound();
        var ips = await _admin.GetPlayerIpsAsync(id, ct);
        ViewData["Title"] = $"Edit Player: {player.LastName}";
        return View(new EditPlayerViewModel(player, ips));
    }

    [HttpPost("Tools/EditPlayer/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPlayer(int id, EditPlayerFormModel form, CancellationToken ct)
    {
        var player = await _admin.GetPlayerForEditAsync(id, ct);
        if (player is null) return NotFound();
        player.FullName = form.FullName;
        player.Email = form.Email;
        player.Homepage = form.Homepage;
        player.Flag = form.Flag;
        player.Skill = form.Skill;
        player.Kills = form.Kills;
        player.Deaths = form.Deaths;
        player.Headshots = form.Headshots;
        player.Suicides = form.Suicides;
        player.HideRanking = form.HideRanking;
        player.BlockAvatar = form.BlockAvatar;
        await _admin.UpdatePlayerAsync(player, ct);
        TempData["Success"] = $"Player '{player.LastName}' updated.";
        return RedirectToAction(nameof(EditPlayer), new { id });
    }

    [HttpGet("Tools/EditClan/{id:int}")]
    public async Task<IActionResult> EditClan(int id, CancellationToken ct)
    {
        var clan = await _admin.GetClanForEditAsync(id, ct);
        if (clan is null) return NotFound();
        ViewData["Title"] = $"Edit Clan: {clan.Name}";
        return View(clan);
    }

    [HttpPost("Tools/EditClan/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditClan(int id, Clan model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        model.ClanId = id;
        await _admin.UpdateClanAsync(model, ct);
        TempData["Success"] = $"Clan '{model.Name}' updated.";
        return RedirectToAction(nameof(EditClan), new { id });
    }

    [HttpGet("Tools/AdminEvents")]
    public async Task<IActionResult> AdminEvents(string? type, int page = 1, CancellationToken ct = default)
    {
        const int pageSize = 50;
        var events = await _admin.GetAdminEventsAsync(type, page, pageSize, ct);
        var total = await _admin.GetAdminEventsCountAsync(type, ct);
        ViewData["Title"] = "Admin Events";
        return View(new AdminEventsViewModel(events, type, page, pageSize, total));
    }

    [HttpGet("Tools/Reset")]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        ViewData["Title"] = "Reset Stats";
        ViewData["Games"] = await _gameRepo.GetAllAsync(ct);
        return View(new ResetViewModel());
    }

    [HttpPost("Tools/Reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(ResetViewModel model, CancellationToken ct)
    {
        var opts = new ResetOptions
        {
            ClearAwards = model.ClearAwards, ClearHistory = model.ClearHistory,
            ClearPlayerNames = model.ClearPlayerNames, ClearSkill = model.ClearSkill,
            ClearCounts = model.ClearCounts, ClearMapData = model.ClearMapData,
            ClearBans = model.ClearBans, ClearEvents = model.ClearEvents,
            DeletePlayers = model.DeletePlayers
        };
        var log = await _admin.ResetStatsAsync(string.IsNullOrEmpty(model.Game) ? null : model.Game, opts, ct);
        ViewData["Title"] = "Reset Stats";
        ViewData["Games"] = await _gameRepo.GetAllAsync(ct);
        ViewData["Log"] = log;
        TempData["Success"] = "Reset complete.";
        return View(model);
    }

    [HttpGet("Tools/Optimize")]
    public IActionResult Optimize()
    {
        ViewData["Title"] = "DB Optimize";
        return View();
    }

    [HttpPost("Tools/Optimize")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OptimizePost(CancellationToken ct)
    {
        await _admin.OptimizeTablesAsync(ct);
        TempData["Success"] = "OPTIMIZE TABLE and ANALYZE TABLE completed.";
        return RedirectToAction(nameof(Optimize));
    }

    [HttpGet("Tools/CopySettings")]
    public async Task<IActionResult> CopySettings(CancellationToken ct)
    {
        ViewData["Title"] = "Copy Game Settings";
        ViewData["Games"] = await _gameRepo.GetAllAsync(ct);
        return View();
    }

    [HttpPost("Tools/CopySettings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopySettings(string fromGame, string toGame, CancellationToken ct)
    {
        var log = await _admin.CopyGameSettingsAsync(fromGame, toGame, ct);
        ViewData["Title"] = "Copy Game Settings";
        ViewData["Games"] = await _gameRepo.GetAllAsync(ct);
        ViewData["Log"] = log;
        TempData["Success"] = $"Settings copied from '{fromGame}' to '{toGame}'.";
        return View();
    }

    [HttpGet("Tools/Cleanup")]
    public async Task<IActionResult> Cleanup(CancellationToken ct)
    {
        ViewData["Title"] = "Cleanup Inactive Players";
        ViewData["Games"] = await _gameRepo.GetAllAsync(ct);
        return View(new CleanupViewModel());
    }

    [HttpPost("Tools/Cleanup")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cleanup(CleanupViewModel model, CancellationToken ct)
    {
        var log = await _admin.CleanupInactiveAsync(string.IsNullOrEmpty(model.Game) ? null : model.Game, model.MinKills, ct);
        ViewData["Title"] = "Cleanup Inactive Players";
        ViewData["Games"] = await _gameRepo.GetAllAsync(ct);
        ViewData["Log"] = log;
        TempData["Success"] = "Cleanup complete.";
        return View(model);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string AwardTypeLabel(string type) => type switch
    {
        "W" => "Weapons",
        "O" => "Player Actions",
        "P" => "Player-vs-Player",
        "V" => "Victims",
        _ => type
    };
}
