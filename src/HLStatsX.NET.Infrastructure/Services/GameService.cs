using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Infrastructure.Services;

public class GameService : IGameService
{
    private readonly IDbContextFactory<HLStatsDbContext> _factory;

    public GameService(IDbContextFactory<HLStatsDbContext> factory) => _factory = factory;

    public async Task<GamesListData> GetGamesListAsync(CancellationToken ct = default)
    {
        List<(string Code, string Name)> games;
        await using (var db = _factory.CreateDbContext())
        {
            var list = await db.Games
                .Where(g => g.Hidden == "0")
                .OrderBy(g => g.RealGame)
                .ThenBy(g => g.Name)
                .Select(g => new { g.Code, g.Name })
                .ToListAsync(ct);
            games = list.Select(g => (g.Code, g.Name)).ToList();
        }

        var gameCodes = games.Select(g => g.Code).ToList();

        var rowTasks     = games.Select(g => BuildRowAsync(g.Code, g.Name, ct)).ToArray();
        var playerTask   = TotalPlayersAsync(gameCodes, ct);
        var clanTask     = TotalClansAsync(gameCodes, ct);
        var serverTask   = TotalServersAsync(gameCodes, ct);
        var killTask     = TotalKillsAsync(gameCodes, ct);
        var lastKillTask = LastKillTimeAsync(ct);
        var deleteDaysTask = GetDeleteDaysAsync(ct);

        await Task.WhenAll(Task.WhenAll(rowTasks), playerTask, clanTask, serverTask, killTask, lastKillTask, deleteDaysTask);

        return new GamesListData(
            rowTasks.Select(t => t.Result).ToList(),
            await playerTask,
            await clanTask,
            await serverTask,
            await killTask,
            await lastKillTask,
            await deleteDaysTask);
    }

    private async Task<GameListRow> BuildRowAsync(string code, string name, CancellationToken ct)
    {
        await using var db = _factory.CreateDbContext();

        var topPlayer = await db.Players
            .Where(p => p.Game == code && p.HideRanking == 0)
            .OrderByDescending(p => p.Skill)
            .Select(p => new { p.PlayerId, p.LastName })
            .FirstOrDefaultAsync(ct);

        var topClan = await db.Clans
            .Where(c => c.Game == code && !c.IsHidden)
            .Select(c => new
            {
                c.ClanId,
                c.Name,
                MemberCount = c.Players.Count(p => p.HideRanking == 0),
                AvgSkill = c.Players.Where(p => p.HideRanking == 0).Average(p => (double?)p.Skill)
            })
            .Where(c => c.MemberCount >= 3 && c.AvgSkill != null)
            .OrderByDescending(c => c.AvgSkill)
            .Select(c => new { c.ClanId, c.Name })
            .FirstOrDefaultAsync(ct);

        var cap = await db.Servers
            .Where(s => s.Game == code)
            .GroupBy(_ => 1)
            .Select(g => new { Act = g.Sum(s => s.ActPlayers), Max = g.Sum(s => s.MaxPlayers) })
            .SingleOrDefaultAsync(ct);

        return new GameListRow(
            code, name,
            cap?.Act ?? 0,
            cap?.Max ?? 0,
            topPlayer?.PlayerId,
            topPlayer?.LastName,
            topClan?.ClanId,
            topClan?.Name);
    }

    private async Task<int> TotalPlayersAsync(List<string> codes, CancellationToken ct)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Players.CountAsync(p => codes.Contains(p.Game), ct);
    }

    private async Task<int> TotalClansAsync(List<string> codes, CancellationToken ct)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Clans.CountAsync(c => codes.Contains(c.Game), ct);
    }

    private async Task<int> TotalServersAsync(List<string> codes, CancellationToken ct)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Servers.CountAsync(s => codes.Contains(s.Game), ct);
    }

    private async Task<long> TotalKillsAsync(List<string> codes, CancellationToken ct)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Servers
            .Where(s => codes.Contains(s.Game))
            .Select(s => (long)s.Kills)
            .SumAsync(ct);
    }

    private async Task<DateTime?> LastKillTimeAsync(CancellationToken ct)
    {
        await using var db = _factory.CreateDbContext();
        return await db.EventFrags
            .OrderByDescending(e => e.Id)
            .Select(e => (DateTime?)e.EventTime)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<int> GetDeleteDaysAsync(CancellationToken ct)
    {
        await using var db = _factory.CreateDbContext();
        var val = await db.Options
            .Where(o => o.KeyName == "DeleteDays")
            .Select(o => o.Value)
            .SingleOrDefaultAsync(ct);
        return val is not null && int.TryParse(val, out var n) ? n : 90;
    }
}
