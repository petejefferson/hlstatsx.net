using System.Text.RegularExpressions;
using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Daemon.Configuration;
using HLStatsX.NET.Daemon.Parsing;
using HLStatsX.NET.Daemon.State;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HLStatsX.NET.Daemon.Events;

/// <summary>
/// Routes parsed log-line event text to the appropriate handler by trying each
/// regex pattern in the same priority order as the Perl daemon's
/// <c>elsif</c> chain in <c>hlstats.pl</c>.
/// </summary>
public sealed partial class EventRouter
{
    // ── Source-generated regexes (order matches Perl dispatch) ──────────────

    // Statsme weaponstats: [STATSME] "player" triggered "weaponstats[2]"[props]
    [GeneratedRegex(
        @"^(?:\[STATSME\] )?""(.+?(?:<[^>]*>)*?)"" triggered ""(weaponstats\d?)""(.*)$",
        RegexOptions.Singleline)]
    private static partial Regex StatsmeWeaponRegex();

    // Statsme latency/time (deferred — matched only to skip the line)
    [GeneratedRegex(
        @"^(?:\[STATSME\] )?""(.+?(?:<[^>]*>)*?)"" triggered ""(latency|time)""(.*)$",
        RegexOptions.Singleline)]
    private static partial Regex StatsmeLatencyRegex();

    // Kill: "killer" [coords] killed "victim" [coords] with "weapon" [props]
    // Groups: 1=killer 2-4=CSGO killer coords 5=victim 6-8=CSGO victim coords 9=weapon 10=props
    [GeneratedRegex(
        @"^(?:\(DEATH\))?""(.+?(?:<[^>]*>)*?)""(?:\s\[(-?\d+)\s(-?\d+)\s(-?\d+)\])?\skilled\s""(.+?(?:<[^>]*>)*?)""(?:\s\[(-?\d+)\s(-?\d+)\s(-?\d+)\])?\swith\s""([^""]*)""(.*)$",
        RegexOptions.Singleline)]
    private static partial Regex KillRegex();

    // CS:GO bracket-coords suicide: "player" [x y z] verb "weapon"[props]
    // Groups: 1=player 2-4=coords 5=verb 6=weapon 7=props
    [GeneratedRegex(
        @"^""(.+?(?:<[^>]*>)*?)""\s\[(-?\d+)\s(-?\d+)\s(-?\d+)\]\s([a-zA-Z,_\s]+)\s""([^""]*)""(.*)$",
        RegexOptions.Singleline)]
    private static partial Regex BracketSuicideRegex();

    // Player-verb-obj: "player" verb "obj"[props]
    // Groups: 1=player 2=verb 3=obj 4=props
    [GeneratedRegex(
        @"^""(.+?(?:<[^>]*>)*?)""\s([a-zA-Z,_\s]+)\s""([^""]*)""(.*)$",
        RegexOptions.Singleline)]
    private static partial Regex PlayerVerbObjRegex();

    // Player-verb: "player" verb[props]  (optional Kick: prefix; verb = anything up to '(')
    // Groups: 1=player 2=verb 3=props
    [GeneratedRegex(
        @"^(?:Kick: )?""(.+?(?:<[^>]*>)*?)""\s([^(]+)(.*)$",
        RegexOptions.Singleline)]
    private static partial Regex PlayerVerbRegex();

    // Team triggered: Team "team" verb "obj"[props]
    // Groups: 1=team 2=verb 3=obj 4=props
    [GeneratedRegex(
        @"^Team\s""([^""]+)""\s([^""(]+)\s""([^""]*)""(.*)$",
        RegexOptions.Singleline)]
    private static partial Regex TeamTriggeredRegex();

