using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildTitleScreen.cs
/// Ana menü: Yeni Oyun / Oyun Yükle / Çıkış
public partial class BuildTitleScreen : SceneBuilderBase
{
    private const float W = 1280f, H = 720f;

    public override void _Initialize()
    {
        GD.Print("Generating: TitleScreen");
        var temp = new Node();
        var root = new TitleScreenScene();
        root.Name = "TitleScreen";
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        temp.AddChild(root);

        // ── Arka plan ───────────────────────────────────────────
        var bg = new TextureRect();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/neighborhood_bg.png");
        bg.StretchMode = TextureRect.StretchModeEnum.Scale;
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bg.ZIndex = -10;
        root.AddChild(bg);

        // Gradient overlay (alt koyulaşır)
        var ov = new ColorRect();
        ov.Color = new Color(0f, 0f, 0f, 0.48f);
        ov.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        ov.ZIndex = -9;
        root.AddChild(ov);

        // ── Sol: kahraman sprite ─────────────────────────────────
        var hero = new TextureRect();
        hero.Texture = GD.Load<Texture2D>("res://assets/img/hero_sprite.png");
        hero.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        hero.Position = new Vector2(120f, H / 2f - 180f);
        hero.Size = new Vector2(360f, 360f);
        hero.ZIndex = 1;
        root.AddChild(hero);

        // ── Merkez panel ─────────────────────────────────────────
        float pw = 440f, ph = 560f;
        float px = (W - pw) / 2f + 60f;   // biraz sağa kaydır
        float py = (H - ph) / 2f;

        var panel = new Panel();
        panel.Position = new Vector2(px, py);
        panel.Size     = new Vector2(pw, ph);
        var sf = new StyleBoxFlat();
        sf.BgColor = new Color(0.08f, 0.06f, 0.03f, 0.88f);
        sf.CornerRadiusTopLeft = sf.CornerRadiusTopRight =
        sf.CornerRadiusBottomLeft = sf.CornerRadiusBottomRight = 14;
        sf.BorderColor = new Color(0.70f, 0.55f, 0.25f, 0.90f);
        sf.BorderWidthTop = sf.BorderWidthRight = sf.BorderWidthBottom = sf.BorderWidthLeft = 2;
        panel.AddThemeStyleboxOverride("panel", sf);
        root.AddChild(panel);

        var mc = new MarginContainer();
        mc.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        mc.AddThemeConstantOverride("margin_left",   28);
        mc.AddThemeConstantOverride("margin_right",  28);
        mc.AddThemeConstantOverride("margin_top",    28);
        mc.AddThemeConstantOverride("margin_bottom", 28);
        panel.AddChild(mc);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 14);
        mc.AddChild(vbox);

        // Logo
        var logo = new Label();
        logo.Text = "YILDIZ\nKRAMPON";
        logo.HorizontalAlignment = HorizontalAlignment.Center;
        logo.AddThemeFontSizeOverride("font_size", 44);
        logo.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.10f));
        vbox.AddChild(logo);

        var sub = new Label();
        sub.Text = "PIXEL PITCH";
        sub.HorizontalAlignment = HorizontalAlignment.Center;
        sub.AddThemeFontSizeOverride("font_size", 16);
        sub.AddThemeColorOverride("font_color", new Color(0.90f, 0.72f, 0.35f));
        vbox.AddChild(sub);

        // Dekoratif çizgi
        var sep = new HSeparator();
        sep.AddThemeColorOverride("separator_color", new Color(0.70f, 0.55f, 0.25f, 0.60f));
        vbox.AddChild(sep);

        // Boşluk
        var sp1 = new Control(); sp1.CustomMinimumSize = new Vector2(0, 8);
        vbox.AddChild(sp1);

        // Alıntı / sloган
        var tagline = new Label();
        tagline.Text = "\"Mahallede başlar, dünyada biter.\"";
        tagline.HorizontalAlignment = HorizontalAlignment.Center;
        tagline.AddThemeFontSizeOverride("font_size", 14);
        tagline.AddThemeColorOverride("font_color", new Color(0.80f, 0.85f, 0.75f));
        tagline.AutowrapMode = TextServer.AutowrapMode.Word;
        vbox.AddChild(tagline);

        // Esnek boşluk
        var sp2 = new Control();
        sp2.SizeFlagsVertical = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        vbox.AddChild(sp2);

        // ── Butonlar ────────────────────────────────────────────
        var btnNew = _Btn("⚽  YENİ OYUN",  "BtnNewGame",
            new Color(0.12f, 0.42f, 0.12f), new Color(0.40f, 0.80f, 0.32f));
        vbox.AddChild(btnNew);

        var btnLoad = _Btn("📂  OYUN YÜKLE", "BtnLoadGame",
            new Color(0.18f, 0.28f, 0.50f), new Color(0.40f, 0.58f, 0.90f));
        vbox.AddChild(btnLoad);

        var btnExit = _Btn("✖  ÇIKIŞ",      "BtnExit",
            new Color(0.38f, 0.10f, 0.08f), new Color(0.70f, 0.25f, 0.18f), fontSize: 16);
        btnExit.CustomMinimumSize = new Vector2(0, 42);
        vbox.AddChild(btnExit);

        // Versiyon etiketi
        var ver = new Label();
        ver.Name = "VersionLabel";
        ver.Text = "v0.1 — Alpha";
        ver.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        ver.Position = new Vector2(-120f, -28f);
        ver.AddThemeFontSizeOverride("font_size", 12);
        ver.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f, 0.7f));
        root.AddChild(ver);

        temp.RemoveChild(root);
        temp.Free();
        PackAndSave(root, "res://scenes/TitleScreen.tscn");
    }

    private Button _Btn(string text, string name, Color bg, Color border, int fontSize = 20)
    {
        var btn = new Button();
        btn.Name = name; btn.UniqueNameInOwner = true;
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(0, 56);
        btn.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        var sf = new StyleBoxFlat();
        sf.BgColor = bg;
        sf.CornerRadiusTopLeft = sf.CornerRadiusTopRight =
        sf.CornerRadiusBottomLeft = sf.CornerRadiusBottomRight = 8;
        sf.BorderColor = border;
        sf.BorderWidthTop = sf.BorderWidthRight = sf.BorderWidthBottom = sf.BorderWidthLeft = 2;
        var sfH = (StyleBoxFlat)sf.Duplicate();
        sfH.BgColor = bg.Lightened(0.18f);
        btn.AddThemeStyleboxOverride("normal",  sf);
        btn.AddThemeStyleboxOverride("hover",   sfH);
        btn.AddThemeStyleboxOverride("pressed", sf);
        btn.AddThemeStyleboxOverride("disabled", sf);
        btn.AddThemeColorOverride("font_color",          Colors.White);
        btn.AddThemeColorOverride("font_hover_color",    new Color(1f, 0.95f, 0.70f));
        btn.AddThemeColorOverride("font_disabled_color", new Color(0.5f, 0.5f, 0.5f));
        btn.AddThemeFontSizeOverride("font_size", fontSize);
        return btn;
    }
}
