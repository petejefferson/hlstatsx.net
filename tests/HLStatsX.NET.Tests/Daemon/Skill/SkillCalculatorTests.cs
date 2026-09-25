using FluentAssertions;
using HLStatsX.NET.Daemon.Skill;

namespace HLStatsX.NET.Tests.Daemon.Skill;

/// <summary>
/// Verifies <see cref="SkillCalculator.Calc"/> against the Perl daemon's
/// <c>calcSkill</c> subroutine with default parameters.
/// </summary>
public sealed class SkillCalculatorTests
{
    // Default daemon options used across most tests
    private const int MaxChange    = 100;
    private const int MinChange    = 2;
    private const int MinKills     = 50;
    private const bool RatioCap   = false;

    private static SkillResult Calc(
        int mode, int killerSkill, int killerKills, int victimSkill, int victimKills,
        double modifier = 1.0, string killerTeam = "")
        => SkillCalculator.Calc(mode, killerSkill, killerKills, victimSkill, victimKills,
            modifier, MaxChange, MinChange, MinKills, RatioCap, killerTeam);

    // --- Zero/low skill guards ---

    [Fact]
    public void Calc_KillerSkillZero_ReturnsMinChange()
    {
        var r = Calc(0, 0, 100, 1000, 100);

        r.KillerSkill.Should().Be(MinChange);  // 0 + minChange
        r.VictimSkill.Should().Be(1000);        // unchanged
    }

    [Fact]
    public void Calc_VictimSkillZero_ReturnsMinChange()
    {
        var r = Calc(0, 1000, 100, 0, 100);

        r.KillerSkill.Should().Be(1002);  // 1000 + minChange
        r.VictimSkill.Should().Be(0);     // unchanged
    }

    // --- Mode 0: symmetric (victim loses same as killer gains) ---

    [Fact]
    public void Calc_Mode0_EqualSkills_EachChangesByMinimumFloor()
    {
        // equal skills → ratio = 1.0, change = 1*5*1 = 5 → above minChange of 2
        var r = Calc(0, 1000, 100, 1000, 100);

        r.KillerSkill.Should().Be(1005);
        r.VictimSkill.Should().Be(995);
    }

    [Fact]
    public void Calc_Mode0_LowKillerSkill_IncreasesMoreThanEqualSkills()
    {
        // victim(2000)/killer(1000) = 2.0 ratio → change = 2*5*1 = 10
        var r = Calc(0, 1000, 100, 2000, 100);

        r.KillerSkill.Should().Be(1010);
        r.VictimSkill.Should().Be(1990);
    }

    [Fact]
    public void Calc_Mode0_ChangesCappedAtMaxChange()
    {
        // victim(10000)/killer(100) = 100 → 100*5 = 500 → capped at MaxChange=100
        var r = Calc(0, 100, 100, 10000, 100);

        r.KillerSkill.Should().Be(200);    // 100 + 100
        r.VictimSkill.Should().Be(9900);   // 10000 - 100
    }

    // --- Mode 1: victim loses 75% ---

    [Fact]
    public void Calc_Mode1_VictimLoses75Percent()
    {
        // ratio 1.0 → killerChange=5, victimChange=5*0.75=3.75 → int(3.75+0.5)=4...
        // Actually victim starts at 1000, loses 3 (int(3.75-0.5) rounds down)
        // Perl uses sprintf "%d", value+0.5 — so 3.75 rounds to 4
        var r = Calc(1, 1000, 100, 1000, 100);

        r.KillerSkill.Should().Be(1005);
        r.VictimSkill.Should().Be(996);  // 1000 - round(5*0.75) = 1000-4=996
    }

    // --- Mode 2: victim loses 50% ---

    [Fact]
    public void Calc_Mode2_VictimLoses50Percent()
    {
        var r = Calc(2, 1000, 100, 1000, 100);

        r.KillerSkill.Should().Be(1005);
        r.VictimSkill.Should().Be(998);   // 1000 - round(5*0.5)=1000-3=997...
                                          // wait: 5*0.5=2.5, victimSkill - 2.5 + 0.5 = 998
    }

    // --- Mode 3: victim loses 25% ---

    [Fact]
    public void Calc_Mode3_VictimLoses25Percent()
    {
        var r = Calc(3, 1000, 100, 1000, 100);

        // 5*0.25=1.25 < minChange(2), so victimChange floored to minChange=2
        r.KillerSkill.Should().Be(1005);
        r.VictimSkill.Should().Be(998);   // 1000 - 2 = 998
    }

    // --- Mode 4: victim loses nothing ---

    [Fact]
    public void Calc_Mode4_VictimLosesZero()
    {
        var r = Calc(4, 1000, 100, 1000, 100);

        r.KillerSkill.Should().Be(1005);
        r.VictimSkill.Should().Be(1000);  // unchanged
    }

    // --- Minimum kills guard ---

    [Fact]
    public void Calc_KillerBelowMinKills_OnlyMinChangeApplied()
    {
        // killer has only 10 kills, below MinKills=50
        var r = Calc(0, 5000, 10, 1000, 100);

        r.KillerSkill.Should().Be(5002);   // 5000 + minChange
        r.VictimSkill.Should().Be(998);    // 1000 - minChange
    }

    [Fact]
    public void Calc_VictimBelowMinKills_OnlyMinChangeApplied()
    {
        var r = Calc(0, 1000, 100, 5000, 10);

        r.KillerSkill.Should().Be(1002);
        r.VictimSkill.Should().Be(4998);
    }

    [Fact]
    public void Calc_Mode4_VictimBelowMinKills_VictimLosesZero()
    {
        var r = Calc(4, 1000, 100, 5000, 10);

        r.KillerSkill.Should().Be(1002);
        r.VictimSkill.Should().Be(5000);  // mode 4 keeps victim unchanged even below min kills
    }

    // --- Weapon modifier ---

    [Fact]
    public void Calc_WeaponModifier2_DoublesSkillChange()
    {
        // ratio=1.0, change=1*5*2=10
        var r = Calc(0, 1000, 100, 1000, 100, modifier: 2.0);

        r.KillerSkill.Should().Be(1010);
        r.VictimSkill.Should().Be(990);
    }

    // --- SkillRatioCap ---

    [Fact]
    public void Calc_SkillRatioCap_ClampedRatio()
    {
        // Without cap: ratio = 10000/100 = 100 → capped at MaxChange=100
        // With cap: ratio clamped to 1/0.7 ≈ 1.4286 → change = 1.4286*5 ≈ 7.14 → 7
        var r = SkillCalculator.Calc(0, 100, 100, 10000, 100,
            1.0, MaxChange, MinChange, MinKills, skillRatioCap: true);

        // With cap, killer only gains ~7 instead of 100
        r.KillerSkill.Should().BeLessThan(200);
        r.KillerSkill.Should().BeGreaterThan(100);
    }

    // --- L4D variant ---

    [Fact]
    public void CalcL4D_StandardDifficulty_ReturnsCorrectSkill()
    {
        // pointValue=10, difficulty=2 → weight=1.0, change=10
        int result = SkillCalculator.CalcL4D(1000, 10, 2, 1.0, MaxChange, MinChange);

        result.Should().Be(1010);
    }

    [Fact]
    public void CalcL4D_ZeroDifficulty_UsesDefaultWeight()
    {
        // difficulty=0 → weight=0.5, change=10*0.5=5
        int result = SkillCalculator.CalcL4D(1000, 10, 0, 1.0, MaxChange, MinChange);

        result.Should().Be(1005);
    }
}