    // Sourcemod/AMXX: [plugin.smx/amxx] message
    [GeneratedRegex(@"^\[(.+)\.(smx|amxx)\]\s*(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SourcemodRegex();

    // Mani Admin Plugin: [MANI_ADMIN_PLUGIN] message
    [GeneratedRegex(@"^\[MANI_ADMIN_PLUGIN\]\s*(.+)$", RegexOptions.Singleline)]
    private static partial Regex ManiAdminRegex();

    // World verb "obj"[props]: verb "obj" (World triggered / Loading map / Started map)
    // Groups: 1=verb 2=obj 3=props
    [GeneratedRegex(
        @"^([^""(]+)\s""([^""]*)""(.*)$",
        RegexOptions.Singleline)]
    private static partial Regex WorldVerbObjRegex();

    // ── Fields ──────────────────────────────────────────────────────────────

    private readonly ConnectHandler _connect;
    private readonly EnterGameHandler _enterGame;
    private readonly DisconnectHandler _disconnect;
    private readonly ChangeTeamHandler _changeTeam;
    private readonly ChangeRoleHandler _changeRole;
    private readonly ChangeNameHandler _changeName;
    private readonly FragHandler _frag;
    private readonly SuicideHandler _suicide;
    private readonly ChatHandler _chat;
    private readonly PlayerActionHandler _playerAction;
    private readonly MapChangeHandler _mapChange;
    private readonly StatsmeHandler _statsme;
    private readonly TeamBonusHandler _teamBonus;
    private readonly IDbContextFactory<HLStatsDbContext> _dbFactory;
    private readonly ILogger<EventRouter> _logger;

    public EventRouter(
        ConnectHandler connect,
        EnterGameHandler enterGame,
        DisconnectHandler disconnect,
        ChangeTeamHandler changeTeam,
        ChangeRoleHandler changeRole,
        ChangeNameHandler changeName,
        FragHandler frag,
        SuicideHandler suicide,
        ChatHandler chat,
        PlayerActionHandler playerAction,
        MapChangeHandler mapChange,
        StatsmeHandler statsme,
        TeamBonusHandler teamBonus,
        IDbContextFactory<HLStatsDbContext> dbFactory,
        ILogger<EventRouter> logger)
    {
        _connect       = connect;
        _enterGame     = enterGame;
        _disconnect    = disconnect;
        _changeTeam    = changeTeam;
        _changeRole    = changeRole;
        _changeName    = changeName;
        _frag          = frag;
        _suicide       = suicide;
        _chat          = chat;
        _playerAction  = playerAction;
        _mapChange     = mapChange;
        _statsme       = statsme;
        _teamBonus     = teamBonus;
        _dbFactory     = dbFactory;
        _logger        = logger;
    }

    // ── Main dispatch ────────────────────────────────────────────────────────

    /// <summary>
    /// Dispatches a parsed log event text to the appropriate handler.
    /// The pattern order mirrors the Perl daemon's <c>elsif</c> chain.
    /// </summary>
    public async Task DispatchAsync(
        string eventText,
        EventContext ctx,
        DaemonOptions options,
        string serverAddr,
        CancellationToken ct = default)
    {
        Match m;

        // 1. Kill (most common hot path — checked first for speed)
        m = KillRegex().Match(eventText);
        if (m.Success)
        {
            await HandleKillAsync(m, ctx, options, serverAddr, ct);
            return;
        }

        // 2. Statsme weaponstats
        m = StatsmeWeaponRegex().Match(eventText);
        if (m.Success)
        {
            await HandleStatsmeAsync(m, ctx, serverAddr, ct);
            return;
        }

        // 3. Statsme latency/time — deferred, skip line
        if (StatsmeLatencyRegex().IsMatch(eventText)) return;

        // 4. CS:GO bracket-coords suicide
        m = BracketSuicideRegex().Match(eventText);
        if (m.Success)
        {
            await HandleBracketSuicideAsync(m, ctx, options, serverAddr, ct);
            return;
        }

        // 5. Player-verb-obj
        m = PlayerVerbObjRegex().Match(eventText);
        if (m.Success)
        {
            await HandlePlayerVerbObjAsync(m, ctx, options, serverAddr, ct);
            return;
        }

        // 6. Player-verb
        m = PlayerVerbRegex().Match(eventText);
        if (m.Success)
        {
            await HandlePlayerVerbAsync(m, ctx, options, serverAddr, ct);
            return;
        }

        // 7. Team triggered
        m = TeamTriggeredRegex().Match(eventText);
        if (m.Success)
        {
            await HandleTeamTriggeredAsync(m, ctx, serverAddr, ct);
            return;
        }

        // 8. Admin plugin lines — deferred, skip
        if (SourcemodRegex().IsMatch(eventText)) return;
        if (ManiAdminRegex().IsMatch(eventText)) return;

        // 9. World triggered / Loading map / Started map
        m = WorldVerbObjRegex().Match(eventText);
        if (m.Success)
        {
            await HandleWorldVerbAsync(m, ctx, ct);
            return;
        }

        _logger.LogDebug("Unmatched event: {EventText}", eventText);
    }

    // ── Private handlers ─────────────────────────────────────────────────────

    private async Task HandleKillAsync(Match m, EventContext ctx, DaemonOptions options, string serverAddr, CancellationToken ct)
    {
        var killerStr = m.Groups[1].Value;
        int? pkX = m.Groups[2].Success && int.TryParse(m.Groups[2].Value, out var kx) ? kx : null;
        int? pkY = m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out var ky) ? ky : null;
        int? pkZ = m.Groups[4].Success && int.TryParse(m.Groups[4].Value, out var kz) ? kz : null;
        var victimStr = m.Groups[5].Value;
        int? pvX = m.Groups[6].Success && int.TryParse(m.Groups[6].Value, out var vx) ? vx : null;
        int? pvY = m.Groups[7].Success && int.TryParse(m.Groups[7].Value, out var vy) ? vy : null;
        int? pvZ = m.Groups[8].Success && int.TryParse(m.Groups[8].Value, out var vz) ? vz : null;
        var weapon = m.Groups[9].Value.ToLowerInvariant();
        var props  = PropertiesParser.Parse(m.Groups[10].Value);
        bool headshot = props.ContainsKey("headshot");

        // Positions from properties (DoD:S, most HL2 games) override CS:GO bracket coords.
        // Perl: checks attacker_position / killerpos (PVKII) then victim_position / victimpos.
        var attackerPos = props.GetValueOrDefault("attacker_position") ?? props.GetValueOrDefault("killerpos");
        if (attackerPos is not null) (pkX, pkY, pkZ) = ParseXYZ(attackerPos);

        var victimPos = props.GetValueOrDefault("victim_position") ?? props.GetValueOrDefault("victimpos");
        if (victimPos is not null) (pvX, pvY, pvZ) = ParseXYZ(victimPos);

        // Fall back to carry-forward coords stored by a prior "killlocation" World trigger.
        // Mirrors Perl's nextkillx / nextkillvicx fallback in doEvent_Frag.
        var server = ctx.Server;
        if (pkX is null && server.NextKillX is not null)
            (pkX, pkY, pkZ) = (server.NextKillX, server.NextKillY, server.NextKillZ);
        if (pvX is null && server.NextKillVicX is not null)
            (pvX, pvY, pvZ) = (server.NextKillVicX, server.NextKillVicY, server.NextKillVicZ);
        // Reset regardless (Perl always resets after a kill, even when coords were null)
        server.NextKillX = server.NextKillY = server.NextKillZ = null;
        server.NextKillVicX = server.NextKillVicY = server.NextKillVicZ = null;

        var killerInfo = PlayerStringParser.Parse(killerStr, serverAddr);
        var victimInfo = PlayerStringParser.Parse(victimStr, serverAddr);
        if (killerInfo is null || victimInfo is null) return;

        var killer = LookupPlayer(killerInfo, ctx.Server);
        var victim = LookupPlayer(victimInfo, ctx.Server);

        // nextkillheadshot carry-forward: "headshot" player action marks next kill by that player as HS.
        // Mirrors Perl's nextkillheadshot check at the top of doEvent_Frag (line ~1138).
        // Always reset after every kill event, even when the kill is otherwise ignored.
        if (!headshot && server.NextKillHeadshot != 0 && killer?.DbPlayerId == server.NextKillHeadshot)
            headshot = true;
        server.NextKillHeadshot = 0;

        if (killer is null || victim is null) return;

        double weaponModifier = await GetWeaponModifierAsync(ctx.Server.Game, weapon, ct);

        await _frag.HandleAsync(
            killer, victim, weapon, headshot,
            pkX, pkY, pkZ, pvX, pvY, pvZ,
            weaponModifier, ctx, options, ct);
    }

    private async Task HandleStatsmeAsync(Match m, EventContext ctx, string serverAddr, CancellationToken ct)
    {
        var playerStr = m.Groups[1].Value;
        var props     = PropertiesParser.Parse(m.Groups[3].Value);

        var playerInfo = PlayerStringParser.Parse(playerStr, serverAddr);
        if (playerInfo is null) return;

        var player = LookupPlayer(playerInfo, ctx.Server);
        if (player is null) return;

        props.TryGetValue("weapon",    out var weaponRaw);
        var weapon = (weaponRaw ?? string.Empty).ToLowerInvariant();
        int shots     = ParseInt(props, "shots");
        int hits      = ParseInt(props, "hits");
        int headshots = ParseInt(props, "headshots");
        int damage    = ParseInt(props, "damage");
        int kills     = ParseInt(props, "kills");
        int deaths    = ParseInt(props, "deaths");

        await _statsme.HandleAsync(player, weapon, shots, hits, headshots, damage, kills, deaths, ctx, ct);
    }

    private async Task HandleBracketSuicideAsync(Match m, EventContext ctx, DaemonOptions options, string serverAddr, CancellationToken ct)
    {
        var verb = m.Groups[5].Value.Trim();
        if (!verb.Equals("committed suicide with", StringComparison.OrdinalIgnoreCase)) return;

        var playerInfo = PlayerStringParser.Parse(m.Groups[1].Value, serverAddr);
        if (playerInfo is null) return;

        var player = LookupPlayer(playerInfo, ctx.Server);
        if (player is null) return;

        int? pkX = m.Groups[2].Success && int.TryParse(m.Groups[2].Value, out var kx) ? kx : null;
        int? pkY = m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out var ky) ? ky : null;
        int? pkZ = m.Groups[4].Success && int.TryParse(m.Groups[4].Value, out var kz) ? kz : null;

        var weapon = m.Groups[6].Value.ToLowerInvariant();
        await _suicide.HandleAsync(player, weapon, pkX, pkY, pkZ, ctx, options, ct);
    }

