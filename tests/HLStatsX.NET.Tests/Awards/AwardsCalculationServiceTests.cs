using HLStatsX.NET.Awards.Services;
using HLStatsX.NET.Core.Entities;

namespace HLStatsX.NET.Tests.Awards;

// Tests for AwardsCalculationService.BuildQueries — the method that routes each
// (AwardType, Code) combination to the right SQL template, replicating the
// if/elsif chain in hlstats-awards.pl DoAwards().
public class AwardsCalculationServiceTests
{
    private static readonly DateTime BaseDate   = new(2024, 1, 15);
    private const int NumDays = 1;

    private static (string DailySql, string GlobalSql) GetSql(string awardType, string code)
    {
        var award = new Award { AwardId = 1, AwardType = awardType, Game = "dods", Code = code };
        var (daily, _, global, _) = AwardsCalculationService.BuildQueries(award, BaseDate, NumDays);
        return (daily, global);
    }

    // ── Special code routing ──────────────────────────────────────────────────

    [Theory]
    [InlineData("latency")]
    public void BuildQueries_LatencyCode_UsesLatencyTable(string code)
    {
        var (daily, global) = GetSql("W", code);  // awardType ignored for special codes
        daily.Should().Contain("hlstats_Events_Latency");
        global.Should().Contain("hlstats_Events_Latency");
    }

    [Theory]
    [InlineData("mostkills",    "kills")]
    [InlineData("suicide",      "suicides")]
    [InlineData("teamkills",    "teamkills")]
    [InlineData("connectiontime","connection_time")]
    [InlineData("killstreak",   "kill_streak")]
    [InlineData("deathstreak",  "death_streak")]
    public void BuildQueries_HistoryCode_UsesPlayerHistoryForDaily_PlayersTableForGlobal(string code, string field)
    {
        var (daily, global) = GetSql("W", code);

        daily.Should().Contain("hlstats_Players_History",
            because: $"daily {code} winner comes from history snapshot");
        daily.Should().Contain(field,
            because: $"the {code} award queries the {field} column");

        global.Should().Contain("hlstats_Players",
            because: $"global {code} winner is the all-time best from hlstats_Players");
        global.Should().Contain(field);
    }

    [Fact]
    public void BuildQueries_BonuspointsCode_UsesUnionOfActionTables()
    {
        var (daily, global) = GetSql("O", "bonuspoints");
        daily.Should().Contain("hlstats_Events_PlayerActions");
        daily.Should().Contain("hlstats_Events_PlayerPlayerActions");
        daily.Should().Contain("UNION ALL");
        global.Should().Contain("UNION ALL");
    }

    [Fact]
    public void BuildQueries_AllsentrykillsCode_UsesFragsWithSentryGunFilter()
    {
        var (daily, global) = GetSql("W", "allsentrykills");
        daily.Should().Contain("hlstats_Events_Frags");
        daily.Should().Contain("obj_sentrygun%");
        global.Should().Contain("obj_sentrygun%");
    }

    // ── Generic code routing by AwardType ─────────────────────────────────────

    [Fact]
    public void BuildQueries_TypeO_UsesPlayerActionsTable()
    {
        var (daily, global) = GetSql("O", "somecustomaction");
        daily.Should().Contain("hlstats_Events_PlayerActions");
        daily.Should().Contain("hlstats_Actions");
        global.Should().Contain("hlstats_Events_PlayerActions");
    }

    [Fact]
    public void BuildQueries_TypeW_UsesFragsTableWithWeaponFilter()
    {
        var (daily, global) = GetSql("W", "deagle");
        daily.Should().Contain("hlstats_Events_Frags");
        global.Should().Contain("hlstats_Events_Frags");
    }

    [Fact]
    public void BuildQueries_TypeP_UsesPlayerPlayerActionsWithPlayerField()
    {
        var (daily, _) = GetSql("P", "someaction");
        daily.Should().Contain("hlstats_Events_PlayerPlayerActions");
        // P type uses playerId (not victimId)
        daily.Should().Contain("playerId");
    }

    [Fact]
    public void BuildQueries_TypeV_UsesPlayerPlayerActionsWithVictimField()
    {
        var (daily, _) = GetSql("V", "someaction");
        daily.Should().Contain("hlstats_Events_PlayerPlayerActions");
        // V type uses victimId
        daily.Should().Contain("victimId");
    }

    // ── Headshot special case within type W ───────────────────────────────────

    [Fact]
    public void BuildQueries_HeadshotCode_TypeW_FiltersOnHeadshotColumn()
    {
        var (daily, global) = GetSql("W", "headshot");
        // The Perl sets $code=1 and $matchfield="$table.headshot"
        // so the query filters on the headshot boolean column, not weapon name
        daily.Should().Contain("headshot");
        global.Should().Contain("headshot");
    }

