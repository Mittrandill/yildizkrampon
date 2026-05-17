using Godot;

/// res://scripts/CharacterCreationScene.cs
/// Karakter oluşturma — iki panel: sol önizleme, sag özelleştirme.
public partial class CharacterCreationScene : Control
{
    private static readonly string[] TypeKeys  = { "FW",          "MF",            "DF"           };
    private static readonly string[] TypeNames = { "Hızlı Kanat", "Oyun Kurucu",   "Demir Duvar"  };
    private static readonly string[] TypeDescs = {
        "Hızlı, golcü. Şut Gücü ve Sprint +bonus ile başlar.",
        "Dengeli, oyun kurucu. Teknik ve Futbol IQ +bonus ile başlar.",
        "Güçlü, dayanıklı. Dayanıklılık ve Teknik +bonus ile başlar.",
    };

    private static readonly Color[] SkinColors = {
        new Color(1.00f, 0.87f, 0.73f),
        new Color(0.95f, 0.76f, 0.60f),
        new Color(0.87f, 0.65f, 0.43f),
        new Color(0.72f, 0.45f, 0.27f),
        new Color(0.45f, 0.27f, 0.12f),
    };
    private static readonly Color[] HairColors = {
        new Color(0.08f, 0.05f, 0.02f),
        new Color(0.28f, 0.16f, 0.06f),
        new Color(0.55f, 0.32f, 0.08f),
        new Color(0.85f, 0.65f, 0.20f),
        new Color(0.75f, 0.30f, 0.12f),
        new Color(0.75f, 0.75f, 0.75f),
    };

    private int _typeIndex = 0;
    private int _skinIndex = 2;
    private int _hairIndex = 0;

    private LineEdit      _nameInput    = null!;
    private Label         _typeLabel    = null!;
    private Label         _typeDesc     = null!;
    private Label         _errorLabel   = null!;
    private HBoxContainer _skinSwatches = null!;
    private HBoxContainer _hairSwatches = null!;

    public override void _Ready()
    {
        _nameInput    = GetNode<LineEdit>("%NameInput");
        _typeLabel    = GetNode<Label>("%TypeLabel");
        _typeDesc     = GetNode<Label>("%TypeDesc");
        _errorLabel   = GetNode<Label>("%ErrorLabel");
        _skinSwatches = GetNode<HBoxContainer>("%SkinSwatches");
        _hairSwatches = GetNode<HBoxContainer>("%HairSwatches");

        GetNode<Button>("%BtnPrevType").Pressed += () => _CycleType(-1);
        GetNode<Button>("%BtnNextType").Pressed += () => _CycleType(+1);
        GetNode<Button>("%BtnStart").Pressed    += _OnStart;
        GetNode<Button>("%BtnBack").Pressed     += _OnBack;

        _skinIndex = PlayerData.Instance.SkinColorIndex;
        _hairIndex = PlayerData.Instance.HairColorIndex;
        _typeIndex = System.Array.IndexOf(TypeKeys, PlayerData.Instance.Position);
        if (_typeIndex < 0) _typeIndex = 0;

        _BuildSwatches(_skinSwatches, SkinColors, true);
        _BuildSwatches(_hairSwatches, HairColors, false);
        _UpdateType();

        if (!PlayerData.Instance.IsNew)
            CallDeferred(MethodName._GoToWorldMap);
    }

    // ─── Swatch helpers ────────────────────────────────────────────

    private void _BuildSwatches(HBoxContainer container, Color[] colors, bool isSkin)
    {
        int selected = isSkin ? _skinIndex : _hairIndex;
        for (int i = 0; i < colors.Length; i++)
        {
            int idx = i;
            var btn = new Button();
            btn.CustomMinimumSize = new Vector2(38, 38);
            btn.AddThemeStyleboxOverride("normal",  _SwatchSF(colors[i], false));
            btn.AddThemeStyleboxOverride("hover",   _SwatchSF(colors[i].Lightened(0.2f), false));
            btn.AddThemeStyleboxOverride("pressed", _SwatchSF(colors[i], true));
            btn.Pressed += () =>
            {
                if (isSkin) { _skinIndex = idx; _RefreshSwatches(_skinSwatches, SkinColors, _skinIndex); }
                else        { _hairIndex = idx; _RefreshSwatches(_hairSwatches, HairColors, _hairIndex); }
            };
            container.AddChild(btn);
        }
        _RefreshSwatches(container, colors, selected);
    }

    private void _RefreshSwatches(HBoxContainer container, Color[] colors, int selected)
    {
        var children = container.GetChildren();
        for (int i = 0; i < children.Count && i < colors.Length; i++)
        {
            if (children[i] is Button btn)
                btn.AddThemeStyleboxOverride("normal", _SwatchSF(colors[i], i == selected));
        }
    }

    private static StyleBoxFlat _SwatchSF(Color color, bool selected)
    {
        var sf = new StyleBoxFlat();
        sf.BgColor = color;
        sf.CornerRadiusTopLeft = sf.CornerRadiusTopRight =
        sf.CornerRadiusBottomLeft = sf.CornerRadiusBottomRight = 5;
        sf.BorderColor = selected ? Colors.White : new Color(0.4f, 0.4f, 0.4f, 0.5f);
        sf.BorderWidthTop = sf.BorderWidthBottom =
        sf.BorderWidthLeft = sf.BorderWidthRight = selected ? 3 : 1;
        return sf;
    }

    // ─── Type cycling ──────────────────────────────────────────────

    private void _CycleType(int dir)
    {
        _typeIndex = (_typeIndex + dir + TypeKeys.Length) % TypeKeys.Length;
        _UpdateType();
    }

    private void _UpdateType()
    {
        _typeLabel.Text = TypeNames[_typeIndex];
        _typeDesc.Text  = TypeDescs[_typeIndex];
    }

    // ─── Button handlers ───────────────────────────────────────────

    private void _OnStart()
    {
        string name = _nameInput.Text.Trim();
        if (name.Length < 2)
        {
            _errorLabel.Text = "En az 2 karakter gir.";
            return;
        }
        _errorLabel.Text = "";

        PlayerData.Instance.PlayerName     = name;
        PlayerData.Instance.Position       = TypeKeys[_typeIndex];
        PlayerData.Instance.SkinColorIndex = _skinIndex;
        PlayerData.Instance.HairColorIndex = _hairIndex;
        PlayerData.Instance.Save();

        _ApplyStartingStats();
        WorldManager.Instance.GoTo("WorldMap");
    }

    private void _OnBack()
    {
        // İlk ekran — geri yok; hata mesajını temizle
        _errorLabel.Text = "";
        _nameInput.Text  = "";
        _typeIndex = 0;
        _UpdateType();
    }

    private void _GoToWorldMap() => WorldManager.Instance.GoTo("WorldMap");

    private void _ApplyStartingStats()
    {
        var gm = GameManager.Instance;
        switch (TypeKeys[_typeIndex])
        {
            case "FW": gm.ShotPower += 3; gm.Sprint    += 2; break;
            case "MF": gm.Technique += 3; gm.FootballIQ += 2; break;
            case "DF": gm.Stamina   += 3; gm.Technique  += 1; break;
        }
    }
}
