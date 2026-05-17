using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildCharacterCreation.cs
/// Konsept: iki panel — sol karakter önizleme, sag ozellestime + BASLA/GERI
public partial class BuildCharacterCreation : SceneBuilderBase
{
    private const float W    = 1280f, H = 720f;
    private const float PAD  = 18f;
    private const float LP_W = 360f;
    private const float RP_X = PAD + LP_W + PAD;
    private const float RP_W = W - RP_X - PAD;
    private const float PH   = H - 2 * PAD;

    public override void _Initialize()
    {
        GD.Print("Generating: CharacterCreation (two-panel)");
        var temp = new Node();
        var root = new CharacterCreationScene();
        root.Name = "CharacterCreation";
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        temp.AddChild(root);

        _AddBackground(root);
        _AddLeftPanel(root);
        _AddRightPanel(root);

        temp.RemoveChild(root);
        temp.Free();
        PackAndSave(root, "res://scenes/CharacterCreation.tscn");
    }

    // ─── Background ────────────────────────────────────────────────

    private void _AddBackground(Control root)
    {
        var bg = new TextureRect();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/neighborhood_bg.png");
        bg.StretchMode = TextureRect.StretchModeEnum.Scale;
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bg.ZIndex = -10;
        root.AddChild(bg);

        var ov = new ColorRect();
        ov.Name = "Overlay";
        ov.Color = new Color(0f, 0f, 0f, 0.52f);
        ov.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        ov.ZIndex = -9;
        root.AddChild(ov);
    }

    // ─── Left Panel ────────────────────────────────────────────────

    private void _AddLeftPanel(Control root)
    {
        var panel = _MakePanel(new Vector2(PAD, PAD), new Vector2(LP_W, PH));
        root.AddChild(panel);

        var (margin, vbox) = _PanelVBox(panel, 14);

        // Title
        var title = _Label("YILDIZ\nKRAMPON", 32, new Color(1f, 0.85f, 0.15f));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var sub = _Label("PIXEL PITCH", 13, new Color(0.85f, 0.70f, 0.35f));
        sub.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(sub);

        vbox.AddChild(_Sep());

        // Character preview frame
        var frame = new Panel();
        frame.CustomMinimumSize = new Vector2(0, 280);
        frame.SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        var frameSF = _PanelSF();
        frameSF.BgColor = new Color(0.05f, 0.14f, 0.05f, 0.92f);
        frameSF.BorderColor = new Color(0.35f, 0.65f, 0.30f, 0.80f);
        frame.AddThemeStyleboxOverride("panel", frameSF);
        vbox.AddChild(frame);

        var charTex = new TextureRect();
        charTex.Name = "CharSprite";
        charTex.UniqueNameInOwner = true;
        charTex.Texture = GD.Load<Texture2D>("res://assets/img/hero_sprite.png");
        charTex.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        charTex.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        frame.AddChild(charTex);

        // Dots (visual only)
        var dotsRow = new HBoxContainer();
        dotsRow.Alignment = BoxContainer.AlignmentMode.Center;
        dotsRow.AddThemeConstantOverride("separation", 6);
        for (int i = 0; i < 5; i++)
        {
            var dot = new ColorRect();
            dot.CustomMinimumSize = new Vector2(10, 10);
            dot.Color = i == 0 ? new Color(1f, 0.85f, 0.15f) : new Color(0.5f, 0.5f, 0.5f, 0.6f);
            vbox.AddChild(dot); // placeholder, shown after frame
        }
        // Remove the individual dots and add the row instead
        for (int i = 0; i < 5; i++) vbox.GetChild(vbox.GetChildCount() - 1).QueueFree();
        vbox.AddChild(dotsRow);
        for (int i = 0; i < 5; i++)
        {
            var dot = new ColorRect();
            dot.CustomMinimumSize = new Vector2(10, 10);
            dot.Color = i == 0 ? new Color(1f, 0.85f, 0.15f) : new Color(0.45f, 0.45f, 0.45f, 0.6f);
            dotsRow.AddChild(dot);
        }

        // Bottom description
        var desc = _Label("Mahallede top seninle güzel!\nKendi tarzını seç, sahada ve hayatta iz bırak!", 13, new Color(0.80f, 0.85f, 0.75f));
        desc.Name = "DescLabel";
        desc.UniqueNameInOwner = true;
        desc.AutowrapMode = TextServer.AutowrapMode.Word;
        desc.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(desc);
    }

