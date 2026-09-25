namespace HLStatsX.NET.Daemon.Skill;

/// <summary>
/// Calculates ELO-style skill changes for kills, matching the Perl daemon's
/// <c>calcSkill</c> and <c>calcL4DSkill</c> subroutines exactly.
/// </summary>
public static class SkillCalculator
{
    /// <summary>
    /// Computes new killer and victim skill values after a kill event.
    /// </summary>
    /// <param name="skillMode">
    /// Controls how much of the killer's gain the victim loses:
    /// 0 = 100%, 1 = 75%, 2 = 50%, 3 = 25%, 4 = 0%,
    /// 5 = team-based (Zombie Panic: Source).
    /// </param>
    /// <param name="killerSkill">Current killer skill.</param>
    /// <param name="killerKills">Lifetime killer kills (for min-kills guard).</param>
    /// <param name="victimSkill">Current victim skill.</param>
    /// <param name="victimKills">Lifetime victim kills (for min-kills guard).</param>
    /// <param name="weaponModifier">Weapon skill modifier (default 1.0).</param>
    /// <param name="skillMaxChange">Maximum skill points that can be gained/lost per kill.</param>
    /// <param name="skillMinChange">Minimum skill points gained/lost per kill.</param>
    /// <param name="playerMinKills">
    /// Players with fewer than this many kills receive only <paramref name="skillMinChange"/>.
    /// </param>
    /// <param name="skillRatioCap">
    /// When true, caps the skill ratio to [0.7, 1/0.7] to prevent huge swings.
    /// </param>
    /// <param name="killerTeam">
    /// Killer's team name. Only used in <paramref name="skillMode"/> 5 (ZPS).
    /// </param>
    /// <returns>
    /// A <see cref="SkillResult"/> with the updated killer and victim skill values.
    /// </returns>
    public static SkillResult Calc(
        int skillMode,
        int killerSkill,
        int killerKills,
        int victimSkill,
        int victimKills,
        double weaponModifier,
        int skillMaxChange,
        int skillMinChange,
        int playerMinKills,
        bool skillRatioCap,
        string killerTeam = "")
    {
        // Guard: ignored/zero-skill killers always receive only the minimum change.
        if (killerSkill < 1)
            return new SkillResult(killerSkill + skillMinChange, victimSkill);
        if (victimSkill < 1)
            return new SkillResult(killerSkill + skillMinChange, victimSkill);

        double ratio = (double)victimSkill / killerSkill;

        if (skillRatioCap)
        {
            const double lowRatio  = 0.7;
            const double highRatio = 1.0 / lowRatio;
            ratio = Math.Clamp(ratio, lowRatio, highRatio);
        }

        double killerChange = ratio * 5.0 * weaponModifier;

        if (killerChange > skillMaxChange)
            killerChange = skillMaxChange;

        double victimChange = skillMode switch
        {
            1 => killerChange * 0.75,
            2 => killerChange * 0.50,
            3 => killerChange * 0.25,
            4 => 0.0,
            5 => killerTeam == "Undead"   ? killerChange * 0.50
               : killerTeam == "Survivor" ? killerChange * 0.25
               : killerChange,
            _ => killerChange,
        };

        if (victimChange > skillMaxChange)
            victimChange = skillMaxChange;

        // Apply minimum change floor (unless skill_mode 4 which forces 0 loss)
        if (skillMaxChange >= skillMinChange)
        {
            if (killerChange < skillMinChange)
                killerChange = skillMinChange;

            if (victimChange < skillMinChange && skillMode != 4)
                victimChange = skillMinChange;
        }

        // Players below the minimum kill threshold receive only the minimum change.
        if (killerKills < playerMinKills || victimKills < playerMinKills)
        {
            killerChange = skillMinChange;
            victimChange = skillMode == 4 ? 0.0 : skillMinChange;
        }

        // Round to nearest integer (Perl: sprintf "%d", value + 0.5)
        int newKillerSkill = (int)(killerSkill + killerChange + 0.5);
        int newVictimSkill = (int)(victimSkill - victimChange + 0.5);

        return new SkillResult(newKillerSkill, newVictimSkill);
    }

    /// <summary>
    /// Computes killer skill gain for Left 4 Dead (single-player kill against infected).
    /// Victim skill does not apply in L4D mode.
    /// </summary>
    /// <param name="killerSkill">Current killer skill.</param>
    /// <param name="pointValue">Base point value for the kill (game-mode dependent).</param>
    /// <param name="difficulty">
    /// Difficulty multiplier (0 = use 0.5 weight; otherwise weight = difficulty / 2).
    /// </param>
    /// <param name="weaponModifier">Weapon skill modifier.</param>
    /// <param name="skillMaxChange">Maximum skill change cap.</param>
    /// <param name="skillMinChange">Minimum skill change floor.</param>
    /// <returns>The updated killer skill value.</returns>
    public static int CalcL4D(
        int killerSkill,
        double pointValue,
        double difficulty,
        double weaponModifier,
        int skillMaxChange,
        int skillMinChange)
    {
        double diffWeight = difficulty > 0 ? difficulty / 2.0 : 0.5;
        double change = pointValue * diffWeight;

        if (change > skillMaxChange)
            change = skillMaxChange;

        if (skillMaxChange >= skillMinChange && change < skillMinChange)
            change = skillMinChange;

        return (int)(killerSkill + change + 0.5);
    }
}
