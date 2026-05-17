using Godot;

/// res://scripts/PlayerData.cs
/// Autoload — oyuncu adı, pozisyonu ve görünümünü saklar. user:// dizinine kaydeder.
public partial class PlayerData : Node
{
    public static PlayerData Instance { get; private set; } = null!;

    public string PlayerName     { get; set; } = "";
    public string Position       { get; set; } = "FW";   // FW | MF | DF
    public bool   IsNew          { get; set; } = true;
    public int    SkinColorIndex { get; set; } = 2;
    public int    HairColorIndex { get; set; } = 0;

    private const string SavePath = "user://player_data.cfg";

    public override void _Ready()
    {
        Instance = this;
        Load();
    }

    public void Save()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("player", "name",       PlayerName);
        cfg.SetValue("player", "position",   Position);
        cfg.SetValue("player", "skin_color", SkinColorIndex);
        cfg.SetValue("player", "hair_color", HairColorIndex);
        cfg.SetValue("player", "is_new",     false);
        cfg.Save(SavePath);
        IsNew = false;
    }

    public void Load()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) == Error.Ok)
        {
            PlayerName     = (string)cfg.GetValue("player", "name",       "");
            Position       = (string)cfg.GetValue("player", "position",   "FW");
            SkinColorIndex = (int)   cfg.GetValue("player", "skin_color", 2);
            HairColorIndex = (int)   cfg.GetValue("player", "hair_color", 0);
            IsNew          = (bool)  cfg.GetValue("player", "is_new",     true);
        }
    }

    // Geliştirme sırasında karakteri sıfırlamak için
    public void Reset()
    {
        PlayerName = "";
        Position   = "FW";
        IsNew      = true;
        DirAccess.Open("user://");
        if (FileAccess.FileExists(SavePath))
            DirAccess.Open("user://").Remove(SavePath);
    }
}
