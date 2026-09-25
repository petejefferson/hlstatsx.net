using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Repositories;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using HLStatsX.NET.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HLStatsX.NET.Tests.Controllers;

public class InGameControllerTests
{
    private readonly Mock<IPlayerService>    _playersMock  = new();
    private readonly Mock<IClanService>      _clansMock    = new();
    private readonly Mock<IServerService>    _serversMock  = new();
    private readonly Mock<IActionRepository> _actionsMock  = new();
    private readonly Mock<IWeaponRepository> _weaponsMock  = new();
    private readonly Mock<IMapRepository>    _mapsMock     = new();
    private readonly IConfiguration          _config;
    private readonly InGameController        _controller;

    private static readonly RealStats DefaultRealStats = new(100, 80, 40, 5, 1.25, 0.4);
    private static readonly GameStats  DefaultGameStats = new(5000, 2000, 3, -1, -1);

    public InGameControllerTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"] = "cstrike"
            })
            .Build();

        _controller = new InGameController(
            _playersMock.Object, _clansMock.Object, _serversMock.Object,
            _actionsMock.Object, _weaponsMock.Object, _mapsMock.Object,
            _config);
    }

    // ── Motd ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Motd_ReturnsViewWithTopPlayersAndClansAndServers()
    {
        var servers = new List<Server> { new() { ServerId = 1, Name = "FragServer", Game = "cstrike" } };
        var players = PagedResult<Player>.Create([new Player { PlayerId = 1, LastName = "FragLord" }], 1, 1, 10);
        var clans   = PagedResult<ClanLeaderboardRow>.Create([new ClanLeaderboardRow { ClanId = 1, Name = "FragClan", MemberCount = 5 }], 1, 1, 3);

        _serversMock.Setup(s => s.GetServersAsync("cstrike", default)).ReturnsAsync(servers);
        _playersMock.Setup(s => s.GetLeaderboardAsync("cstrike", 1, 10, "skill", true, 1, default)).ReturnsAsync(players);
        _clansMock.Setup(s => s.GetLeaderboardAsync("cstrike", 1, 3, "skill", true, 1, default)).ReturnsAsync(clans);

        var result = await _controller.Motd(null);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<InGameMotdViewModel>().Subject;
        model.Game.Should().Be("cstrike");
        model.TopPlayers.Should().HaveCount(1);
        model.TopClans.Should().HaveCount(1);
        model.Servers.Should().HaveCount(1);
    }

    [Fact]
    public async Task Motd_SkipsPlayerQuery_WhenPlayersParamIsZero()
    {
        _serversMock.Setup(s => s.GetServersAsync("cstrike", default)).ReturnsAsync([]);
        _clansMock.Setup(s => s.GetLeaderboardAsync("cstrike", 1, 3, "skill", true, 1, default))
                  .ReturnsAsync(PagedResult<ClanLeaderboardRow>.Create([], 0, 1, 3));

        await _controller.Motd(null, players: 0, clans: 3);

        _playersMock.Verify(s => s.GetLeaderboardAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Motd_ClampsPlayersParam_ToMax100()
    {
        _serversMock.Setup(s => s.GetServersAsync("cstrike", default)).ReturnsAsync([]);
        _playersMock.Setup(s => s.GetLeaderboardAsync("cstrike", 1, 100, "skill", true, 1, default))
                    .ReturnsAsync(PagedResult<Player>.Create([], 0, 1, 100));
        _clansMock.Setup(s => s.GetLeaderboardAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(PagedResult<ClanLeaderboardRow>.Create([], 0, 1, 3));

        await _controller.Motd(null, players: 9999, clans: 0);

        _playersMock.Verify(s => s.GetLeaderboardAsync("cstrike", 1, 100, "skill", true, 1, default), Times.Once);
    }

    // ── Players ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Players_ReturnsViewWithLeaderboard()
    {
        var paged = PagedResult<Player>.Create([new Player { PlayerId = 5 }], 1, 1, 25);
        _playersMock.Setup(s => s.GetLeaderboardAsync("cstrike", 1, 25, "skill", true, 1, default)).ReturnsAsync(paged);

        var result = await _controller.Players(null);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<InGamePlayerListViewModel>().Subject;
        model.Game.Should().Be("cstrike");
        model.Players.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Players_UsesExplicitGame_WhenProvided()
    {
        _playersMock.Setup(s => s.GetLeaderboardAsync("dods", 1, 25, "skill", true, 1, default))
                    .ReturnsAsync(PagedResult<Player>.Create([], 0, 1, 25));

        var result = await _controller.Players("dods");

        result.Should().BeOfType<ViewResult>().Which.Model
              .Should().BeOfType<InGamePlayerListViewModel>()
              .Which.Game.Should().Be("dods");
    }

    // ── Clans ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Clans_ReturnsViewWithClanLeaderboard()
    {
        var paged = PagedResult<ClanLeaderboardRow>.Create([new ClanLeaderboardRow { ClanId = 1, Name = "FragClan", MemberCount = 5 }], 1, 1, 25);
        _clansMock.Setup(s => s.GetLeaderboardAsync("cstrike", 1, 25, "skill", true, 3, default)).ReturnsAsync(paged);

        var result = await _controller.Clans(null);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<InGameClanListViewModel>().Subject;
        model.Clans.Items.Should().HaveCount(1);
        model.MinMembers.Should().Be(3);
    }

    // ── ClanInfo ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClanInfo_ReturnsNotFound_WhenClanDoesNotExist()
    {
        _clansMock.Setup(s => s.GetClanAsync(99, default)).ReturnsAsync((Clan?)null);

        var result = await _controller.ClanInfo(null, 99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ClanInfo_ReturnsView_WithClanAndMembers()
    {
        var clan    = new Clan { ClanId = 1, Tag = "FRAG", Name = "FragClan", Game = "cstrike" };
        var summary = new ClanSummaryStats(5000, 4000, 2000, 100000, 5, 8, 1500, 0.8);
        var members = PagedResult<ClanMemberRow>.Create([], 0, 1, 20);

        _clansMock.Setup(s => s.GetClanAsync(1, default)).ReturnsAsync(clan);
        _clansMock.Setup(s => s.GetSummaryAsync(1, default)).ReturnsAsync(summary);
        _clansMock.Setup(s => s.GetMembersPagedAsync(1, "cstrike", 1, 20, "skill", true, 5000, default)).ReturnsAsync(members);

        var result = await _controller.ClanInfo(null, 1);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<InGameClanInfoViewModel>().Subject;
        model.Clan.Name.Should().Be("FragClan");
        model.Summary.TotalKills.Should().Be(5000);
    }

    // ── PlayerStats ───────────────────────────────────────────────────────────

    [Fact]
    public async Task PlayerStats_ReturnsBadRequest_WhenNoPlayerIdOrUniqueId()
    {
        var result = await _controller.PlayerStats(null, null, null);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PlayerStats_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        _playersMock.Setup(s => s.GetPlayerAsync(99, default)).ReturnsAsync((Player?)null);

        var result = await _controller.PlayerStats(null, 99, null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PlayerStats_ReturnsView_WithPlayerStatsAndAccuracy()
    {
        var player  = new Player { PlayerId = 1, LastName = "FragLord", Game = "cstrike" };
        var statsme = new List<WeaponStatsmeRow>
        {
            new("rifle",  "Rifle",  null, 200, 80, 0, 0, 0, 0, 0, 0, 0, 0),
            new("pistol", "Pistol", null, 100, 20, 0, 0, 0, 0, 0, 0, 0, 0)
        };

        _playersMock.Setup(s => s.GetPlayerAsync(1, default)).ReturnsAsync(player);
        _playersMock.Setup(s => s.GetPlayerRankAsync(1, "cstrike", default)).ReturnsAsync(5);
        _playersMock.Setup(s => s.GetRealStatsAsync(1, default)).ReturnsAsync(DefaultRealStats);
        _playersMock.Setup(s => s.GetWeaponStatsmeAsync(1, "cstrike", default)).ReturnsAsync(statsme);

        var result = await _controller.PlayerStats(null, 1, null);

        var view  = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<InGamePlayerStatsViewModel>().Subject;
        model.Player.LastName.Should().Be("FragLord");
        model.Rank.Should().Be(5);
        model.StatsmeShots.Should().Be(300);
        model.StatsmeHits.Should().Be(100);
        model.StatsmeAccuracy.Should().Be(33.3);
    }

    // ── ResolvePlayerIdAsync (via PlayerStats) ────────────────────────────────

    [Fact]
    public async Task PlayerStats_ResolvesPlayerId_FromUniqueId()
    {
        _playersMock.Setup(s => s.GetPlayerIdByUniqueIdAsync("STEAM_0:1:12345", "cstrike", default))
                    .ReturnsAsync(7);
        _playersMock.Setup(s => s.GetPlayerAsync(7, default))
                    .ReturnsAsync(new Player { PlayerId = 7, Game = "cstrike" });
        _playersMock.Setup(s => s.GetPlayerRankAsync(7, "cstrike", default)).ReturnsAsync(10);
        _playersMock.Setup(s => s.GetRealStatsAsync(7, default)).ReturnsAsync(DefaultRealStats);
        _playersMock.Setup(s => s.GetWeaponStatsmeAsync(7, "cstrike", default)).ReturnsAsync([]);

        var result = await _controller.PlayerStats(null, null, "STEAM_0:1:12345");

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task PlayerStats_StripsSteamPrefix_WhenExactMatchFails()
    {
        // Exact match for STEAM_1:1:12345 returns null; stripped "1:12345" matches player 8
        _playersMock.Setup(s => s.GetPlayerIdByUniqueIdAsync("STEAM_1:1:12345", "cstrike", default))
                    .ReturnsAsync((int?)null);
        _playersMock.Setup(s => s.GetPlayerIdByUniqueIdAsync("1:12345", "cstrike", default))
                    .ReturnsAsync(8);
        _playersMock.Setup(s => s.GetPlayerAsync(8, default))
                    .ReturnsAsync(new Player { PlayerId = 8, Game = "cstrike" });
        _playersMock.Setup(s => s.GetPlayerRankAsync(8, "cstrike", default)).ReturnsAsync(3);
        _playersMock.Setup(s => s.GetRealStatsAsync(8, default)).ReturnsAsync(DefaultRealStats);
        _playersMock.Setup(s => s.GetWeaponStatsmeAsync(8, "cstrike", default)).ReturnsAsync([]);

        var result = await _controller.PlayerStats(null, null, "STEAM_1:1:12345");

        result.Should().BeOfType<ViewResult>();
        _playersMock.Verify(s => s.GetPlayerIdByUniqueIdAsync("1:12345", "cstrike", default), Times.Once);
    }

    // ── Kills ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Kills_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        _playersMock.Setup(s => s.GetPlayerAsync(99, default)).ReturnsAsync((Player?)null);

        var result = await _controller.Kills(null, 99, null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Kills_ReturnsView_WithKillStats()
    {
        var player = new Player { PlayerId = 2, LastName = "Sniper", Game = "cstrike" };
        _playersMock.Setup(s => s.GetPlayerAsync(2, default)).ReturnsAsync(player);
        _playersMock.Setup(s => s.GetKillStatsAsync(2, 5, default)).ReturnsAsync([]);
        _playersMock.Setup(s => s.GetRealStatsAsync(2, default)).ReturnsAsync(DefaultRealStats);

        var result = await _controller.Kills(null, 2, null);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameKillsViewModel>().Subject;
        model.Player.LastName.Should().Be("Sniper");
        model.TotalRealHeadshots.Should().Be(40);
    }

    // ── Servers ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Servers_ReturnsViewWithServerList()
    {
        var servers = new List<Server>
        {
            new() { ServerId = 1, Name = "Server A", Game = "cstrike" },
            new() { ServerId = 2, Name = "Server B", Game = "cstrike" }
        };
        _serversMock.Setup(s => s.GetServersAsync("cstrike", default)).ReturnsAsync(servers);

        var result = await _controller.Servers(null);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameServersViewModel>().Subject;
        model.Servers.Should().HaveCount(2);
    }

    // ── Status ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Status_ReturnsNotFound_WhenNoServersExistAndNoIdGiven()
    {
        _serversMock.Setup(s => s.GetServersAsync("cstrike", default)).ReturnsAsync([]);

        var result = await _controller.Status(null, null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Status_ReturnsNotFound_WhenServerIdDoesNotExist()
    {
        _serversMock.Setup(s => s.GetServerAsync(99, default)).ReturnsAsync((Server?)null);

        var result = await _controller.Status(null, 99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Status_ReturnsView_WithServerAndLivePlayers()
    {
        var server = new Server { ServerId = 1, Name = "FragServer", Game = "cstrike" };
        var live   = new List<Livestat> { new() { PlayerId = 5, Name = "FragLord" } };

        _serversMock.Setup(s => s.GetServerAsync(1, default)).ReturnsAsync(server);
        _serversMock.Setup(s => s.GetLivestatsAsync(1, default)).ReturnsAsync(live);
        _serversMock.Setup(s => s.GetGameStatsAsync("cstrike", default)).ReturnsAsync(DefaultGameStats);
        _playersMock.Setup(s => s.GetTotalCountAsync("cstrike", default)).ReturnsAsync(200);

        var result = await _controller.Status(null, 1);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameStatusViewModel>().Subject;
        model.Server.Name.Should().Be("FragServer");
        model.LivePlayers.Should().HaveCount(1);
    }

    [Fact]
    public async Task Status_FallsBackToFirstServer_WhenNoIdGiven()
    {
        var server = new Server { ServerId = 3, Name = "AutoServer", Game = "cstrike" };
        _serversMock.Setup(s => s.GetServersAsync("cstrike", default)).ReturnsAsync([server]);
        _serversMock.Setup(s => s.GetLivestatsAsync(3, default)).ReturnsAsync([]);
        _serversMock.Setup(s => s.GetGameStatsAsync("cstrike", default)).ReturnsAsync(DefaultGameStats);
        _playersMock.Setup(s => s.GetTotalCountAsync("cstrike", default)).ReturnsAsync(0);

        var result = await _controller.Status(null, null);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameStatusViewModel>().Subject;
        model.Server.Name.Should().Be("AutoServer");
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Load_ReturnsViewWithGamewideTotals()
    {
        var gs = new GameStats(12345, 4000, 5, -1, -1);
        _serversMock.Setup(s => s.GetGameStatsAsync("cstrike", default)).ReturnsAsync(gs);
        _playersMock.Setup(s => s.GetTotalCountAsync("cstrike", default)).ReturnsAsync(500);

        var result = await _controller.Load(null, null);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameLoadViewModel>().Subject;
        model.TotalPlayers.Should().Be(500);
        model.TotalKills.Should().Be(12345);
        model.TotalServers.Should().Be(5);
        model.ServerId.Should().Be(0);
    }

    [Fact]
    public async Task Load_PassesServerId_WhenProvided()
    {
        _serversMock.Setup(s => s.GetGameStatsAsync("cstrike", default)).ReturnsAsync(DefaultGameStats);
        _playersMock.Setup(s => s.GetTotalCountAsync("cstrike", default)).ReturnsAsync(0);

        var result = await _controller.Load(null, 7);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameLoadViewModel>().Subject;
        model.ServerId.Should().Be(7);
    }

    // ── Bans ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Bans_ReturnsViewWithBannedPlayers()
    {
        var paged = PagedResult<BanListRow>.Create(
            [new(1, "Cheater", null, new DateTime(2025, 1, 1), 1000, 50, 10, 20, 5, 1.25, 0.5, 0.4)],
            1, 1, 25);
        _playersMock.Setup(s => s.GetBannedPlayersAsync("cstrike", 1, 25, "last_event", true, 0, default))
                    .ReturnsAsync(paged);

        var result = await _controller.Bans(null);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameBansViewModel>().Subject;
        model.Bans.Items.Should().HaveCount(1);
        model.Bans.Items[0].PlayerName.Should().Be("Cheater");
    }

    // ── Help ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Help_ReturnsViewWithServerList()
    {
        _serversMock.Setup(s => s.GetServersAsync("cstrike", default))
                    .ReturnsAsync([new Server { ServerId = 1, Name = "S1", Game = "cstrike" }]);

        var result = await _controller.Help(null, null);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameHelpViewModel>().Subject;
        model.Servers.Should().HaveCount(1);
    }

    // ── WeaponInfo ────────────────────────────────────────────────────────────

    [Fact]
    public async Task WeaponInfo_ReturnsBadRequest_WhenWeaponCodeMissing()
    {
        var result = await _controller.WeaponInfo(null, null);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task WeaponInfo_ReturnsView_WithKillerList()
    {
        var wep     = new Weapon { Code = "knife", Name = "Knife", Game = "cstrike" };
        var killers = PagedResult<WeaponKillerRow>.Create([new(1, "FragLord", null, 50, 5)], 1, 1, 25);

        _weaponsMock.Setup(w => w.GetByCodeAsync("knife", "cstrike", default)).ReturnsAsync(wep);
        _weaponsMock.Setup(w => w.GetWeaponKillersAsync("knife", "cstrike", 1, 25, "frags", true, default)).ReturnsAsync(killers);

        var result = await _controller.WeaponInfo(null, "knife");

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameWeaponInfoViewModel>().Subject;
        model.WeaponName.Should().Be("Knife");
        model.Killers.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task WeaponInfo_FallsBackToCapitalisedCode_WhenWeaponNotInDb()
    {
        _weaponsMock.Setup(w => w.GetByCodeAsync("rifle", "cstrike", default)).ReturnsAsync((Weapon?)null);
        _weaponsMock.Setup(w => w.GetWeaponKillersAsync("rifle", "cstrike", 1, 25, "frags", true, default))
                    .ReturnsAsync(PagedResult<WeaponKillerRow>.Create([], 0, 1, 25));

        var result = await _controller.WeaponInfo(null, "rifle");

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameWeaponInfoViewModel>().Subject;
        model.WeaponName.Should().Be("Rifle");
    }

    // ── MapInfo ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task MapInfo_ReturnsBadRequest_WhenMapMissing()
    {
        var result = await _controller.MapInfo(null, null);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task MapInfo_ReturnsView_WithPlayerLeaderboard()
    {
        var players = PagedResult<MapPlayerRow>.Create([new(1, "FragLord", null, 200, 80, 0.4)], 1, 1, 50);
        _mapsMock.Setup(m => m.GetPlayerLeaderboardAsync("de_dust2", "cstrike", 1, 50, "kills", true, default))
                 .ReturnsAsync(players);

        var result = await _controller.MapInfo(null, "de_dust2");

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameMapInfoViewModel>().Subject;
        model.MapName.Should().Be("de_dust2");
        model.Players.Items.Should().HaveCount(1);
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Actions_ReturnsViewWithActionList()
    {
        var actions = new List<ActionListRow> { new("knife_kill", "Knife Kill", 250, 3) };
        _actionsMock.Setup(r => r.GetListAsync("cstrike", "count", true, default)).ReturnsAsync(actions);

        var result = await _controller.Actions(null);

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameActionsViewModel>().Subject;
        model.Actions.Should().HaveCount(1);
        model.SortBy.Should().Be("count");
    }

    // ── ActionInfo ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ActionInfo_ReturnsBadRequest_WhenActionCodeMissing()
    {
        var result = await _controller.ActionInfo(null, null);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ActionInfo_ReturnsView_WithAchievers()
    {
        var action   = new GameAction { ActionId = 1, Code = "knife_kill", Description = "Knife Kill", Game = "cstrike", ForPlayerPlayerActions = false };
        var achievers = PagedResult<ActionAchieverRow>.Create([new(1, "FragLord", null, 50, 150)], 1, 1, 50);

        _actionsMock.Setup(r => r.GetByCodeAsync("knife_kill", "cstrike", default)).ReturnsAsync(action);
        _actionsMock.Setup(r => r.GetAchieversAsync("knife_kill", "cstrike", false, 1, 50, "count", true, default)).ReturnsAsync(achievers);

        var result = await _controller.ActionInfo(null, "knife_kill");

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameActionInfoViewModel>().Subject;
        model.ActionDescription.Should().Be("Knife Kill");
        model.Achievers.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ActionInfo_FallsBackToCapitalisedCode_WhenActionNotInDb()
    {
        _actionsMock.Setup(r => r.GetByCodeAsync("headshot", "cstrike", default)).ReturnsAsync((GameAction?)null);
        _actionsMock.Setup(r => r.GetAchieversAsync("headshot", "cstrike", false, 1, 50, "count", true, default))
                    .ReturnsAsync(PagedResult<ActionAchieverRow>.Create([], 0, 1, 50));

        var result = await _controller.ActionInfo(null, "headshot");

        var model = result.Should().BeOfType<ViewResult>().Subject
                          .Model.Should().BeOfType<InGameActionInfoViewModel>().Subject;
        model.ActionDescription.Should().Be("Headshot");
    }

    // ── Default game ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Servers_UsesDefaultGame_WhenGameParamIsNull()
    {
        _serversMock.Setup(s => s.GetServersAsync("cstrike", default)).ReturnsAsync([]);

        await _controller.Servers(null);

        _serversMock.Verify(s => s.GetServersAsync("cstrike", default), Times.Once);
    }
}
