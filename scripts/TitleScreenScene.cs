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

    private void _OnNewGame()
    {
        // Eski kaydı sıfırla, karakter oluşturmaya git
        PlayerData.Instance.Reset();
        WorldManager.Instance.GoTo("CharacterCreation");
    }

    private void _OnLoadGame()
    {
        // Kayıtlı oyun varsa doğrudan WorldMap'e
        WorldManager.Instance.GoTo("WorldMap");
    }

    private void _OnExit()
    {
        GetTree().Quit();
    }
}
