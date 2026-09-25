using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Daemon.Events;
using HLStatsX.NET.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HLStatsX.NET.Tests.Daemon.Events;

/// <summary>
/// Tests for <see cref="ClanService"/> covering the Perl <c>getClanId</c> pattern
/// transformation and the full <c>MatchAsync</c> DB path.
/// </summary>
public sealed class ClanServiceTests
{
    // ── TryMatchTag: position = START ────────────────────────────────────────

    [Fact]
    public void TryMatchTag_StartPosition_MatchesTagAtStart()
    {
        var tag = new ClanTag { Pattern = "[TF2]", Position = "START" };
        var (matched, _) = ClanService.TryMatchTag(tag, "[TF2]FragLord");
        matched.Should().Be("[TF2]");
    }

    [Fact]
    public void TryMatchTag_StartPosition_NoMatchWhenTagAtEnd()
    {
        var tag = new ClanTag { Pattern = "[TF2]", Position = "START" };
        var (matched, _) = ClanService.TryMatchTag(tag, "FragLord[TF2]");
        matched.Should().BeNull();
    }

    [Fact]
    public void TryMatchTag_StartPosition_RequiresSuffixAfterTag()
    {
        var tag = new ClanTag { Pattern = "[TF2]", Position = "START" };
        var (matched, _) = ClanService.TryMatchTag(tag, "[TF2]");
        matched.Should().BeNull("regex requires .+ after the tag");
    }

    // ── TryMatchTag: position = END ──────────────────────────────────────────

    [Fact]
    public void TryMatchTag_EndPosition_MatchesTagAtEnd()
    {
        var tag = new ClanTag { Pattern = "[TF2]", Position = "END" };
        var (matched, _) = ClanService.TryMatchTag(tag, "FragLord[TF2]");
        matched.Should().Be("[TF2]");
    }

    [Fact]
    public void TryMatchTag_EndPosition_NoMatchWhenTagAtStart()
    {
        var tag = new ClanTag { Pattern = "[TF2]", Position = "END" };
        var (matched, _) = ClanService.TryMatchTag(tag, "[TF2]FragLord");
        matched.Should().BeNull();
    }

    [Fact]
    public void TryMatchTag_EndPosition_RequiresPrefixBeforeTag()
    {
        var tag = new ClanTag { Pattern = "[TF2]", Position = "END" };
        var (matched, _) = ClanService.TryMatchTag(tag, "[TF2]");
        matched.Should().BeNull("regex requires .+ before the tag");
    }

    // ── TryMatchTag: position = EITHER ───────────────────────────────────────

    [Fact]
    public void TryMatchTag_EitherPosition_MatchesAtStart()
    {
        var tag = new ClanTag { Pattern = "=TB=", Position = "EITHER" };
        var (matched, _) = ClanService.TryMatchTag(tag, "=TB=FragLord");
        matched.Should().Be("=TB=");
    }

    [Fact]
    public void TryMatchTag_EitherPosition_MatchesAtEnd()
    {
        var tag = new ClanTag { Pattern = "=TB=", Position = "EITHER" };
        var (matched, _) = ClanService.TryMatchTag(tag, "FragLord=TB=");
        matched.Should().Be("=TB=");
    }

    // ── TryMatchTag: clan name capture ───────────────────────────────────────

    [Fact]
    public void TryMatchTag_BracketTag_CapturesClanNameAsInnerWord()
    {
        // "[TF2]" → escaped → "\[TF2\]" → wrap first alnum "TF2" → "\[(TF2)\]"
        // group 1 = full tag "[TF2]", group 2 = "TF2"
        var tag = new ClanTag { Pattern = "[TF2]", Position = "START" };
        var (_, clanName) = ClanService.TryMatchTag(tag, "[TF2]SomeName");
        clanName.Should().Be("TF2");
    }

    [Fact]
    public void TryMatchTag_EqualsTag_CapturesClanNameAsInnerWord()
    {
        // "=TB=" → first alnum "TB" → "=(TB)="
        var tag = new ClanTag { Pattern = "=TB=", Position = "START" };
        var (_, clanName) = ClanService.TryMatchTag(tag, "=TB=SomeName");
        clanName.Should().Be("TB");
    }

