using Godot;

/// res://scripts/TitleScreenScene.cs
/// Ana menü — Yeni Oyun / Oyun Yükle / Çıkış
public partial class TitleScreenScene : Control
{
    public override void _Ready()
    {
        GetNode<Button>("%BtnNewGame").Pressed  += _OnNewGame;
        GetNode<Button>("%BtnLoadGame").Pressed += _OnLoadGame;
        GetNode<Button>("%BtnExit").Pressed     += _OnExit;

        // Kayıtlı oyun varsa Yükle aktif, yoksa soluk
        bool hasSave = PlayerData.Instance != null && !PlayerData.Instance.IsNew;
        GetNode<Button>("%BtnLoadGame").Disabled = !hasSave;

        // Giriş animasyonu
        Modulate = new Color(1, 1, 1, 0);
        var tw = CreateTween();
        tw.TweenProperty(this, "modulate:a", 1f, 0.6f);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // M tuşu = hızlı maç testi (geliştirici kısayolu)
        if (@event is InputEventKey key && key.Pressed && !key.Echo
            && key.Keycode == Key.M)
        {
            PlayerData.Instance.Reset();
            WorldManager.Instance.GoTo("Match");
        }
    }

    private void _OnNewGame()
    {
        PlayerData.Instance.Reset();
        WorldManager.Instance.GoTo("CharacterCreation");
    }

    private void _OnLoadGame()
    {
        WorldManager.Instance.GoTo("WorldMap");
    }

    private void _OnExit()
    {
        GetTree().Quit();
    }
}
