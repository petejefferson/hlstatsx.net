using HLStatsX.NET.Awards.Workers;

namespace HLStatsX.NET.Tests.Awards;

public class MaintenanceTaskFlagsTests
{
    // ── Default (no specific task flags) ─────────────────────────────────────
    // Matches the Perl script's default behaviour: -iarp (inactive, awards, ribbons, prune)

    [Fact]
    public void FromArgs_NoTaskFlags_ReturnsDefaultSet()
    {
        var flags = MaintenanceTaskFlags.FromArgs(["app.exe", "--run-now"]);
        flags.Should().BeEquivalentTo(MaintenanceTaskFlags.Default);
    }

    [Fact]
    public void Default_IncludesExpectedTasks()
    {
        var d = MaintenanceTaskFlags.Default;
        d.Inactive.Should().BeTrue();
        d.Awards.Should().BeTrue();
        d.Ribbons.Should().BeTrue();
        d.Prune.Should().BeTrue();
        d.Clans.Should().BeFalse();
        d.Optimize.Should().BeFalse();
        d.GeoIp.Should().BeFalse();
    }

    // ── --all ─────────────────────────────────────────────────────────────────

    [Fact]
    public void FromArgs_AllFlag_ReturnsAllTasks()
    {
        var flags = MaintenanceTaskFlags.FromArgs(["app.exe", "--run-now", "--all"]);
        flags.Should().BeEquivalentTo(MaintenanceTaskFlags.All);
    }

    [Fact]
    public void All_HasEveryTaskEnabled()
    {
        var a = MaintenanceTaskFlags.All;
        a.Inactive.Should().BeTrue();
        a.Awards.Should().BeTrue();
        a.Ribbons.Should().BeTrue();
        a.Prune.Should().BeTrue();
        a.Clans.Should().BeTrue();
        a.Optimize.Should().BeTrue();
        a.GeoIp.Should().BeTrue();
        a.Resolve.Should().BeTrue();
    }

    [Fact]
    public void FromArgs_AllFlagTakesPrecedenceOverOtherFlags()
    {
        // Even if individual flags are also present, --all wins
        var flags = MaintenanceTaskFlags.FromArgs(["app.exe", "--run-now", "--all", "--prune"]);
        flags.Should().BeEquivalentTo(MaintenanceTaskFlags.All);
    }

    // ── Individual flags ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("--inactive", true,  false, false, false, false, false, false, false)]
    [InlineData("--awards",   false, true,  false, false, false, false, false, false)]
    [InlineData("--ribbons",  false, false, true,  false, false, false, false, false)]
    [InlineData("--prune",    false, false, false, true,  false, false, false, false)]
    [InlineData("--clans",    false, false, false, false, true,  false, false, false)]
    [InlineData("--optimize", false, false, false, false, false, true,  false, false)]
    [InlineData("--geoip",    false, false, false, false, false, false, true,  false)]
    [InlineData("--resolve",  false, false, false, false, false, false, false, true )]
    public void FromArgs_SingleFlag_EnablesOnlyThatTask(
        string flag,
        bool inactive, bool awards, bool ribbons, bool prune, bool clans, bool optimize, bool geoip, bool resolve)
    {
        var f = MaintenanceTaskFlags.FromArgs(["app.exe", "--run-now", flag]);

        f.Inactive.Should().Be(inactive);
        f.Awards.Should().Be(awards);
        f.Ribbons.Should().Be(ribbons);
        f.Prune.Should().Be(prune);
        f.Clans.Should().Be(clans);
        f.Optimize.Should().Be(optimize);
        f.GeoIp.Should().Be(geoip);
        f.Resolve.Should().Be(resolve);
    }

    // ── Flag combinations ─────────────────────────────────────────────────────

    [Fact]
    public void FromArgs_TwoFlags_EnablesBothTasks()
    {
        var flags = MaintenanceTaskFlags.FromArgs(["app.exe", "--run-now", "--awards", "--ribbons"]);
        flags.Awards.Should().BeTrue();
        flags.Ribbons.Should().BeTrue();
        flags.Inactive.Should().BeFalse();
        flags.Prune.Should().BeFalse();
        flags.Clans.Should().BeFalse();
        flags.Optimize.Should().BeFalse();
    }

    [Fact]
    public void FromArgs_ClansAndOptimize_OnlyThoseTwoEnabled()
    {
        var flags = MaintenanceTaskFlags.FromArgs(["app.exe", "--run-now", "--clans", "--optimize"]);
        flags.Clans.Should().BeTrue();
        flags.Optimize.Should().BeTrue();
        flags.Inactive.Should().BeFalse();
        flags.Awards.Should().BeFalse();
        flags.Ribbons.Should().BeFalse();
        flags.Prune.Should().BeFalse();
    }

    // ── Arg order and noise ───────────────────────────────────────────────────

    [Fact]
    public void FromArgs_UnrecognisedArgs_AreIgnored()
    {
        var flags = MaintenanceTaskFlags.FromArgs(["app.exe", "--run-now", "--verbose", "--awards"]);
        flags.Awards.Should().BeTrue();
        flags.Inactive.Should().BeFalse();
    }

    [Fact]
    public void FromArgs_EmptyArgs_ReturnsDefault()
    {
        var flags = MaintenanceTaskFlags.FromArgs([]);
        flags.Should().BeEquivalentTo(MaintenanceTaskFlags.Default);
    }

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_Default_ContainsExpectedTaskNames()
    {
        var s = MaintenanceTaskFlags.Default.ToString();
        s.Should().Contain("inactive");
        s.Should().Contain("awards");
        s.Should().Contain("ribbons");
        s.Should().Contain("prune");
        s.Should().NotContain("clans");
        s.Should().NotContain("optimize");
        s.Should().NotContain("geoip");
    }

    [Fact]
    public void ToString_AllFalse_ReturnsNone()
    {
        var flags = new MaintenanceTaskFlags(false, false, false, false, false, false, false, false);
        flags.ToString().Should().Be("none");
    }

    [Fact]
    public void ToString_AllTrue_ContainsAllTaskNames()
    {
        var s = MaintenanceTaskFlags.All.ToString();
        s.Should().Contain("inactive");
        s.Should().Contain("awards");
        s.Should().Contain("ribbons");
        s.Should().Contain("prune");
        s.Should().Contain("clans");
        s.Should().Contain("optimize");
        s.Should().Contain("geoip");
        s.Should().Contain("resolve");
    }
}