    // ─── Right Panel ───────────────────────────────────────────────

    private void _AddRightPanel(Control root)
    {
        var panel = _MakePanel(new Vector2(RP_X, PAD), new Vector2(RP_W, PH));
        root.AddChild(panel);

        var (margin, vbox) = _PanelVBox(panel, 14);
        vbox.AddThemeConstantOverride("separation", 12);

        // İsim
        var (nameRow, _) = _Row(vbox, "İsim");
        var nameInput = new LineEdit();
        nameInput.Name = "NameInput"; nameInput.UniqueNameInOwner = true;
        nameInput.PlaceholderText = "Adını gir...";
        nameInput.MaxLength = 16;
        nameInput.CustomMinimumSize = new Vector2(0, 40);
        nameInput.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        nameInput.AddThemeFontSizeOverride("font_size", 16);
        nameRow.AddChild(nameInput);

        vbox.AddChild(_Sep());

        // Karakter Tipi
        var (typeRow, _) = _Row(vbox, "Karakter Tipi");
        var btnPrev = _ArrowBtn("◄", "BtnPrevType");
        typeRow.AddChild(btnPrev);

        var typeLabel = _Label("Hızlı Kanat", 17, new Color(1f, 0.90f, 0.45f));
        typeLabel.Name = "TypeLabel"; typeLabel.UniqueNameInOwner = true;
        typeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        typeLabel.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        typeRow.AddChild(typeLabel);

        var btnNext = _ArrowBtn("►", "BtnNextType");
        typeRow.AddChild(btnNext);

        var typeDesc = _Label("Hızlı, golcü. Şut Gücü ve Sprint +bonus ile başlar.", 13, new Color(0.72f, 0.85f, 0.95f));
        typeDesc.Name = "TypeDesc"; typeDesc.UniqueNameInOwner = true;
        typeDesc.AutowrapMode = TextServer.AutowrapMode.Word;
        vbox.AddChild(typeDesc);

        vbox.AddChild(_Sep());

        // Ten Rengi
        var (skinRow, _) = _Row(vbox, "Ten Rengi");
        var skinSwatches = new HBoxContainer();
        skinSwatches.Name = "SkinSwatches"; skinSwatches.UniqueNameInOwner = true;
        skinSwatches.AddThemeConstantOverride("separation", 8);
        skinSwatches.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        skinRow.AddChild(skinSwatches);

        // Saç Rengi
        var (hairRow, _) = _Row(vbox, "Saç Rengi");
        var hairSwatches = new HBoxContainer();
        hairSwatches.Name = "HairSwatches"; hairSwatches.UniqueNameInOwner = true;
        hairSwatches.AddThemeConstantOverride("separation", 8);
        hairSwatches.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        hairRow.AddChild(hairSwatches);

        vbox.AddChild(_Sep());

        // Error label
        var err = _Label("", 13, new Color(1f, 0.38f, 0.38f));
        err.Name = "ErrorLabel"; err.UniqueNameInOwner = true;
        err.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(err);

        // Spacer
        var sp = new Control();
        sp.SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        vbox.AddChild(sp);

        // Bottom hint
        var hint = _Label("⚽  Hayalini kurduğun futbol hayatı seni bekliyor.  ★", 13, new Color(0.85f, 0.80f, 0.50f));
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(hint);

        // BAŞLA / GERİ
        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 16);
        btnRow.CustomMinimumSize = new Vector2(0, 58);
        vbox.AddChild(btnRow);

        var btnStart = _BigBtn("⚽  BAŞLA", "BtnStart", new Color(0.12f, 0.42f, 0.12f), new Color(0.38f, 0.80f, 0.32f));
        btnRow.AddChild(btnStart);

