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

    public async Task<IActionResult> Index(string? game, int page = 1, string sortBy = "kills", CancellationToken ct = default)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        int pageSize = _config.GetValue<int>("HLStatsX:DefaultPageSize", 50);
        var result = await _weapons.GetAllAsync(game, page, pageSize, sortBy, ct);
        return View(new WeaponListViewModel(result, game, sortBy));
    }

    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var weapon = await _weapons.GetByIdAsync(id, ct);
        if (weapon is null) return NotFound();
        return View(new WeaponDetailViewModel(weapon, weapon.Game));
    }
}
