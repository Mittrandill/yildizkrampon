using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildSporTesisiInterior.cs
public partial class BuildSporTesisiInterior : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: SporTesisiInterior");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "SporTesisiInterior";
        temp.AddChild(root);

        var bg = new Sprite2D();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/training_bg.png");
        bg.Position = new Vector2(640, 360);
        bg.ZIndex = -1;
        root.AddChild(bg);

        var overlay = new ColorRect();
        overlay.Name = "Overlay";
        overlay.Color = new Color(0, 0, 0, 0.2f);
        overlay.Position = Vector2.Zero;
        overlay.Size = new Vector2(1280, 720);
        root.AddChild(overlay);

        var roomLbl = new Label();
        roomLbl.Name = "RoomLabel";
        roomLbl.Text = "Yıldız Spor Tesisleri";
        roomLbl.Position = new Vector2(20, 20);
        roomLbl.AddThemeFontSizeOverride("font_size", 20);
        roomLbl.AddThemeColorOverride("font_color", new Color(1f,0.85f,0.2f));
        roomLbl.ZIndex = 2;
        root.AddChild(roomLbl);

        // Kemal Hoca NPC
        var npc = new Sprite2D();
        npc.Name = "KemalHoca";
        npc.Texture = GD.Load<Texture2D>("res://assets/img/npc_blue.png");
        npc.Scale = new Vector2(0.18f, 0.18f);
        npc.Position = new Vector2(900, 280);
        npc.ZIndex = 2;
        root.AddChild(npc);

        var npcLbl = new Label();
        npcLbl.Name = "KemalLbl";
        npcLbl.Text = "Kemal Hoca";
        npcLbl.Position = new Vector2(860, 230);
        npcLbl.AddThemeFontSizeOverride("font_size", 13);
        npcLbl.AddThemeColorOverride("font_color", Colors.White);
        npcLbl.ZIndex = 3;
        root.AddChild(npcLbl);

        _Wall(root, "WallTop",    Vector2.Zero,         new Vector2(1280, 20));
        _Wall(root, "WallBottom", new Vector2(0, 700),  new Vector2(1280, 20));
        _Wall(root, "WallLeft",   new Vector2(-20, 0),  new Vector2(20, 720));
        _Wall(root, "WallRight",  new Vector2(1280, 0), new Vector2(20, 720));

        var zones = new Node2D();
        zones.Name = "InteractZones";
        root.AddChild(zones);

        _Zone(zones, "ZoneShot",  new Vector2(400, 420), 80, "ShotTraining",
              "[E] Şut Antrenmanı — 2 saat",
              "Şut antrenmanı yaptın! Şut Gücü +2, Yorgunluk +15",
              120f, "ShotPower:+2,Fatigue:+15,Energy:-10");

        _Zone(zones, "ZoneCoach", new Vector2(900, 380), 80, "ChatCoach",
              "[E] Kemal Hoca ile Konuş — 15 dk",
              "Kemal Hoca'dan ipuçları aldın! Teknik +1",
              15f, "Technique:+1,Morale:+5");

        _Zone(zones, "ZoneExit", new Vector2(640, 660), 60, "Exit",
              "[E] Çık", "", 0f, "");

        root.AddChild(_MakePlayer(new Vector2(640, 500)));
        _MakeHUD(root);

        root.SetScript(GD.Load("res://scripts/SimpleInteriorScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/SporTesisiInterior.tscn");
    }

    private static void _Zone(Node zones, string name, Vector2 pos, float radius,
        string action, string prompt, string feedback, float timeMins, string effects)
    {
        var area = new Area2D();
        area.Name = name;
        area.Position = pos;
        area.SetMeta("action",   action);
        area.SetMeta("prompt",   prompt);
        area.SetMeta("feedback", feedback);
        area.SetMeta("time_min", timeMins);
        area.SetMeta("effects",  effects);
        var col = new CollisionShape2D();
        var ci  = new CircleShape2D();
        ci.Radius = radius;
        col.Shape = ci;
        area.AddChild(col);
        zones.AddChild(area);
    }

    private static CharacterBody2D _MakePlayer(Vector2 pos)
    {
        var p = new CharacterBody2D();
        p.Name = "Player"; p.Position = pos;
        var col = new CollisionShape2D();
        var cap = new CapsuleShape2D();
        cap.Radius = 16f; cap.Height = 30f;
        col.Shape = cap;
        p.AddChild(col);
        var sp = new Sprite2D();
        sp.Name = "Sprite";
        sp.Texture = GD.Load<Texture2D>("res://assets/img/player_sprite.png");
        sp.Scale = new Vector2(0.18f, 0.18f);
        sp.Position = new Vector2(0, -8);
        p.AddChild(sp);
        return p;
    }

    private static void _MakeHUD(Node root)
    {
        var hud = new CanvasLayer();
        hud.Name = "HUD"; hud.Layer = 10;
        root.AddChild(hud);

        var hudBg = new ColorRect();
        hudBg.Color = new Color(0,0,0,0.55f);
        hudBg.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        hudBg.Size = new Vector2(1280,38);
        hud.AddChild(hudBg);

        var t = new Label(); t.Name = "TimeLabel"; t.UniqueNameInOwner = true;
        t.Text = "Gün 1   07:30   Sabah"; t.Position = new Vector2(10,8);
        t.AddThemeFontSizeOverride("font_size",16);
        t.AddThemeColorOverride("font_color", new Color(1f,0.95f,0.7f));
        hud.AddChild(t);

        var p = new Label(); p.Name = "PromptLabel"; p.UniqueNameInOwner = true;
        p.Text = ""; p.Visible = false;
        p.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        p.HorizontalAlignment = HorizontalAlignment.Center;
        p.Position = new Vector2(0,-50);
        p.AddThemeFontSizeOverride("font_size",20);
        p.AddThemeColorOverride("font_color", Colors.White);
        hud.AddChild(p);

        var f = new Label(); f.Name = "FeedbackLabel"; f.UniqueNameInOwner = true;
        f.Text = ""; f.Visible = false;
        f.SetAnchorsPreset(Control.LayoutPreset.Center);
        f.Position = new Vector2(-220,-40);
        f.CustomMinimumSize = new Vector2(440,0);
        f.HorizontalAlignment = HorizontalAlignment.Center;
        f.AddThemeFontSizeOverride("font_size",18);
        f.AddThemeColorOverride("font_color", new Color(0.4f,1f,0.4f));
        hud.AddChild(f);
    }

    private static void _Wall(Node parent, string name, Vector2 pos, Vector2 size)
    {
        var sb = new StaticBody2D(); sb.Name = name;
        sb.Position = pos + size / 2f;
        var col = new CollisionShape2D();
        var sh  = new RectangleShape2D(); sh.Size = size;
        col.Shape = sh; sb.AddChild(col);
        parent.AddChild(sb);
    }
}