    private async Task HandlePlayerVerbObjAsync(Match m, EventContext ctx, DaemonOptions options, string serverAddr, CancellationToken ct)
    {
        var playerStr = m.Groups[1].Value;
        var verb      = m.Groups[2].Value.Trim();
        var obj       = m.Groups[3].Value;
        var props     = PropertiesParser.Parse(m.Groups[4].Value);
        var server    = ctx.Server;

        if (verb.Equals("connected, address", StringComparison.OrdinalIgnoreCase))
        {
            var ip = obj.Contains(':') ? obj[..obj.LastIndexOf(':')] : obj;
            var playerInfo = PlayerStringParser.Parse(playerStr, serverAddr);
            if (playerInfo is null) return;
            await _connect.HandleAsync(playerInfo, ip, ctx, options, ct);
        }
        else if (verb.Equals("joined team", StringComparison.OrdinalIgnoreCase))
        {
            var player = ParseAndLookup(playerStr, serverAddr, server);
            if (player is null) return;
            await _changeTeam.HandleAsync(player, obj, ctx, ct);
        }
        else if (verb.Equals("changed role to", StringComparison.OrdinalIgnoreCase))
        {
            var player = ParseAndLookup(playerStr, serverAddr, server);
            if (player is null) return;
            await _changeRole.HandleAsync(player, obj, ctx, ct);
        }
        else if (verb.Equals("changed name to", StringComparison.OrdinalIgnoreCase))
        {
            var player = ParseAndLookup(playerStr, serverAddr, server);
            if (player is null) return;
            await _changeName.HandleAsync(player, obj, ctx, options, ct);
        }
        else if (verb.Equals("triggered", StringComparison.OrdinalIgnoreCase) ||
                 verb.Equals("triggered a", StringComparison.OrdinalIgnoreCase))
        {
            // player_changeclass uses the "newclass" property instead of the action code
            if (obj.Equals("player_changeclass", StringComparison.OrdinalIgnoreCase) &&
                props.TryGetValue("newclass", out var newClass))
            {
                var player = ParseAndLookup(playerStr, serverAddr, server);
                if (player is null) return;
                await _changeRole.HandleAsync(player, newClass, ctx, ct);
            }
            else
            {
                var player = ParseAndLookup(playerStr, serverAddr, server);
                if (player is null) return;
                // Perl: $properties{position} // $properties{attacker_position}
                var posStr = props.GetValueOrDefault("position") ?? props.GetValueOrDefault("attacker_position");
                var (posX, posY, posZ) = posStr is not null ? ParseXYZ(posStr) : ((int?)null, null, null);
                await _playerAction.HandleAsync(player, obj, posX, posY, posZ, ctx, ct);
            }
        }
        else if (verb.Equals("say", StringComparison.OrdinalIgnoreCase) ||
                 verb.Equals("say_team", StringComparison.OrdinalIgnoreCase) ||
                 verb.Equals("say_squad", StringComparison.OrdinalIgnoreCase))
        {
            var player = ParseAndLookup(playerStr, serverAddr, server);
            if (player is null) return;
            await _chat.HandleAsync(player, verb, obj, ctx, ct);
        }
        else if (verb.Equals("committed suicide with", StringComparison.OrdinalIgnoreCase))
        {
            var player = ParseAndLookup(playerStr, serverAddr, server);
            if (player is null) return;
            await _suicide.HandleAsync(player, obj.ToLowerInvariant(), null, null, null, ctx, options, ct);
        }
    }

