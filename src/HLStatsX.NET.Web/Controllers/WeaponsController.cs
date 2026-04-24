using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class WeaponsController : Controller
{
    private readonly IWeaponRepository _weapons;
    private readonly IConfiguration _config;

    public WeaponsController(IWeaponRepository weapons, IConfiguration config)
    {
        _weapons = weapons;
        _config = config;
    }

    public async Task<IActionResult> Index(string? game, int page = 1, string sortBy = "kills", bool desc = true, CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);
        var resultTask = _weapons.GetAllAsync(game, page, pageSize, sortBy, desc, ct);
        var totalsTask = _weapons.GetKillTotalsAsync(game, ct);
        await Task.WhenAll(resultTask, totalsTask);
        var totals = totalsTask.Result;
        return View(new WeaponListViewModel(resultTask.Result, game, sortBy, desc, totals.TotalKills, totals.TotalHeadshots));
    }

    public async Task<IActionResult> Detail(int id, int page = 1, string sortBy = "frags", bool desc = true, CancellationToken ct = default)
    {
        var weapon = await _weapons.GetByIdAsync(id, ct);
        if (weapon is null) return NotFound();

        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);
        var killersTask = _weapons.GetWeaponKillersAsync(weapon.Code, weapon.Game, page, pageSize, sortBy, desc, ct);
        var totalsTask  = _weapons.GetWeaponKillTotalsAsync(weapon.Code, weapon.Game, ct);
        await Task.WhenAll(killersTask, totalsTask);

        var totals = totalsTask.Result;
        return View(new WeaponDetailViewModel(weapon, weapon.Game, killersTask.Result, totals.TotalKills, totals.TotalHeadshots, sortBy, desc));
    }
}
