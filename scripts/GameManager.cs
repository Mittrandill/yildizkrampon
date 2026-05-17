using Godot;

/// res://scripts/GameManager.cs
/// Autoload singleton — persists all player stats across scene transitions.
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; } = null!;

    // Core stats (0-100)
    public int Energy { get; set; } = 60;
    public int Morale { get; set; } = 70;
    public int Fatigue { get; set; } = 20;

    // Football attributes
    public int ShotPower { get; set; } = 38;
    public int Sprint { get; set; } = 36;
    public int Technique { get; set; } = 35;
    public int Overall { get; set; } = 38;

    // Day state
    public int DayStep { get; set; } = 0;  // 0=morning, 1=afternoon, 2=evening
    public bool BoughtProteinBar { get; set; } = false;
    public bool WatchedByCoach { get; set; } = false;
    public int MatchGoalsScored { get; set; } = 0;

    // XP events for end-of-day display
    public System.Collections.Generic.List<string> DayEvents { get; } = new();

    public override void _Ready()
    {
        Instance = this;
    }

    public void ClampStats()
    {
        Energy = Mathf.Clamp(Energy, 0, 100);
        Morale = Mathf.Clamp(Morale, 0, 100);
        Fatigue = Mathf.Clamp(Fatigue, 0, 100);
        ShotPower = Mathf.Clamp(ShotPower, 0, 99);
        Sprint = Mathf.Clamp(Sprint, 0, 99);
        Technique = Mathf.Clamp(Technique, 0, 99);
    }

    public void RecalcOverall()
    {
        Overall = (ShotPower + Sprint + Technique) / 3;
    }

    public void AddEvent(string description)
    {
        DayEvents.Add(description);
    }
}
