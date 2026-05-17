using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildCayBahcesi.cs
public partial class BuildCayBahcesi : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: CayBahcesi");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "CayBahcesi";
        temp.AddChild(root);

        // Background — outdoor tea garden feel
        var sky = new ColorRect();
        sky.Name = "Sky"; sky.Color = new Color(0.53f, 0.75f, 0.87f);
        sky.Position = Vector2.Zero; sky.Size = new Vector2(1280, 400); sky.ZIndex = -2;
        root.AddChild(sky);

        var ground = new ColorRect();
        ground.Name = "Ground"; ground.Color = new Color(0.42f, 0.62f, 0.35f);
        ground.Position = new Vector2(0, 380); ground.Size = new Vector2(1280, 340); ground.ZIndex = -2;
        root.AddChild(ground);

        // Title banner
        var banner = new ColorRect();
        banner.Color = new Color(0.3f, 0.55f, 0.35f);
        banner.Position = Vector2.Zero; banner.Size = new Vector2(1280, 60);
        root.AddChild(banner);

        var roomLbl = new Label();
        roomLbl.Text = "Çınar Çay Bahçesi";
        roomLbl.Position = new Vector2(20, 15);
        roomLbl.AddThemeFontSizeOverride("font_size", 22);
        roomLbl.AddThemeColorOverride("font_color", Colors.White);
        roomLbl.ZIndex = 2; root.AddChild(roomLbl);

        // Tables
        _Box(root, "Table1", new Color(0.65f,0.45f,0.25f), new Vector2(200, 350), new Vector2(120, 80));
        _Box(root, "Table2", new Color(0.65f,0.45f,0.25f), new Vector2(560, 350), new Vector2(120, 80));
        _Box(root, "Table3", new Color(0.65f,0.45f,0.25f), new Vector2(960, 350), new Vector2(120, 80));

        _Lbl(root, "T1L", "Masa", new Vector2(240, 365));
        _Lbl(root, "T2L", "Masa", new Vector2(600, 365));
        _Lbl(root, "T3L", "Masa", new Vector2(1000, 365));

        // Baran NPC (afternoon)
        var npc = new Sprite2D();
        npc.Name = "Baran";
        npc.Texture = GD.Load<Texture2D>("res://assets/img/npc_blue.png");
        npc.Scale = new Vector2(0.18f, 0.18f);
        npc.Position = new Vector2(580, 310);
        npc.ZIndex = 2; root.AddChild(npc);

        var npcLbl = new Label();
        npcLbl.Text = "Baran"; npcLbl.Position = new Vector2(553, 265);
        npcLbl.AddThemeFontSizeOverride("font_size", 13);
        npcLbl.AddThemeColorOverride("font_color", Colors.White);
        npcLbl.ZIndex = 3; root.AddChild(npcLbl);

        _Wall(root, "WallTop",    Vector2.Zero,         new Vector2(1280, 20));
        _Wall(root, "WallBottom", new Vector2(0, 700),  new Vector2(1280, 20));
        _Wall(root, "WallLeft",   new Vector2(-20, 0),  new Vector2(20, 720));
        _Wall(root, "WallRight",  new Vector2(1280, 0), new Vector2(20, 720));

        var zones = new Node2D();
        zones.Name = "InteractZones";
        root.AddChild(zones);

        _Zone(zones, "ZoneCay",  new Vector2(260, 450), 80, "DrinkTea",
              "[E] Çay İç — 1 saat",
              "Çay içtin, dinlendin! Moral +10, Enerji -5",
              60f, "Morale:+10,Energy:-5");

        _Zone(zones, "ZoneBaran", new Vector2(620, 420), 80, "ChatBaran",
              "[E] Baran ile Sohbet — 15 dk",
              "Baran ile sohbet ettin! Moral +5",
              15f, "Morale:+5");

        _Zone(zones, "ZoneExit", new Vector2(640, 660), 60, "Exit", "[E] Çık", "", 0f, "");

        root.AddChild(_MakePlayer(new Vector2(640, 550)));
        _MakeHUD(root);

        root.SetScript(GD.Load("res://scripts/SimpleInteriorScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/CayBahcesi.tscn");
    }

    private static void _Box(Node p, string n, Color c, Vector2 pos, Vector2 size)
    {
        var r = new ColorRect(); r.Name = n; r.Color = c;
        r.Position = pos; r.Size = size; r.ZIndex = 1; p.AddChild(r);
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
