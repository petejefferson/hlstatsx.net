namespace HLStatsX.NET.Core.Entities.Events;

public class EventStatsme2
{
    public int ServerId { get; set; }
    public int PlayerId { get; set; }
    public string Weapon { get; set; } = string.Empty;
    public int Head { get; set; }
    public int Chest { get; set; }
    public int Stomach { get; set; }
    public int LeftArm { get; set; }
    public int RightArm { get; set; }
    public int LeftLeg { get; set; }
    public int RightLeg { get; set; }
}
