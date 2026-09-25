using HLStatsX.NET.Awards.Services;

namespace HLStatsX.NET.Tests.Awards;

public class ClanPatternMatcherTests
{
    // ── Position: START ─────────────────────────────────────────────────────

    [Fact]
    public void Match_StartPosition_MatchesTagAtStartOfName()
    {
        var m = new ClanPatternMatcher([("[TF2]", "START")]);
        var result = m.Match("[TF2]FragLord");
        result.Should().NotBeNull();
        result!.Tag.Should().Be("[TF2]");
    }

    [Fact]
    public void Match_StartPosition_NoMatchWhenTagAtEnd()
    {
        var m = new ClanPatternMatcher([("[TF2]", "START")]);
        m.Match("FragLord[TF2]").Should().BeNull();
    }

    [Fact]
    public void Match_StartPosition_NoMatchWhenNameHasNoSuffix()
    {
        // Pattern requires at least one character AFTER the tag (.+)
        var m = new ClanPatternMatcher([("[TF2]", "START")]);
        m.Match("[TF2]").Should().BeNull();
    }

    // ── Position: END ────────────────────────────────────────────────────────

    [Fact]
    public void Match_EndPosition_MatchesTagAtEndOfName()
    {
        var m = new ClanPatternMatcher([("[TF2]", "END")]);
        var result = m.Match("FragLord[TF2]");
        result.Should().NotBeNull();
        result!.Tag.Should().Be("[TF2]");
    }

    [Fact]
    public void Match_EndPosition_NoMatchWhenTagAtStart()
    {
        var m = new ClanPatternMatcher([("[TF2]", "END")]);
        m.Match("[TF2]FragLord").Should().BeNull();
    }

    [Fact]
    public void Match_EndPosition_NoMatchWhenNameHasNoPrefix()
    {
        // Pattern requires at least one character BEFORE the tag (.+)
        var m = new ClanPatternMatcher([("[TF2]", "END")]);
        m.Match("[TF2]").Should().BeNull();
    }

    // ── Position: EITHER ─────────────────────────────────────────────────────

    [Fact]
    public void Match_EitherPosition_MatchesTagAtStart()
    {
        var m = new ClanPatternMatcher([("=TB=", "EITHER")]);
        var result = m.Match("=TB=FragLord");
        result.Should().NotBeNull();
        result!.Tag.Should().Be("=TB=");
    }

    [Fact]
    public void Match_EitherPosition_MatchesTagAtEnd()
    {
        var m = new ClanPatternMatcher([("=TB=", "EITHER")]);
        var result = m.Match("FragLord=TB=");
        result.Should().NotBeNull();
        result!.Tag.Should().Be("=TB=");
    }

    // ── Clan name capture (group 2) ──────────────────────────────────────────

    [Fact]
    public void Match_CapturesClanName_AlphanumericPortionOfTag()
    {
        // "[TF2]" → escaped → "\[TF2\]" → wrap first alnum → "\[(TF2)\]"
        // group 1 = "[TF2]", group 2 = "TF2"
        var m = new ClanPatternMatcher([("[TF2]", "START")]);
        var result = m.Match("[TF2]SomeName");
        result!.Name.Should().Be("TF2");
    }

    [Fact]
    public void Match_CapturesClanName_BracketsAroundPureName()
    {
        // "=TB=" → escaped → "=TB=" → wrap "TB" → "=(TB)="
        // group 1 = "=TB=", group 2 = "TB"
        var m = new ClanPatternMatcher([("=TB=", "START")]);
        var result = m.Match("=TB=SomeName");
        result!.Name.Should().Be("TB");
    }

    [Fact]
    public void Match_CapturesClanName_PlainAlphanumericTag()
    {
        // "TEAM" → "(TEAM)" → group 1 = group 2 = "TEAM"
        var m = new ClanPatternMatcher([("TEAM", "START")]);
        var result = m.Match("TEAMSomeName");
        result!.Tag.Should().Be("TEAM");
        result.Name.Should().Be("TEAM");
    }

    // ── No match ─────────────────────────────────────────────────────────────

    [Fact]
    public void Match_ReturnsNull_WhenNoPatternMatches()
    {
        var m = new ClanPatternMatcher([("[TF2]", "START"), ("=TB=", "EITHER")]);
        m.Match("UnaffiliatedPlayer").Should().BeNull();
    }

    [Fact]
    public void Match_ReturnsNull_WhenMatcherHasNoPatterns()
    {
        var m = new ClanPatternMatcher([]);
        m.Match("AnyPlayer").Should().BeNull();
    }

