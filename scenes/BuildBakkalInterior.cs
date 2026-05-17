using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildBakkalInterior.cs
public partial class BuildBakkalInterior : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: BakkalInterior");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "BakkalInterior";
        temp.AddChild(root);

        // Background
        var bg = new Sprite2D();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/shop_bg.png");
        bg.Position = new Vector2(640, 360);
        bg.ZIndex = -1;
        root.AddChild(bg);

        // Dark overlay for readability
        var overlay = new ColorRect();
        overlay.Name = "Overlay";
        overlay.Color = new Color(0, 0, 0, 0.25f);
        overlay.Position = Vector2.Zero;
        overlay.Size = new Vector2(1280, 720);
        overlay.ZIndex = 0;
        root.AddChild(overlay);

        // Room label
        var roomLbl = new Label();
        roomLbl.Name = "RoomLabel";
        roomLbl.Text = "Rıza Bakkal";
        roomLbl.Position = new Vector2(20, 20);
        roomLbl.AddThemeFontSizeOverride("font_size", 20);
        roomLbl.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        roomLbl.ZIndex = 2;
        root.AddChild(roomLbl);

        // Counter visual
        var counter = new ColorRect();
        counter.Name = "Counter";
        counter.Color = new Color(0.5f, 0.35f, 0.2f);
        counter.Position = new Vector2(400, 260);
        counter.Size = new Vector2(480, 80);
        counter.ZIndex = 1;
        root.AddChild(counter);

        // Counter wall
        var cWall = new StaticBody2D();
        cWall.Name = "CounterWall";
        cWall.Position = new Vector2(640, 300);
        var cCol = new CollisionShape2D();
        var cRect = new RectangleShape2D();
        cRect.Size = new Vector2(480, 80);
        cCol.Shape = cRect;
        cWall.AddChild(cCol);
        root.AddChild(cWall);

        // NPC (Rıza Abi) visual
        var npcSprite = new Sprite2D();
        npcSprite.Name = "RizaAbi";
        npcSprite.Texture = GD.Load<Texture2D>("res://assets/img/npc_blue.png");
        npcSprite.Scale = new Vector2(0.18f, 0.18f);
        npcSprite.Position = new Vector2(640, 220);
        npcSprite.ZIndex = 2;
        root.AddChild(npcSprite);

        var npcLbl = new Label();
        npcLbl.Name = "RizaLbl";
        npcLbl.Text = "Rıza Abi";
        npcLbl.Position = new Vector2(600, 175);
        npcLbl.AddThemeFontSizeOverride("font_size", 13);
        npcLbl.AddThemeColorOverride("font_color", Colors.White);
        npcLbl.ZIndex = 3;
        root.AddChild(npcLbl);

        // Boundary walls
        _Wall(root, "WallTop",    Vector2.Zero,          new Vector2(1280, 20));
        _Wall(root, "WallBottom", new Vector2(0, 700),   new Vector2(1280, 20));
        _Wall(root, "WallLeft",   new Vector2(-20, 0),   new Vector2(20, 720));
        _Wall(root, "WallRight",  new Vector2(1280, 0),  new Vector2(20, 720));

        // Interact zones
        var zones = new Node2D();
        zones.Name = "InteractZones";
        root.AddChild(zones);

        // Buy protein bar
        var zBuy = new Area2D();
        zBuy.Name = "ZoneBuy";
        zBuy.Position = new Vector2(640, 380);
        zBuy.SetMeta("action",   "BuyProteinBar");
        zBuy.SetMeta("prompt",   "[E] Protein Bar Al — 15 dk");
        zBuy.SetMeta("feedback", "Protein bar aldın! Enerji +25, Şut Gücü +1");
        zBuy.SetMeta("time_min", 15f);
        zBuy.SetMeta("effects",  "Energy:+25,ShotPower:+1");
        var zbC = new CollisionShape2D();
        var zbCi = new CircleShape2D();
        zbCi.Radius = 70f;
        zbC.Shape = zbCi;
        zBuy.AddChild(zbC);
        zones.AddChild(zBuy);

        // Chat with Rıza
        var zChat = new Area2D();
        zChat.Name = "ZoneChat";
        zChat.Position = new Vector2(640, 420);
        zChat.SetMeta("action",   "ChatRiza");
        zChat.SetMeta("prompt",   "[E] Rıza Abi ile Sohbet — 15 dk");
        zChat.SetMeta("feedback", "Sohbet ettin! Moral +5");
        zChat.SetMeta("time_min", 15f);
        zChat.SetMeta("effects",  "Morale:+5");
        var zcC = new CollisionShape2D();
        var zcCi = new CircleShape2D();
        zcCi.Radius = 90f;
        zcC.Shape = zcCi;
        zChat.AddChild(zcC);
        zones.AddChild(zChat);

        // Exit
        var zExit = new Area2D();
        zExit.Name = "ZoneExit";
        zExit.Position = new Vector2(640, 650);
        zExit.SetMeta("action", "Exit");
        zExit.SetMeta("prompt", "[E] Çık");
        var zeC = new CollisionShape2D();
        var zeCi = new CircleShape2D();
        zeCi.Radius = 60f;
        zeC.Shape = zeCi;
        zExit.AddChild(zeC);
        zones.AddChild(zExit);

        // Player
        var player = _MakePlayer(new Vector2(640, 500));
        root.AddChild(player);

        // HUD
        _MakeHUD(root);

        root.SetScript(GD.Load("res://scripts/SimpleInteriorScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/BakkalInterior.tscn");
    }

    private static CharacterBody2D _MakePlayer(Vector2 pos)
    {
        var p = new CharacterBody2D();
        p.Name = "Player";
        p.Position = pos;
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
        hud.Name = "HUD";
        hud.Layer = 10;
        root.AddChild(hud);

        var hudBg = new ColorRect();
        hudBg.Color = new Color(0,0,0,0.55f);
        hudBg.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        hudBg.Size = new Vector2(1280, 38);
        hud.AddChild(hudBg);

        var timeLabel = new Label();
        timeLabel.Name = "TimeLabel";
        timeLabel.UniqueNameInOwner = true;
        timeLabel.Text = "Gün 1   07:30   Sabah";
        timeLabel.Position = new Vector2(10, 8);
        timeLabel.AddThemeFontSizeOverride("font_size", 16);
        timeLabel.AddThemeColorOverride("font_color", new Color(1f,0.95f,0.7f));
        hud.AddChild(timeLabel);

        var promptLabel = new Label();
        promptLabel.Name = "PromptLabel";
        promptLabel.UniqueNameInOwner = true;
        promptLabel.Text = "";
        promptLabel.Visible = false;
        promptLabel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
        promptLabel.Position = new Vector2(0, -50);
        promptLabel.AddThemeFontSizeOverride("font_size", 20);
        promptLabel.AddThemeColorOverride("font_color", Colors.White);
        hud.AddChild(promptLabel);

        var feedbackLabel = new Label();
        feedbackLabel.Name = "FeedbackLabel";
        feedbackLabel.UniqueNameInOwner = true;
        feedbackLabel.Text = "";
        feedbackLabel.Visible = false;
        feedbackLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        feedbackLabel.Position = new Vector2(-220, -40);
        feedbackLabel.CustomMinimumSize = new Vector2(440, 0);
        feedbackLabel.HorizontalAlignment = HorizontalAlignment.Center;
        feedbackLabel.AddThemeFontSizeOverride("font_size", 18);
        feedbackLabel.AddThemeColorOverride("font_color", new Color(0.4f,1f,0.4f));
        hud.AddChild(feedbackLabel);
    }

    private static void _Wall(Node parent, string name, Vector2 pos, Vector2 size)
    {
        var sb = new StaticBody2D();
        sb.Name = name;
        sb.Position = pos + size / 2f;
        var col = new CollisionShape2D();
        var sh  = new RectangleShape2D();
        sh.Size = size;
        col.Shape = sh;
        sb.AddChild(col);
        parent.AddChild(sb);
    }
}
