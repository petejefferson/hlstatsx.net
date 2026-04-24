using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class RolesController : Controller
{
    private readonly IRoleRepository _roles;
    private readonly IConfiguration _config;

    public RolesController(IRoleRepository roles, IConfiguration config)
    {
        _roles = roles;
        _config = config;
    }

    public async Task<IActionResult> Index(string? game, string sortBy = "kills", bool desc = true, CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        var rolesTask  = _roles.GetAllAsync(game, ct);
        var totalsTask = _roles.GetTotalsAsync(game, ct);
        await Task.WhenAll(rolesTask, totalsTask);

        var roles  = rolesTask.Result;
        var totals = totalsTask.Result;

        var sorted = SortRoles(roles, sortBy, desc);
        return View(new RoleListViewModel(sorted, game, sortBy, desc, totals.TotalKills, totals.TotalDeaths, totals.TotalPicked));
    }

    public async Task<IActionResult> Detail(string? code, string? game, int page = 1, string sortBy = "frags", bool desc = true, CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        if (string.IsNullOrWhiteSpace(code)) return NotFound();

        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);

        var role = await _roles.GetByCodeAsync(code, game, ct);
        if (role is null) return NotFound();

        var killersTask = _roles.GetRoleKillersAsync(code, game, page, pageSize, sortBy, desc, ct);
        var totalsTask  = _roles.GetRoleKillTotalsAsync(code, game, ct);
        await Task.WhenAll(killersTask, totalsTask);

        var totals = totalsTask.Result;
        return View(new RoleDetailViewModel(role, game, killersTask.Result, totals.TotalKills, totals.TotalHeadshots, sortBy, desc));
    }

    private static IReadOnlyList<Core.Entities.Role> SortRoles(IReadOnlyList<Core.Entities.Role> roles, string sortBy, bool desc)
    {
        IEnumerable<Core.Entities.Role> q = sortBy.ToLowerInvariant() switch
        {
            "role"     => desc ? roles.OrderByDescending(r => r.Name)   : roles.OrderBy(r => r.Name),
            "picked"   => desc ? roles.OrderByDescending(r => r.Picked) : roles.OrderBy(r => r.Picked),
            "ppercent" => desc ? roles.OrderByDescending(r => r.Picked) : roles.OrderBy(r => r.Picked),
            "deaths"   => desc ? roles.OrderByDescending(r => r.Deaths) : roles.OrderBy(r => r.Deaths),
            "dpercent" => desc ? roles.OrderByDescending(r => r.Deaths) : roles.OrderBy(r => r.Deaths),
            "kd"       => desc ? roles.OrderByDescending(r => r.Deaths == 0 ? r.Kills : (double)r.Kills / r.Deaths)
                                : roles.OrderBy(r => r.Deaths == 0 ? r.Kills : (double)r.Kills / r.Deaths),
            _          => desc ? roles.OrderByDescending(r => r.Kills)  : roles.OrderBy(r => r.Kills)
        };
        return q.ToList();
    }
}
