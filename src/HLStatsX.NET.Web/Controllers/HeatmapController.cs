using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.Controllers;

public class HeatmapController : Controller
{
    private readonly IHeatmapRepository _heatmap;
    private readonly IConfiguration _config;

    private const int BinSize = 64;

    public HeatmapController(IHeatmapRepository heatmap, IConfiguration config)
    {
        _heatmap = heatmap;
        _config = config;
    }

    // GET /Heatmap/Data?game=dods&map=dod_jagd&type=kills&playerId=
    public async Task<IActionResult> Data(string? game, string? map, string type = "kills", int? playerId = null, CancellationToken ct = default)
    {
        try
        {
            game ??= _config["HLStatsX:DefaultGame"] ?? "cstrike";

            if (string.IsNullOrEmpty(map))
                return Json(new { error = "No map specified." });

            var config = await _heatmap.GetConfigAsync(game, map, ct);
            int days = config?.Days ?? 0; // 0 = no cutoff when uncalibrated

            IReadOnlyList<HeatPoint> kills = [];
            IReadOnlyList<HeatPoint> deaths = [];

            if (type is "kills" or "both")
                kills = await _heatmap.GetKillPointsAsync(game, map, days, BinSize, playerId, ct);

            if (type is "deaths" or "both")
                deaths = await _heatmap.GetDeathPointsAsync(game, map, days, BinSize, playerId, ct);

            return Json(new
            {
                kills = kills.Select(p => new { x = p.X, y = p.Y, value = p.Value }),
                deaths = deaths.Select(p => new { x = p.X, y = p.Y, value = p.Value }),
                config = config is null ? null : new
                {
                    xoffset = config.XOffset,
                    yoffset = config.YOffset,
                    flipx = config.FlipX,
                    flipy = config.FlipY,
                    scale = config.Scale
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { configured = false, error = ex.Message });
        }
    }
}
