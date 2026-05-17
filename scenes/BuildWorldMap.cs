using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildWorldMap.cs
public partial class BuildWorldMap : SceneBuilderBase
{
    // World bounds: 2048 x 1440
    private const float W = 2048f;
    private const float H = 1440f;

    public override void _Initialize()
    {
        GD.Print("Generating: WorldMap");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "WorldMap";
        temp.AddChild(root);

        // --- Ground ---
        var ground = new ColorRect();
        ground.Name = "Ground";
        ground.Color = new Color(0.22f, 0.38f, 0.18f); // dark green
        ground.Position = Vector2.Zero;
        ground.Size = new Vector2(W, H);
        ground.ZIndex = -10;
        root.AddChild(ground);

        // --- Roads (tan/brown) ---
        _AddColorRect(root, "RoadH", new Color(0.55f, 0.48f, 0.35f), new Vector2(0, 660), new Vector2(W, 80), -9);
        _AddColorRect(root, "RoadV", new Color(0.55f, 0.48f, 0.35f), new Vector2(920, 0), new Vector2(80, H), -9);

        // --- Buildings + zones ---
        // Home (Yıldız'ın Evi)
        _AddBuilding(root, "HomeLabel", "Ev", new Color(0.6f, 0.3f, 0.15f), new Vector2(200, 400), new Vector2(180, 150));
        _AddZone(root, "HomeInterior", "Yıldız'ın Evi",  new Vector2(290, 550), 70);

        // Bakkal
        _AddBuilding(root, "BakkalLabel", "Bakkal", new Color(0.7f, 0.55f, 0.2f), new Vector2(500, 180), new Vector2(160, 130));
        _AddZone(root, "BakkalInterior", "Rıza Bakkal", new Vector2(580, 310), 70);

        // Mahalle Sahası
        _AddBuilding(root, "SahaLabel", "Mahalle\nSahası", new Color(0.25f, 0.55f, 0.25f), new Vector2(1100, 380), new Vector2(260, 200));
        _AddZone(root, "Match", "Mahalle Sahası", new Vector2(1230, 480), 90);

        // Çay Bahçesi
        _AddBuilding(root, "CayLabel", "Çay\nBahçesi", new Color(0.3f, 0.55f, 0.35f), new Vector2(680, 800), new Vector2(200, 150));
        _AddZone(root, "CayBahcesi", "Çınar Çay Bahçesi", new Vector2(780, 875), 80);

        // Fitness Salonu
        _AddBuilding(root, "FitnessLabel", "Fitness\nSalonu", new Color(0.45f, 0.25f, 0.55f), new Vector2(200, 820), new Vector2(180, 130));
        _AddZone(root, "Fitness", "Fitness Salonu", new Vector2(290, 885), 70);

        // Spor Tesisi
        _AddBuilding(root, "SporLabel", "Spor\nTesisi", new Color(0.2f, 0.35f, 0.65f), new Vector2(1400, 220), new Vector2(220, 170));
        _AddZone(root, "SporTesisi", "Yıldız Spor Tesisleri", new Vector2(1510, 390), 80);

        // Okul (yakında açılacak — zone yok)
        _AddBuilding(root, "OkulLabel", "İlkokul", new Color(0.65f, 0.2f, 0.2f), new Vector2(450, 820), new Vector2(180, 130));

        // İskele (yakında açılacak — zone yok)
        _AddBuilding(root, "IskeleLabel", "İskele", new Color(0.2f, 0.5f, 0.65f), new Vector2(1600, 700), new Vector2(200, 100));

        // --- World boundary walls ---
        _AddWall(root, "WallTop",    new Vector2(0, -20),  new Vector2(W, 20));
        _AddWall(root, "WallBottom", new Vector2(0, H),    new Vector2(W, 20));
        _AddWall(root, "WallLeft",   new Vector2(-20, 0),  new Vector2(20, H));
        _AddWall(root, "WallRight",  new Vector2(W, 0),    new Vector2(20, H));

        // --- Player ---
        var player = new CharacterBody2D();
        player.Name = "Player";
        player.Position = new Vector2(290, 490); // near home

        var playerCol = new CollisionShape2D();
        playerCol.Name = "Collision";
        var capsule = new CapsuleShape2D();
        capsule.Radius = 18f;
        capsule.Height = 36f;
        playerCol.Shape = capsule;
        player.AddChild(playerCol);

        var playerSprite = new Sprite2D();
        playerSprite.Name = "Sprite";
        playerSprite.Texture = GD.Load<Texture2D>("res://assets/img/player_sprite.png");
        playerSprite.Scale = new Vector2(0.22f, 0.22f);
        playerSprite.Position = new Vector2(0, -10);
        player.AddChild(playerSprite);

        var cam = new Camera2D();
        cam.Name = "Camera2D";
        cam.Enabled = true;
        cam.LimitLeft   = 0;
        cam.LimitTop    = 0;
        cam.LimitRight  = (int)W;
        cam.LimitBottom = (int)H;
        player.AddChild(cam);

        root.AddChild(player);

        // --- HUD ---
        var hud = new CanvasLayer();
        hud.Name = "HUD";
        hud.Layer = 10;
        root.AddChild(hud);

        // Time bar (top)
        var hudBg = new ColorRect();
        hudBg.Name = "HUDBg";
        hudBg.Color = new Color(0, 0, 0, 0.55f);
        hudBg.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        hudBg.Size = new Vector2(1280, 38);
        hud.AddChild(hudBg);

        var timeLabel = new Label();
        timeLabel.Name = "TimeLabel";
        timeLabel.UniqueNameInOwner = true;
        timeLabel.Text = "Gün 1   07:30   Sabah";
        timeLabel.Position = new Vector2(10, 8);
        timeLabel.AddThemeFontSizeOverride("font_size", 16);
        timeLabel.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.7f));
        hud.AddChild(timeLabel);

        var playerLabel = new Label();
        playerLabel.Name = "PlayerLabel";
        playerLabel.UniqueNameInOwner = true;
        playerLabel.Text = "";
        playerLabel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        playerLabel.Position = new Vector2(-200, 8);
        playerLabel.AddThemeFontSizeOverride("font_size", 16);
        playerLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        hud.AddChild(playerLabel);

        // Interact prompt (bottom center)
        var promptLabel = new Label();
        promptLabel.Name = "PromptLabel";
        promptLabel.UniqueNameInOwner = true;
        promptLabel.Text = "[E]  Eve  →  Gir";
        promptLabel.Visible = false;
        promptLabel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
        promptLabel.Position = new Vector2(0, -50);
        promptLabel.AddThemeFontSizeOverride("font_size", 20);
        promptLabel.AddThemeColorOverride("font_color", Colors.White);
        promptLabel.AddThemeColorOverride("font_shadow_color", Colors.Black);
        hud.AddChild(promptLabel);

        // --- Mini location labels on map ---
        // (Already done via building labels above)

        root.SetScript(GD.Load("res://scripts/WorldMapScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/WorldMap.tscn");
    }

    private void _AddColorRect(Node parent, string name, Color color, Vector2 pos, Vector2 size, int z = 0)
    {
        var r = new ColorRect();
        r.Name = name;
        r.Color = color;
        r.Position = pos;
        r.Size = size;
        r.ZIndex = z;
        parent.AddChild(r);
    }

    private void _AddBuilding(Node parent, string labelName, string text, Color color, Vector2 pos, Vector2 size)
    {
        var r = new ColorRect();
        r.Name = labelName + "Rect";
        r.Color = color;
        r.Position = pos;
        r.Size = size;
        r.ZIndex = 0;
        parent.AddChild(r);

        var lbl = new Label();
        lbl.Name = labelName;
        lbl.Text = text;
        lbl.Position = pos + new Vector2(8, 8);
        lbl.AddThemeFontSizeOverride("font_size", 14);
        lbl.AddThemeColorOverride("font_color", Colors.White);
        lbl.ZIndex = 1;
        parent.AddChild(lbl);

        // Simple StaticBody2D wall for the building
        var wall = new StaticBody2D();
        wall.Name = labelName + "Wall";
        var col = new CollisionShape2D();
        var shape = new RectangleShape2D();
        shape.Size = size;
        col.Shape = shape;
        col.Position = pos + size / 2f;
        wall.AddChild(col);
        parent.AddChild(wall);
    }

    private void _AddZone(Node parent, string locationKey, string displayName, Vector2 center, float radius)
    {
        // Ensure a Zones node exists
        Node zones;
        if (parent.HasNode("Zones"))
        {
            zones = parent.GetNode("Zones");
        }
        else
        {
            zones = new Node2D();
            zones.Name = "Zones";
            parent.AddChild(zones);
        }

        var area = new Area2D();
        area.Name = displayName.Replace(" ", "").Replace("'", "");
        area.Position = center;
        area.SetMeta("location", locationKey);

        var col = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = radius;
        col.Shape = circle;
        area.AddChild(col);

        zones.AddChild(area);
    }

    private void _AddWall(Node parent, string name, Vector2 pos, Vector2 size)
    {
        var wall = new StaticBody2D();
        wall.Name = name;
        var col = new CollisionShape2D();
        var shape = new RectangleShape2D();
        shape.Size = size;
        col.Shape = shape;
        col.Position = pos + size / 2f;
        wall.AddChild(col);
        parent.AddChild(wall);
    }
}