    private async Task HandlePlayerVerbAsync(Match m, EventContext ctx, DaemonOptions options, string serverAddr, CancellationToken ct)
    {
        var playerStr = m.Groups[1].Value;
        var verb      = m.Groups[2].Value.Trim();
        var server    = ctx.Server;

        if (verb.Equals("entered the game", StringComparison.OrdinalIgnoreCase))
        {
            var player = ParseAndLookup(playerStr, serverAddr, server);
            if (player is null) return;
            await _enterGame.HandleAsync(player, ctx, ct);
        }
        else if (verb.StartsWith("disconnected", StringComparison.OrdinalIgnoreCase) ||
                 verb.StartsWith("was kicked", StringComparison.OrdinalIgnoreCase))
        {
            var player = ParseAndLookup(playerStr, serverAddr, server);
            if (player is null) return;
            await _disconnect.HandleAsync(player, ctx, verb, ct);
        }
        else if (verb.Equals("STEAM USERID validated", StringComparison.OrdinalIgnoreCase) ||
                 verb.Equals("VALVE USERID validated", StringComparison.OrdinalIgnoreCase))
        {
            // CS:GO: "STEAM USERID validated" is the connect trigger
            if (server.Game.Equals("csgo", StringComparison.OrdinalIgnoreCase))
            {
                var playerInfo = PlayerStringParser.Parse(playerStr, serverAddr);
                if (playerInfo is null) return;
                var existingIp = server.FindByUniqueId(playerInfo.UniqueId)?.IpAddress ?? string.Empty;
                await _connect.HandleAsync(playerInfo, existingIp, ctx, options, ct);
            }
        }
    }

