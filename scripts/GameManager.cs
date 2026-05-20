using Godot;

/// Global oyun durumu — enerji, para, gün, istatistikler.
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; } = null!;

    // Oyuncu stats
    public int   Day     { get; set; } = 1;
    public float Energy  { get; set; } = 100f;
    public int   Money   { get; set; } = 0;
    public int   Speed   { get; set; } = 30;
    public int   Stamina { get; set; } = 30;
    public int   Skill   { get; set; } = 30;
    public int   Overall => (Speed + Stamina + Skill) / 3;

    // Oyuncu adı
    public string PlayerName { get; set; } = "Kahraman";

    // Match system stats (used by NeighborhoodMatch)
    public int   ShotPower { get; set; } = 50;
    public int   SoccerIq  { get; set; } = 50;
    public float Morale    { get; set; } = 100f;
    public float Fatigue   { get; set; } = 0f;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public void SpendEnergy(float amount)
        => Energy = Mathf.Max(0f, Energy - amount);

    public void EarnMoney(int amount)
        => Money += amount;

    public string CompletePlayableMatch(int goalsScored, int goalsConceded, float rating = 6f)
    {
        Fatigue = Mathf.Min(100f, Fatigue + 20f);
        return goalsScored > goalsConceded ? "win" : goalsScored < goalsConceded ? "loss" : "draw";
    }
}
