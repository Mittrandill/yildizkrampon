using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildCharacterCreation.cs
public partial class BuildCharacterCreation : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: CharacterCreation");

        var temp = new Node();
        var root = new Control();
        root.Name = "CharacterCreation";
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        temp.AddChild(root);

        // Background
        var bg = new Sprite2D();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/bedroom_bg.png");
        bg.Position = new Vector2(640, 360);
        bg.ZIndex = -1;
        root.AddChild(bg);

        // Dark overlay
        var overlay = new ColorRect();
        overlay.Name = "Overlay";
        overlay.Color = new Color(0, 0, 0, 0.65f);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(overlay);

        // Center panel
        var panel = new PanelContainer();
        panel.Name = "Panel";
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.Position = new Vector2(390, 100);
        panel.Size = new Vector2(500, 520);
        root.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.Name = "VBox";
        vbox.AddThemeConstantOverride("separation", 18);
        panel.AddChild(vbox);

        // Game title
        var gameTitle = new Label();
        gameTitle.Name = "GameTitle";
        gameTitle.Text = "YILDIZ KRAMPON";
        gameTitle.HorizontalAlignment = HorizontalAlignment.Center;
        gameTitle.AddThemeFontSizeOverride("font_size", 28);
        gameTitle.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        vbox.AddChild(gameTitle);

        var subtitle = new Label();
        subtitle.Name = "Subtitle";
        subtitle.Text = "Karakterini Oluştur";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AddThemeFontSizeOverride("font_size", 16);
        subtitle.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        vbox.AddChild(subtitle);

        // Separator
        var sep1 = new HSeparator();
        vbox.AddChild(sep1);

        // Name section
        var nameLabel = new Label();
        nameLabel.Text = "Karakterinin Adı:";
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(nameLabel);

        var nameInput = new LineEdit();
        nameInput.Name = "NameInput";
        nameInput.UniqueNameInOwner = true;
        nameInput.PlaceholderText = "Adını gir...";
        nameInput.MaxLength = 16;
        nameInput.CustomMinimumSize = new Vector2(0, 40);
        nameInput.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(nameInput);

        // Position section
        var posLabel = new Label();
        posLabel.Text = "Pozisyon Seç:";
        posLabel.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(posLabel);

        var hbox = new HBoxContainer();
        hbox.Name = "PositionButtons";
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        hbox.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(hbox);

        var btnFW = new Button();
        btnFW.Name = "BtnFW";
        btnFW.UniqueNameInOwner = true;
        btnFW.Text = "FW\nForvet";
        btnFW.CustomMinimumSize = new Vector2(120, 60);
        btnFW.AddThemeFontSizeOverride("font_size", 14);
        hbox.AddChild(btnFW);

        var btnMF = new Button();
        btnMF.Name = "BtnMF";
        btnMF.UniqueNameInOwner = true;
        btnMF.Text = "MF\nOrta Saha";
        btnMF.CustomMinimumSize = new Vector2(120, 60);
        btnMF.AddThemeFontSizeOverride("font_size", 14);
        hbox.AddChild(btnMF);

        var btnDF = new Button();
        btnDF.Name = "BtnDF";
        btnDF.UniqueNameInOwner = true;
        btnDF.Text = "DF\nDefans";
        btnDF.CustomMinimumSize = new Vector2(120, 60);
        btnDF.AddThemeFontSizeOverride("font_size", 14);
        hbox.AddChild(btnDF);

        // Position description
        var posDesc = new Label();
        posDesc.Name = "PosDesc";
        posDesc.UniqueNameInOwner = true;
        posDesc.Text = "Forvet — Hızlı, golcü. Şut Gücü ve Sprint +bonus ile başlar.";
        posDesc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        posDesc.CustomMinimumSize = new Vector2(0, 50);
        posDesc.AddThemeFontSizeOverride("font_size", 13);
        posDesc.AddThemeColorOverride("font_color", new Color(0.85f, 0.9f, 1f));
        vbox.AddChild(posDesc);

        // Error label
        var errorLabel = new Label();
        errorLabel.Name = "ErrorLabel";
        errorLabel.UniqueNameInOwner = true;
        errorLabel.Text = "";
        errorLabel.HorizontalAlignment = HorizontalAlignment.Center;
        errorLabel.AddThemeColorOverride("font_color", new Color(1f, 0.4f, 0.4f));
        errorLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(errorLabel);

        // Start button
        var btnStart = new Button();
        btnStart.Name = "BtnStart";
        btnStart.UniqueNameInOwner = true;
        btnStart.Text = "Oyuna Başla";
        btnStart.CustomMinimumSize = new Vector2(0, 50);
        btnStart.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(btnStart);

        root.SetScript(GD.Load("res://scripts/CharacterCreationScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/CharacterCreation.tscn");
    }
}
