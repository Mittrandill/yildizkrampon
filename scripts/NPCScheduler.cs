using Godot;
using System.Collections.Generic;

/// res://scripts/NPCScheduler.cs
/// Autoload — her NPC'nin saatlik konumunu döndürür.
public partial class NPCScheduler : Node
{
    public static NPCScheduler Instance { get; private set; } = null!;

    // NPC adları
    public static readonly string[] NPCNames = { "Anne", "Baba", "Eren", "Baran", "KemalHoca", "RizaAbi" };

    public override void _Ready()
    {
        Instance = this;
    }

    /// NPC'nin şu anki konumunu döndürür ("HomeInterior", "BakkalInterior" vb.)
    public string GetLocation(string npc)
    {
        int h = (int)GameTime.Instance.Hour;
        return npc switch
        {
            "Anne"      => h is >= 6  and < 8  ? "Mutfak"
                         : h is >= 8  and < 12 ? "Dis" // market
                         : "HomeInterior",
            "Baba"      => h is >= 8  and < 18 ? "Dis" // iş
                         : "HomeInterior",
            "Eren"      => h is >= 15 and < 18 ? "MahalleSahasi"
                         : h is >= 8  and < 15 ? "Dis" // okul
                         : "HomeInterior",
            "Baran"     => h is >= 15 and < 18 ? "CayBahcesi"
                         : h is >= 8  and < 15 ? "Dis" // okul
                         : "HomeInterior",
            "KemalHoca" => h is >= 8  and < 14 ? "SporTesisi"
                         : h is >= 14 and < 18 ? "MahalleSahasi"
                         : "HomeInterior",
            "RizaAbi"   => h is >= 8  and < 20 ? "BakkalInterior"
                         : "HomeInterior",
            _           => "HomeInterior",
        };
    }

    public bool IsAt(string npc, string location) => GetLocation(npc) == location;
}