    [Fact]
    public void TryMatchTag_PlainAlphanumericTag_CapturesSelf()
    {
        // "TEAM" → first alnum "(TEAM)" → both groups equal "TEAM"
        var tag = new ClanTag { Pattern = "TEAM", Position = "START" };
        var (matched, clanName) = ClanService.TryMatchTag(tag, "TEAMSomeName");
        matched.Should().Be("TEAM");
        clanName.Should().Be("TEAM");
    }

    // ── TryMatchTag: no match ────────────────────────────────────────────────

    [Fact]
    public void TryMatchTag_PatternNotInName_ReturnsNull()
    {
        var tag = new ClanTag { Pattern = "[TF2]", Position = "EITHER" };
        var (matched, clanName) = ClanService.TryMatchTag(tag, "UnaffiliatedPlayer");
        matched.Should().BeNull();
        clanName.Should().BeNull();
    }

    // ── TryMatchTag: wildcard A = any single char ────────────────────────────

    [Fact]
    public void TryMatchTag_AWildcard_MatchesAnyCharInPosition()
    {
        var tag = new ClanTag { Pattern = "ATeam", Position = "START" };

        ClanService.TryMatchTag(tag, "ATeamPlayer").tag.Should().NotBeNull("literal A matches via dot");
        ClanService.TryMatchTag(tag, "BTeamPlayer").tag.Should().NotBeNull("B matches via dot wildcard");
        ClanService.TryMatchTag(tag, "ZTeamPlayer").tag.Should().NotBeNull("any char matches");
    }

    [Fact]
    public void TryMatchTag_AWildcard_RequiresExactlyOneChar()
    {
        var tag = new ClanTag { Pattern = "ATeam", Position = "START" };
        ClanService.TryMatchTag(tag, "TeamPlayer").tag.Should().BeNull("dot requires one char, not zero");
    }

    // ── TryMatchTag: wildcard X = optional char ──────────────────────────────

    [Fact]
    public void TryMatchTag_XWildcard_MatchesZeroOrOneChar()
    {
        var tag = new ClanTag { Pattern = "XTeam", Position = "START" };

        ClanService.TryMatchTag(tag, "TeamPlayer").tag.Should().NotBeNull("X is optional — zero chars OK");
        ClanService.TryMatchTag(tag, "ATeamPlayer").tag.Should().NotBeNull("one char also OK");
    }

    // ── TryMatchTag: case insensitivity ──────────────────────────────────────

    [Fact]
    public void TryMatchTag_IsCaseInsensitive()
    {
        var tag = new ClanTag { Pattern = "[TF2]", Position = "START" };

        ClanService.TryMatchTag(tag, "[tf2]FragLord").tag.Should().Be("[tf2]");
        ClanService.TryMatchTag(tag, "[TF2]FragLord").tag.Should().Be("[TF2]");
    }

    // ── TryMatchTag: special regex characters are escaped ────────────────────

    [Fact]
    public void TryMatchTag_ParenthesesInTag_AreEscaped()
    {
        var tag = new ClanTag { Pattern = "(Elite)", Position = "START" };
        var (matched, _) = ClanService.TryMatchTag(tag, "(Elite)SomeName");
        matched.Should().Be("(Elite)");
    }

    [Fact]
    public void TryMatchTag_PipeAndPlusInTag_AreEscaped()
    {
        var tag = new ClanTag { Pattern = "TF|2+", Position = "START" };

        ClanService.TryMatchTag(tag, "TF|2+SomeName").tag.Should().NotBeNull();
        ClanService.TryMatchTag(tag, "TF2SomeName").tag.Should().BeNull("unescaped would falsely match");
    }

    [Fact]
    public void TryMatchTag_DotInTag_IsEscaped()
    {
        var tag = new ClanTag { Pattern = "x.y", Position = "START" };

        ClanService.TryMatchTag(tag, "x.ySomeName").tag.Should().NotBeNull("literal dot matches");
        ClanService.TryMatchTag(tag, "xAySomeName").tag.Should().BeNull("unescaped dot would match any char; escaped dot requires literal dot");
    }

    // ── MatchAsync: full DB path ──────────────────────────────────────────────

