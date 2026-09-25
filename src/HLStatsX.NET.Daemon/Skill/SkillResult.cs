namespace HLStatsX.NET.Daemon.Skill;

/// <summary>Updated skill values for both kill participants.</summary>
/// <param name="KillerSkill">Killer's new skill total.</param>
/// <param name="VictimSkill">Victim's new skill total.</param>
public readonly record struct SkillResult(int KillerSkill, int VictimSkill);