    // ── Wildcard: A = any single character ───────────────────────────────────

    [Fact]
    public void Match_AWildcard_MatchesAnyCharInThatPosition()
    {
        // Pattern "ATeam" → "A" → "." → pattern matches any char + "Team"
        var m = new ClanPatternMatcher([("ATeam", "START")]);

        m.Match("ATeamPlayer").Should().NotBeNull("original A matches literal A via dot wildcard");
        m.Match("BTeamPlayer").Should().NotBeNull("B matches the dot wildcard too");
        m.Match("ZTeamPlayer").Should().NotBeNull("any char matches");
    }

    [Fact]
    public void Match_AWildcard_RequiresExactlyOneChar()
    {
        // "." matches exactly one char, not zero
        var m = new ClanPatternMatcher([("ATeam", "START")]);
        m.Match("TeamPlayer").Should().BeNull("no char before Team — dot requires one");
    }

    // ── Wildcard: X = optional character ─────────────────────────────────────

    [Fact]
    public void Match_XWildcard_MatchesZeroOrOneChar()
    {
        // Pattern "XTeam" → "X" → ".?" → matches optional char + "Team"
        var m = new ClanPatternMatcher([("XTeam", "START")]);

        m.Match("TeamPlayer").Should().NotBeNull("X is optional — zero chars before Team");
        m.Match("ATeamPlayer").Should().NotBeNull("one char before Team");
    }

    // ── Case insensitivity ───────────────────────────────────────────────────

    [Fact]
    public void Match_IsCaseInsensitive()
    {
        var m = new ClanPatternMatcher([("[TF2]", "START")]);
        m.Match("[tf2]FragLord").Should().NotBeNull();
        m.Match("[TF2]FragLord").Should().NotBeNull();
    }

    // ── Multiple patterns: first match wins ──────────────────────────────────

    [Fact]
    public void Match_MultiplePatterns_ReturnsFirstMatch()
    {
        // Patterns tried in order; the player name matches both but first should win
        var m = new ClanPatternMatcher([
            ("[TF2]", "START"),
            ("TF",    "START")
        ]);

        // "[TF2]FragLord" matches "[TF2]" first
        var result = m.Match("[TF2]FragLord");
        result!.Tag.Should().Be("[TF2]");
    }

    // ── Special characters in tag are properly escaped ────────────────────────

    [Fact]
    public void Match_SpecialRegexCharsInTag_AreEscaped()
    {
        // Parentheses, dots, etc. in the actual tag string must not break the regex
        var m = new ClanPatternMatcher([("(Elite)", "START")]);
        var result = m.Match("(Elite)SomeName");
        result.Should().NotBeNull();
        result!.Tag.Should().Be("(Elite)");
    }

    [Fact]
    public void Match_PipesAndPlusSigns_AreEscaped()
    {
        var m = new ClanPatternMatcher([("TF|2+", "START")]);
        m.Match("TF|2+SomeName").Should().NotBeNull();
        m.Match("TF2SomeName").Should().BeNull("unescaped pipe/plus would produce false matches");
    }

    // ── Pattern ordering: longer patterns are tried first ────────────────────

    [Fact]
    public void BuildPatterns_LongerTagFirstInInputOrder_MatchedFirst()
    {
        // Caller is responsible for ordering — ClanPatternMatcher preserves input order
        var m = new ClanPatternMatcher([
            ("[ELITE]", "START"),  // longer — should be tried first
            ("[EL]",    "START"),
        ]);

        // "[ELITE]Player" should match the longer tag, not the shorter prefix
        var result = m.Match("[ELITE]Player");
        result!.Tag.Should().Be("[ELITE]");
    }

    // ── BuildPatterns static method ──────────────────────────────────────────

    [Fact]
    public void BuildPatterns_StartPosition_ProducesOnePattern()
    {
        var patterns = ClanPatternMatcher.BuildPatterns([("TF2", "START")]);
        patterns.Should().HaveCount(1);
    }

    [Fact]
    public void BuildPatterns_EndPosition_ProducesOnePattern()
    {
        var patterns = ClanPatternMatcher.BuildPatterns([("TF2", "END")]);
        patterns.Should().HaveCount(1);
    }

    [Fact]
    public void BuildPatterns_EitherPosition_ProducesTwoPatterns()
    {
        var patterns = ClanPatternMatcher.BuildPatterns([("TF2", "EITHER")]);
        patterns.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPatterns_EmptyInput_ProducesEmptyList()
    {
        var patterns = ClanPatternMatcher.BuildPatterns([]);
        patterns.Should().BeEmpty();
    }
}