        var btnBack = _BigBtn("◄  GERİ", "BtnBack", new Color(0.42f, 0.12f, 0.08f), new Color(0.75f, 0.28f, 0.18f));
        btnRow.AddChild(btnBack);
    }

    // ─── Helpers ───────────────────────────────────────────────────

    private Panel _MakePanel(Vector2 pos, Vector2 size)
    {
        var panel = new Panel();
        panel.Position = pos; panel.Size = size;
        panel.AddThemeStyleboxOverride("panel", _PanelSF());
        return panel;
    }

    private StyleBoxFlat _PanelSF()
    {
        var sf = new StyleBoxFlat();
        sf.BgColor = new Color(0.10f, 0.07f, 0.04f, 0.88f);
        sf.CornerRadiusTopLeft = sf.CornerRadiusTopRight =
        sf.CornerRadiusBottomLeft = sf.CornerRadiusBottomRight = 10;
        sf.BorderColor = new Color(0.65f, 0.50f, 0.28f, 0.80f);
        sf.BorderWidthTop = sf.BorderWidthRight = sf.BorderWidthBottom = sf.BorderWidthLeft = 2;
        return sf;
    }

    private (MarginContainer margin, VBoxContainer vbox) _PanelVBox(Panel panel, int margin)
    {
        var mc = new MarginContainer();
        mc.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        mc.AddThemeConstantOverride("margin_left",   margin);
        mc.AddThemeConstantOverride("margin_right",  margin);
        mc.AddThemeConstantOverride("margin_top",    margin);
        mc.AddThemeConstantOverride("margin_bottom", margin);
        panel.AddChild(mc);
        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 12);
        mc.AddChild(vb);
        return (mc, vb);
    }

    private Label _Label(string text, int size, Color color)
    {
        var l = new Label();
        l.Text = text;
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        return l;
    }

    private HSeparator _Sep()
    {
        var sep = new HSeparator();
        sep.AddThemeColorOverride("separator_color", new Color(0.65f, 0.50f, 0.28f, 0.40f));
        return sep;
    }

    private (HBoxContainer row, Label lbl) _Row(VBoxContainer parent, string labelText)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        row.CustomMinimumSize = new Vector2(0, 40);
        parent.AddChild(row);

        var lbl = _Label(labelText, 15, new Color(0.90f, 0.85f, 0.68f));
        lbl.CustomMinimumSize = new Vector2(140, 0);
        lbl.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(lbl);

        return (row, lbl);
    }

    private Button _ArrowBtn(string text, string name)
    {
        var btn = new Button();
        btn.Name = name; btn.UniqueNameInOwner = true;
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(38, 38);
        var sf = new StyleBoxFlat();
        sf.BgColor = new Color(0.22f, 0.15f, 0.07f);
        sf.CornerRadiusTopLeft = sf.CornerRadiusTopRight =
        sf.CornerRadiusBottomLeft = sf.CornerRadiusBottomRight = 6;
        sf.BorderColor = new Color(0.60f, 0.45f, 0.25f); sf.BorderWidthTop = sf.BorderWidthRight = sf.BorderWidthBottom = sf.BorderWidthLeft = 1;
        btn.AddThemeStyleboxOverride("normal", sf);
        btn.AddThemeColorOverride("font_color", Colors.White);
        btn.AddThemeFontSizeOverride("font_size", 14);
        return btn;
    }

    private Button _BigBtn(string text, string name, Color bg, Color border)
    {
        var btn = new Button();
        btn.Name = name; btn.UniqueNameInOwner = true;
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(0, 54);
        btn.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        var sf = new StyleBoxFlat();
        sf.BgColor = bg;
        sf.CornerRadiusTopLeft = sf.CornerRadiusTopRight =
        sf.CornerRadiusBottomLeft = sf.CornerRadiusBottomRight = 8;
        sf.BorderColor = border; sf.BorderWidthTop = sf.BorderWidthRight = sf.BorderWidthBottom = sf.BorderWidthLeft = 2;
        var sfH = (StyleBoxFlat)sf.Duplicate();
        sfH.BgColor = bg.Lightened(0.15f);
        btn.AddThemeStyleboxOverride("normal", sf);
        btn.AddThemeStyleboxOverride("hover",  sfH);
        btn.AddThemeStyleboxOverride("pressed", sf);
        btn.AddThemeColorOverride("font_color", Colors.White);
        btn.AddThemeFontSizeOverride("font_size", 20);
        return btn;
    }
}