    private async Task HandleTeamTriggeredAsync(Match m, EventContext ctx, string serverAddr, CancellationToken ct)
    {
        var team  = m.Groups[1].Value;
        var verb  = m.Groups[2].Value.Trim();
        var obj   = m.Groups[3].Value;
        var props = PropertiesParser.Parse(m.Groups[4].Value);
        var server = ctx.Server;

        // pointcaptured: reward each capper listed in properties (player1..playerN)
        if (obj.Equals("pointcaptured", StringComparison.OrdinalIgnoreCase))
        {
            if (props.TryGetValue("numcappers", out var numStr) && int.TryParse(numStr, out int numCappers))
            {
                for (int i = 1; i <= numCappers; i++)
                {
                    if (!props.TryGetValue($"player{i}", out var capStr)) continue;
                    var capper = ParseAndLookup(capStr, serverAddr, server);
                    if (capper is null) continue;
                    await _playerAction.HandleAsync(capper, obj, null, null, null, ctx, ct);
                }
            }
            return;
        }

        // captured_loc: reward player_a and player_b from properties
        if (obj.Equals("captured_loc", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var key in new[] { "player_a", "player_b" })
            {
                if (!props.TryGetValue(key, out var pStr)) continue;
                var p = ParseAndLookup(pStr, serverAddr, server);
                if (p is null) continue;
                await _playerAction.HandleAsync(p, obj, null, null, null, ctx, ct);
            }
            return;
        }

        // General team action
        if (verb.Equals("triggered", StringComparison.OrdinalIgnoreCase) ||
            verb.Equals("triggered a", StringComparison.OrdinalIgnoreCase))
        {
            await HandleTeamActionAsync(team, obj, ctx, ct);
        }
    }