    [Fact]
    public void BuildQueries_HeadshotCode_CodeParamIsOne()
    {
        var award = new Award { AwardId = 1, AwardType = "W", Game = "dods", Code = "headshot" };
        var (_, dailyParams, _, globalParams) = AwardsCalculationService.BuildQueries(award, BaseDate, NumDays);

        // The Perl sets $code=1 for headshot; the WHERE clause becomes headshot = @code where @code = "1"
        dailyParams.Single(p => p.Item1 == "@code").Item2.Should().Be("1");
        globalParams.Single(p => p.Item1 == "@code").Item2.Should().Be("1");
    }

    // ── Date range in daily vs. global queries ────────────────────────────────

    [Fact]
    public void BuildQueries_DailySql_ContainsDateRangeFilter()
    {
        var (daily, _) = GetSql("O", "somecustomaction");
        // Daily query must narrow to a specific time window
        daily.Should().Contain("@date").And.Contain("@cutoff");
    }

    [Fact]
    public void BuildQueries_GlobalSql_HasNoDailyDateFilter()
    {
        var (_, global) = GetSql("O", "somecustomaction");
        // Global query covers all time — no date range
        global.Should().NotContain("@date");
        global.Should().NotContain("@cutoff");
    }

    [Fact]
    public void BuildQueries_BothQueries_FilterByGame()
    {
        var (daily, global) = GetSql("O", "somecustomaction");
        daily.Should().Contain("@game");
        global.Should().Contain("@game");
    }

    // ── Parameters ────────────────────────────────────────────────────────────

    [Fact]
    public void BuildQueries_DailyParams_ContainsGameAndDateAndCutoff()
    {
        var award = new Award { AwardId = 1, AwardType = "O", Game = "dods", Code = "somecustomaction" };
        var (_, dailyParams, _, _) = AwardsCalculationService.BuildQueries(award, BaseDate, NumDays);

        dailyParams.Should().ContainSingle(p => p.Item1 == "@game");
        dailyParams.Should().ContainSingle(p => p.Item1 == "@date");
        dailyParams.Should().ContainSingle(p => p.Item1 == "@cutoff");
    }

    [Fact]
    public void BuildQueries_DailyParams_CutoffIsBaseDateMinusNumDays()
    {
        var award = new Award { AwardId = 1, AwardType = "O", Game = "dods", Code = "somecustomaction" };
        var (_, dailyParams, _, _) = AwardsCalculationService.BuildQueries(award, BaseDate, 3);

        var cutoff = (DateTime)dailyParams.Single(p => p.Item1 == "@cutoff").Item2;
        cutoff.Should().Be(BaseDate.AddDays(-3));
    }

    [Fact]
    public void BuildQueries_GlobalParams_ContainsGameButNotDates()
    {
        var award = new Award { AwardId = 1, AwardType = "O", Game = "dods", Code = "somecustomaction" };
        var (_, _, _, globalParams) = AwardsCalculationService.BuildQueries(award, BaseDate, NumDays);

        globalParams.Should().ContainSingle(p => p.Item1 == "@game");
        globalParams.Should().NotContain(p => p.Item1 == "@date");
        globalParams.Should().NotContain(p => p.Item1 == "@cutoff");
    }

    // ── Unknown AwardType ─────────────────────────────────────────────────────

    [Fact]
    public void BuildQueries_UnknownAwardType_ReturnsEmptySql()
    {
        // Unknown types produce a "no rows" stub so the caller gets a null winner
        var (daily, global) = GetSql("Z", "unknown");
        daily.Should().Contain("WHERE 1=0");
        global.Should().Contain("WHERE 1=0");
    }

    // ── Latency SQL specifics ─────────────────────────────────────────────────

    [Fact]
    public void BuildQueries_Latency_SortsAscendingLowestWins()
    {
        var (daily, global) = GetSql("W", "latency");
        // Latency is special: lowest ping wins, so ORDER BY ASC
        daily.Should().NotContain("DESC");
        global.Should().NotContain("DESC");
    }

    // ── Game parameter value is passed through ────────────────────────────────

    [Fact]
    public void BuildQueries_GameValue_IsIncludedInParams()
    {
        var award = new Award { AwardId = 1, AwardType = "W", Game = "cstrike", Code = "deagle" };
        var (_, dailyParams, _, globalParams) = AwardsCalculationService.BuildQueries(award, BaseDate, NumDays);

        dailyParams.Single(p => p.Item1 == "@game").Item2.Should().Be("cstrike");
        globalParams.Single(p => p.Item1 == "@game").Item2.Should().Be("cstrike");
    }
}
