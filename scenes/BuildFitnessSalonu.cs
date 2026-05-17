using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildFitnessSalonu.cs
public partial class BuildFitnessSalonu : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: FitnessSalonu");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "FitnessSalonu";
        temp.AddChild(root);

        // Background (re-use neighborhood placeholder)
        var bgRect = new ColorRect();
        bgRect.Name = "Background";
        bgRect.Color = new Color(0.2f, 0.2f, 0.25f);
        bgRect.Position = Vector2.Zero;
        bgRect.Size = new Vector2(1280, 720);
        bgRect.ZIndex = -1;
        root.AddChild(bgRect);

        // Floor
        var floor = new ColorRect();
        floor.Name = "Floor";
        floor.Color = new Color(0.35f, 0.3f, 0.28f);
        floor.Position = new Vector2(0, 200);
        floor.Size = new Vector2(1280, 520);
        floor.ZIndex = -1;
        root.AddChild(floor);

        // Title banner
        var banner = new ColorRect();
        banner.Color = new Color(0.45f, 0.25f, 0.55f);
        banner.Position = Vector2.Zero;
        banner.Size = new Vector2(1280, 80);
        root.AddChild(banner);

        var roomLbl = new Label();
        roomLbl.Text = "Fitness Salonu";
        roomLbl.Position = new Vector2(20, 20);
        roomLbl.AddThemeFontSizeOverride("font_size", 24);
        roomLbl.AddThemeColorOverride("font_color", Colors.White);
        roomLbl.ZIndex = 2;
        root.AddChild(roomLbl);

        // Equipment visuals
        _Box(root, "Treadmill", new Color(0.5f,0.5f,0.6f), new Vector2(150, 250), new Vector2(200, 100));
        _Box(root, "Weights",   new Color(0.6f,0.4f,0.3f), new Vector2(450, 250), new Vector2(150, 100));
        _Box(root, "Bench",     new Color(0.4f,0.55f,0.4f), new Vector2(750, 250), new Vector2(200, 80));

        _Lbl(root, "LblTreadmill", "Koşu Bandı",  new Vector2(165, 265));
        _Lbl(root, "LblWeights",   "Ağırlıklar",  new Vector2(470, 265));
        _Lbl(root, "LblBench",     "Bench Press",  new Vector2(768, 265));

        _Wall(root, "WallTop",    Vector2.Zero,         new Vector2(1280, 20));
        _Wall(root, "WallBottom", new Vector2(0, 700),  new Vector2(1280, 20));
        _Wall(root, "WallLeft",   new Vector2(-20, 0),  new Vector2(20, 720));
        _Wall(root, "WallRight",  new Vector2(1280, 0), new Vector2(20, 720));

        var zones = new Node2D();
        zones.Name = "InteractZones";
        root.AddChild(zones);

        _Zone(zones, "ZoneCardio",  new Vector2(250, 400), 80, "Cardio",
              "[E] Kardiyo — 2 saat",
              "Kardiyo yaptın! Sprint +1, Dayanıklılık +1, Yorgunluk +20",
              120f, "Sprint:+1,Stamina:+1,Fatigue:+20,Energy:-15");

        _Zone(zones, "ZoneWeights", new Vector2(525, 400), 80, "WeightTraining",
              "[E] Ağırlık Antrenmanı — 2 saat",
              "Ağırlık çalıştın! Dayanıklılık +2, Yorgunluk +25",
              120f, "Stamina:+2,Fatigue:+25,Energy:-20");

        _Zone(zones, "ZoneExit", new Vector2(640, 660), 60, "Exit", "[E] Çık", "", 0f, "");

        root.AddChild(_MakePlayer(new Vector2(640, 500)));
        _MakeHUD(root);

        root.SetScript(GD.Load("res://scripts/SimpleInteriorScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/FitnessSalonu.tscn");
    }

    private static void _Box(Node p, string n, Color c, Vector2 pos, Vector2 size)
    {
        var r = new ColorRect(); r.Name = n; r.Color = c;
        r.Position = pos; r.Size = size; r.ZIndex = 1;
        p.AddChild(r);
    }
    private static void _Lbl(Node p, string n, string t, Vector2 pos)
    {
        var l = new Label(); l.Name = n; l.Text = t; l.Position = pos;
        l.AddThemeFontSizeOverride("font_size", 13);
        l.AddThemeColorOverride("font_color", Colors.White);
        l.ZIndex = 2; p.AddChild(l);
    }
    private static void _Zone(Node zones, string name, Vector2 pos, float radius,
        string action, string prompt, string feedback, float timeMins, string effects)
    {
        var area = new Area2D(); area.Name = name; area.Position = pos;
        area.SetMeta("action", action); area.SetMeta("prompt", prompt);
        area.SetMeta("feedback", feedback); area.SetMeta("time_min", timeMins);
        area.SetMeta("effects", effects);
        var col = new CollisionShape2D();
        var ci = new CircleShape2D(); ci.Radius = radius;
        col.Shape = ci; area.AddChild(col); zones.AddChild(area);
    }
    private static CharacterBody2D _MakePlayer(Vector2 pos)
    {
        var p = new CharacterBody2D(); p.Name = "Player"; p.Position = pos;
        var col = new CollisionShape2D();
        var cap = new CapsuleShape2D(); cap.Radius = 16f; cap.Height = 30f;
        col.Shape = cap; p.AddChild(col);
        var sp = new Sprite2D(); sp.Name = "Sprite";
        sp.Texture = GD.Load<Texture2D>("res://assets/img/player_sprite.png");
        sp.Scale = new Vector2(0.18f, 0.18f); sp.Position = new Vector2(0,-8);
        p.AddChild(sp); return p;
    }
    private static void _MakeHUD(Node root)
    {
        var hud = new CanvasLayer(); hud.Name = "HUD"; hud.Layer = 10;
        root.AddChild(hud);
        var bg = new ColorRect(); bg.Color = new Color(0,0,0,0.55f);
        bg.SetAnchorsPreset(Control.LayoutPreset.TopWide); bg.Size = new Vector2(1280,38);
        hud.AddChild(bg);
        var t = new Label(); t.Name = "TimeLabel"; t.UniqueNameInOwner = true;
        t.Text = "Gün 1   07:30   Sabah"; t.Position = new Vector2(10,8);
        t.AddThemeFontSizeOverride("font_size",16);
        t.AddThemeColorOverride("font_color", new Color(1f,0.95f,0.7f)); hud.AddChild(t);
        var p = new Label(); p.Name = "PromptLabel"; p.UniqueNameInOwner = true;
        p.Visible = false; p.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        p.HorizontalAlignment = HorizontalAlignment.Center; p.Position = new Vector2(0,-50);
        p.AddThemeFontSizeOverride("font_size",20);
        p.AddThemeColorOverride("font_color", Colors.White); hud.AddChild(p);
        var f = new Label(); f.Name = "FeedbackLabel"; f.UniqueNameInOwner = true;
        f.Visible = false; f.SetAnchorsPreset(Control.LayoutPreset.Center);
        f.Position = new Vector2(-220,-40); f.CustomMinimumSize = new Vector2(440,0);
        f.HorizontalAlignment = HorizontalAlignment.Center;
        f.AddThemeFontSizeOverride("font_size",18);
        f.AddThemeColorOverride("font_color", new Color(0.4f,1f,0.4f)); hud.AddChild(f);
    }
    private static void _Wall(Node parent, string name, Vector2 pos, Vector2 size)
    {
        var sb = new StaticBody2D(); sb.Name = name; sb.Position = pos + size/2f;
        var col = new CollisionShape2D();
        var sh = new RectangleShape2D(); sh.Size = size; col.Shape = sh;
        sb.AddChild(col); parent.AddChild(sb);
    }
}
