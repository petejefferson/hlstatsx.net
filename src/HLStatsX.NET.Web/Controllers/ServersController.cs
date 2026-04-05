using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class ServersController : Controller
{
    private readonly IServerService _servers;
    private readonly IConfiguration _config;

    public ServersController(IServerService servers, IConfiguration config)
    {
        _servers = servers;
        _config = config;
    }

    public async Task<IActionResult> Index(string? game, CancellationToken ct)
    {
        game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";
        var servers = await _servers.GetServersAsync(game, ct);
        return View(new ServerListViewModel(servers, game));
    }

    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var server = await _servers.GetServerAsync(id, ct);
        if (server is null) return NotFound();

        var livestats = await _servers.GetLivestatsAsync(id, ct);
        return View(new ServerDetailViewModel(server, livestats));
    }
}
