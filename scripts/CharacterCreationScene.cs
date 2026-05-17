using Godot;

/// res://scripts/CharacterCreationScene.cs
/// Karakter oluşturma ekranı — ad girişi + pozisyon seçimi.
public partial class CharacterCreationScene : Control
{
    private LineEdit _nameInput = null!;
    private Label    _posDesc   = null!;
    private Button   _btnFW     = null!;
    private Button   _btnMF     = null!;
    private Button   _btnDF     = null!;
    private Button   _btnStart  = null!;
    private Label    _errorLabel = null!;

    private string _selectedPosition = "FW";

    private static readonly System.Collections.Generic.Dictionary<string, string> PosDescriptions = new()
    {
        { "FW", "Forvet — Hızlı, golcü. Şut Gücü ve Sprint +bonus ile başlar." },
        { "MF", "Orta Saha — Dengeli, oyun kurucu. Teknik ve Futbol IQ +bonus ile başlar." },
        { "DF", "Defans — Güçlü, dayanıklı. Dayanıklılık ve Teknik +bonus ile başlar." },
    };

    public override void _Ready()
    {
        _nameInput  = GetNode<LineEdit>("%NameInput");
        _posDesc    = GetNode<Label>("%PosDesc");
        _btnFW      = GetNode<Button>("%BtnFW");
        _btnMF      = GetNode<Button>("%BtnMF");
        _btnDF      = GetNode<Button>("%BtnDF");
        _btnStart   = GetNode<Button>("%BtnStart");
        _errorLabel = GetNode<Label>("%ErrorLabel");

        _nameInput.PlaceholderText = "Adını gir...";
        _nameInput.MaxLength = 16;

        _btnFW.Pressed   += () => _SelectPosition("FW");
        _btnMF.Pressed   += () => _SelectPosition("MF");
        _btnDF.Pressed   += () => _SelectPosition("DF");
        _btnStart.Pressed += _OnStart;

        _SelectPosition("FW");

        if (PlayerData.Instance.IsNew == false)
            CallDeferred(MethodName._GoToWorldMap);
    }

    private void _SelectPosition(string pos)
    {
        _selectedPosition = pos;
        _posDesc.Text = PosDescriptions[pos];

        _btnFW.Modulate = pos == "FW" ? new Color(1f, 0.85f, 0.2f) : Colors.White;
        _btnMF.Modulate = pos == "MF" ? new Color(1f, 0.85f, 0.2f) : Colors.White;
        _btnDF.Modulate = pos == "DF" ? new Color(1f, 0.85f, 0.2f) : Colors.White;
    }

    private void _OnStart()
    {
        string name = _nameInput.Text.Trim();
        if (name.Length < 2)
        {
            _errorLabel.Text = "En az 2 karakter giriniz.";
            return;
        }

        PlayerData.Instance.PlayerName = name;
        PlayerData.Instance.Position   = _selectedPosition;
        PlayerData.Instance.Save();

        _ApplyStartingStats();

        WorldManager.Instance.GoTo("WorldMap");
    }

    private void _GoToWorldMap() => WorldManager.Instance.GoTo("WorldMap");

    private void _ApplyStartingStats()
    {
        var gm = GameManager.Instance;
        switch (_selectedPosition)
        {
            case "FW":
                gm.ShotPower += 3;
                gm.Sprint    += 2;
                break;
            case "MF":
                gm.Technique  += 3;
                gm.FootballIQ += 2;
                break;
            case "DF":
                gm.Stamina    += 3;
                gm.Technique  += 1;
                break;
        }
    }
}
