namespace HLStatsX.NET.Core.Models;

/// <summary>
/// One row in the banned-players leaderboard, matching the 10-column output of
/// <c>bans.php</c>. All ratio fields are pre-computed from the raw player stats.
/// </summary>
/// <param name="PlayerId">Database primary key — used to link to the player profile.</param>
/// <param name="PlayerName">Display name at the time the ban was recorded.</param>
/// <param name="Flag">Two-letter country code used to locate the flag image, or <c>null</c>.</param>
/// <param name="BanDate">
///     Timestamp of the player's last in-game event, which the PHP site repurposes as the
///     ban date. Derived from the <c>last_event</c> Unix timestamp column.
/// </param>
/// <param name="Skill">Current skill-point total.</param>
/// <param name="ActivityScore">Decay-based activity value (0–100) used to render the bar graph.</param>
/// <param name="Kills">Career kill count.</param>
/// <param name="Deaths">Career death count.</param>
/// <param name="Headshots">Career headshot count.</param>
/// <param name="Kpd">
///     Kill/death ratio rounded to 2 decimal places.
///     Uses <c>max(deaths, 1)</c> as the denominator so the value is never undefined.
/// </param>
/// <param name="HsK">
///     Headshots per kill rounded to 2 decimal places, or <c>null</c> when
///     <paramref name="Kills"/> is zero (matches PHP's <c>IFNULL(…, '-')</c> behaviour).
/// </param>
/// <param name="Accuracy">
///     Shot accuracy as a percentage (0–100), rounded to the nearest integer.
///     Returns <c>0</c> when no shots have been recorded.
/// </param>
public record BanListRow(
    int      PlayerId,
    string   PlayerName,
    string?  Flag,
    DateTime BanDate,
    int      Skill,
    int      ActivityScore,
    int      Kills,
    int      Deaths,
    int      Headshots,
    double   Kpd,
    double?  HsK,
    double   Accuracy
);