    private static TestDbContextFactory BuildDb(Action<HLStatsDbContext>? seed = null)
    {
        var opts = new DbContextOptionsBuilder<HLStatsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var factory = new TestDbContextFactory(opts);
        if (seed is not null)
        {
            using var ctx = factory.CreateDbContext();
            seed(ctx);
            ctx.SaveChanges();
        }
        return factory;
    }

    [Fact]
    public async Task MatchAsync_NoTags_ReturnsNull()
    {
        var factory = BuildDb();
        var svc = new ClanService(factory, NullLogger<ClanService>.Instance);

        var result = await svc.MatchAsync("FragLord", "dods");

        result.Should().BeNull();
    }

    [Fact]
    public async Task MatchAsync_NoTagMatches_ReturnsNull()
    {
        var factory = BuildDb(ctx =>
            ctx.ClanTags.Add(new ClanTag { Id = 1, Pattern = "[TF2]", Position = "START" }));

        var svc = new ClanService(factory, NullLogger<ClanService>.Instance);

        var result = await svc.MatchAsync("UnaffiliatedPlayer", "dods");

        result.Should().BeNull();
    }

    [Fact]
    public async Task MatchAsync_TagMatchesExistingClan_ReturnsClanId()
    {
        var factory = BuildDb(ctx =>
        {
            ctx.ClanTags.Add(new ClanTag { Id = 1, Pattern = "[TF2]", Position = "START" });
            ctx.Clans.Add(new Clan { ClanId = 42, Tag = "[TF2]", Game = "dods" });
        });

        var svc = new ClanService(factory, NullLogger<ClanService>.Instance);

        var result = await svc.MatchAsync("[TF2]FragLord", "dods");

        result.Should().Be(42);
    }

    [Fact]
    public async Task MatchAsync_TagMatchesNewClan_CreatesClanAndReturnsId()
    {
        var factory = BuildDb(ctx =>
            ctx.ClanTags.Add(new ClanTag { Id = 1, Pattern = "[TF2]", Position = "START" }));

        var svc = new ClanService(factory, NullLogger<ClanService>.Instance);

        var result = await svc.MatchAsync("[TF2]FragLord", "dods");

        result.Should().NotBeNull();

        await using var ctx = factory.CreateDbContext();
        var clan = ctx.Clans.Single();
        clan.Tag.Should().Be("[TF2]");
        clan.Name.Should().Be("TF2", "inner word extracted from bracket tag");
        clan.Game.Should().Be("dods");
    }

    [Fact]
    public async Task MatchAsync_ClanExistsInDifferentGame_CreatesNewClanForThisGame()
    {
        var factory = BuildDb(ctx =>
        {
            ctx.ClanTags.Add(new ClanTag { Id = 1, Pattern = "[TF2]", Position = "START" });
            ctx.Clans.Add(new Clan { ClanId = 99, Tag = "[TF2]", Game = "tf" });
        });

        var svc = new ClanService(factory, NullLogger<ClanService>.Instance);

        var result = await svc.MatchAsync("[TF2]FragLord", "dods");

        result.Should().NotBeNull().And.NotBe(99);

        await using var ctx = factory.CreateDbContext();
        ctx.Clans.Count().Should().Be(2, "new clan created for dods game");
    }

    [Fact]
    public async Task MatchAsync_LongerTagTriedFirst_MatchesLongerPattern()
    {
        // The service sorts tags longest-first, so "[ELITE]" should win over "[EL]"
        // even though both would match the player name.
        var factory = BuildDb(ctx =>
        {
            ctx.ClanTags.Add(new ClanTag { Id = 1, Pattern = "[EL]",    Position = "START" });
            ctx.ClanTags.Add(new ClanTag { Id = 2, Pattern = "[ELITE]", Position = "START" });
            ctx.Clans.Add(new Clan { ClanId = 10, Tag = "[EL]",    Game = "dods" });
            ctx.Clans.Add(new Clan { ClanId = 20, Tag = "[ELITE]", Game = "dods" });
        });

        var svc = new ClanService(factory, NullLogger<ClanService>.Instance);

        var result = await svc.MatchAsync("[ELITE]Player", "dods");

        result.Should().Be(20, "longer pattern wins");
    }
}