    private async Task HandleWorldVerbAsync(Match m, EventContext ctx, CancellationToken ct)
    {
        var verb = m.Groups[1].Value.Trim();
        var obj  = m.Groups[2].Value;
        var props = PropertiesParser.Parse(m.Groups[3].Value);

        if (verb.Equals("World triggered", StringComparison.OrdinalIgnoreCase))
        {
            if (obj.Equals("killlocation", StringComparison.OrdinalIgnoreCase))
            {
                // Store positions for the next kill event (DoD:S / most HL2 games send this before the kill line).
                // Mirrors doEvent_Kill_Loc storing nextkillx / nextkillvicx in Perl.
                if (props.TryGetValue("attacker_position", out var ap))
                    (ctx.Server.NextKillX, ctx.Server.NextKillY, ctx.Server.NextKillZ) = ParseXYZ(ap);
                if (props.TryGetValue("victim_position", out var vp))
                    (ctx.Server.NextKillVicX, ctx.Server.NextKillVicY, ctx.Server.NextKillVicZ) = ParseXYZ(vp);
            }
            // Round_Win / Mini_Round_Win: call team action with the winning team
            else if ((obj.Equals("Round_Win", StringComparison.OrdinalIgnoreCase) ||
                      obj.Equals("Mini_Round_Win", StringComparison.OrdinalIgnoreCase)) &&
                     props.TryGetValue("winner", out var winner))
            {
                await HandleTeamActionAsync(winner, obj, ctx, ct);
            }
        }
        else if (verb.Equals("Loading map", StringComparison.OrdinalIgnoreCase))
        {
            await _mapChange.HandleAsync("loading", obj, ctx.Server, ctx.EventUnix, ctx.IsReplay, ct);
        }
        else if (verb.Equals("Started map", StringComparison.OrdinalIgnoreCase))
        {
            await _mapChange.HandleAsync("started", obj, ctx.Server, ctx.EventUnix, ctx.IsReplay, ct);
        }
    }

    // ── Team action helper ───────────────────────────────────────────────────

    private async Task HandleTeamActionAsync(string team, string actionCode, EventContext ctx, CancellationToken ct)
    {
        var server = ctx.Server;
        if (server.PlayerCount < server.Config.MinPlayers) return;

        await using var db = _dbFactory.CreateDbContext();

        var action = await db.Set<GameAction>()
            .Where(a =>
                a.Game == server.Game &&
                (a.Code == actionCode || a.Code == ctx.Map + "_" + actionCode) &&
                a.ForTeamActions)
            .OrderByDescending(a => a.Code.Length)
            .FirstOrDefaultAsync(ct);

        if (action is null) return;

        await db.Database.ExecuteSqlRawAsync(
            "UPDATE hlstats_Actions SET count = count + 1 WHERE game = {0} AND (code = {1} OR code = {2})",
            new object[] { server.Game, actionCode, ctx.Map + "_" + actionCode }, ct);

        if (action.RewardTeam != 0)
        {
            var actionTeam = string.IsNullOrEmpty(action.Team) ? team : action.Team;
            await _teamBonus.RewardTeamAsync(actionTeam, action.RewardTeam, action.ActionId, ctx, actionCode, ct);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static PlayerSession? LookupPlayer(PlayerInfo info, ServerState server)
        => server.LookupPlayer(info.UserId, info.UniqueId)
           ?? server.FindByUniqueId(info.UniqueId);

    private static PlayerSession? ParseAndLookup(string playerStr, string serverAddr, ServerState server)
    {
        var info = PlayerStringParser.Parse(playerStr, serverAddr);
        return info is null ? null : LookupPlayer(info, server);
    }

    private async Task<double> GetWeaponModifierAsync(string game, string weapon, CancellationToken ct)
    {
        await using var db = _dbFactory.CreateDbContext();
        var modifier = await db.Weapons
            .Where(w => w.Game == game && w.Code == weapon)
            .Select(w => (float?)w.Modifier)
            .SingleOrDefaultAsync(ct);
        return modifier ?? 1.0;
    }

    private static int ParseInt(Dictionary<string, string> props, string key)
        => props.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : 0;

    private static (int? x, int? y, int? z) ParseXYZ(string pos)
    {
        var parts = pos.Split(' ');
        if (parts.Length < 3) return (null, null, null);
        return (
            int.TryParse(parts[0], out var x) ? x : null,
            int.TryParse(parts[1], out var y) ? y : null,
            int.TryParse(parts[2], out var z) ? z : null);
    }
}
