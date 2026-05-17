using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildHomeInterior.cs
public partial class BuildHomeInterior : SceneBuilderBase
{
    // Layout: 4 rooms side-by-side, each 480 wide x 640 tall
    private const float RoomW = 480f;
    private const float RoomH = 640f;
    private const float TotalW = RoomW * 4; // 1920
    private const float DoorW  = 80f;

    // Room colors
    private static readonly Color ColBedroom = new(0.55f, 0.42f, 0.32f);
    private static readonly Color ColKitchen = new(0.70f, 0.62f, 0.42f);
    private static readonly Color ColSalon   = new(0.48f, 0.52f, 0.62f);
    private static readonly Color ColBathroom= new(0.38f, 0.55f, 0.62f);
    private static readonly Color ColFloor   = new(0.85f, 0.78f, 0.60f);
    private static readonly Color ColWall    = new(0.30f, 0.25f, 0.20f);
    private static readonly Color ColDoor    = new(0.60f, 0.40f, 0.20f);

    private Node2D _root = null!;
    private Node   _interactZones = null!;
    private Node   _roomZones = null!;
    private Node   _walls = null!;

    public override void _Initialize()
    {
        GD.Print("Generating: HomeInterior");

        var temp = new Node();
        _root = new Node2D();
        _root.Name = "HomeInterior";
        temp.AddChild(_root);

        _interactZones = new Node2D(); _interactZones.Name = "InteractZones"; _root.AddChild(_interactZones);
        _roomZones     = new Node2D(); _roomZones.Name     = "RoomZones";     _root.AddChild(_roomZones);
        _walls         = new Node2D(); _walls.Name         = "Walls";         _root.AddChild(_walls);

        // --- Floors ---
        _Rect(_root, "FloorBedroom",  ColFloor,                      new Vector2(0, 100),      new Vector2(RoomW, RoomH - 100), -2);
        _Rect(_root, "FloorKitchen",  new Color(0.80f,0.75f,0.55f),  new Vector2(RoomW, 100),  new Vector2(RoomW, RoomH - 100), -2);
        _Rect(_root, "FloorSalon",    new Color(0.72f,0.68f,0.55f),  new Vector2(RoomW*2,100), new Vector2(RoomW, RoomH - 100), -2);
        _Rect(_root, "FloorBathroom", new Color(0.60f,0.75f,0.80f),  new Vector2(RoomW*3,100), new Vector2(RoomW, RoomH - 100), -2);

        // --- Ceiling / walls (colored band at top) ---
        _Rect(_root, "CeilBedroom",  ColBedroom, new Vector2(0, 0),       new Vector2(RoomW, 100), 0);
        _Rect(_root, "CeilKitchen",  ColKitchen, new Vector2(RoomW, 0),   new Vector2(RoomW, 100), 0);
        _Rect(_root, "CeilSalon",    ColSalon,   new Vector2(RoomW*2, 0), new Vector2(RoomW, 100), 0);
        _Rect(_root, "CeilBathroom", ColBathroom,new Vector2(RoomW*3, 0), new Vector2(RoomW, 100), 0);

        // Room title labels
        _Label(_root, "LblBedroom",  "Yatak Odası", new Vector2(20,  12), 18, Colors.White);
        _Label(_root, "LblKitchen",  "Mutfak",      new Vector2(RoomW+20, 12), 18, Colors.White);
        _Label(_root, "LblSalon",    "Salon",        new Vector2(RoomW*2+20, 12), 18, Colors.White);
        _Label(_root, "LblBathroom", "Banyo",        new Vector2(RoomW*3+20, 12), 18, Colors.White);

        // --- Room zone detectors (for room label in HUD) ---
        _RoomZone("Yatak Odası", new Vector2(RoomW/2,   RoomH/2), RoomW*0.45f, RoomH*0.45f);
        _RoomZone("Mutfak",      new Vector2(RoomW*1.5f,RoomH/2), RoomW*0.45f, RoomH*0.45f);
        _RoomZone("Salon",       new Vector2(RoomW*2.5f,RoomH/2), RoomW*0.45f, RoomH*0.45f);
        _RoomZone("Banyo",       new Vector2(RoomW*3.5f,RoomH/2), RoomW*0.45f, RoomH*0.45f);

        // --- Outer walls ---
        _Wall("OuterTop",    new Vector2(0,    -20), new Vector2(TotalW, 20));
        _Wall("OuterBottom", new Vector2(0,   RoomH), new Vector2(TotalW, 20));
        _Wall("OuterLeft",   new Vector2(-20,   0),  new Vector2(20, RoomH));
        _Wall("OuterRight",  new Vector2(TotalW, 0), new Vector2(20, RoomH));

        // --- Room dividers (with doorways) ---
        // Each divider: wall left half, gap, wall right half
        _RoomDivider(RoomW,   "Div1"); // bedroom-kitchen
        _RoomDivider(RoomW*2, "Div2"); // kitchen-salon
        _RoomDivider(RoomW*3, "Div3"); // salon-bathroom

        // --- Interactive objects ---

        // BEDROOM: Bed (top-left area)
        _Furniture(_root, "Bed", new Color(0.4f,0.4f,0.8f), new Vector2(40, 130), new Vector2(160, 100));
        _Label(_root, "LblBed", "Yatak", new Vector2(70, 155), 13, Colors.White);
        _InteractZone("Sleep", "[E] Uyu — Enerji +60", new Vector2(120, 230), 55);

        // BEDROOM: Study desk (right side)
        _Furniture(_root, "Desk", new Color(0.5f,0.35f,0.2f), new Vector2(320, 180), new Vector2(120, 70));
        _Label(_root, "LblDesk", "Masa", new Vector2(355, 200), 13, Colors.White);
        _InteractZone("Study", "[E] Ders Çalış — Teknik +1", new Vector2(380, 250), 50);

        // KITCHEN: Fridge (left wall)
        _Furniture(_root, "Fridge", new Color(0.8f,0.85f,0.9f), new Vector2(RoomW+20, 130), new Vector2(80, 120));
        _Label(_root, "LblFridge", "Buzdolabı", new Vector2(RoomW+18, 155), 11, Colors.Black);
        _InteractZone("Eat", "[E] Yemek Ye — Enerji +20", new Vector2(RoomW+120, 210), 60);

        // BATHROOM: Shower (right corner)
        _Furniture(_root, "Shower", new Color(0.5f,0.8f,0.9f), new Vector2(RoomW*3+320, 130), new Vector2(120, 100));
        _Label(_root, "LblShower", "Duş", new Vector2(RoomW*3+355, 155), 13, Colors.White);
        _InteractZone("Shower", "[E] Duş Al — Yorgunluk -15", new Vector2(RoomW*3+350, 240), 55);

        // SALON: Front door (exit to WorldMap)
        _Furniture(_root, "FrontDoor", ColDoor, new Vector2(RoomW*2+200, RoomH-80), new Vector2(80, 80));
        _Label(_root, "LblFrontDoor", "Çıkış", new Vector2(RoomW*2+210, RoomH-65), 13, Colors.White);
        _InteractZone("Exit", "[E] Dışarı Çık", new Vector2(RoomW*2+240, RoomH-120), 60);

        // --- Player (starts near bedroom door) ---
        var player = new CharacterBody2D();
        player.Name = "Player";
        player.Position = new Vector2(RoomW - 60, RoomH / 2);

        var pCol = new CollisionShape2D();
        var capsule = new CapsuleShape2D();
        capsule.Radius = 16f;
        capsule.Height = 30f;
        pCol.Shape = capsule;
        player.AddChild(pCol);

        var pSprite = new Sprite2D();
        pSprite.Name = "Sprite";
        pSprite.Texture = GD.Load<Texture2D>("res://assets/img/player_sprite.png");
        pSprite.Scale = new Vector2(0.18f, 0.18f);
        pSprite.Position = new Vector2(0, -8);
        player.AddChild(pSprite);

        var cam = new Camera2D();
        cam.Name = "Camera2D";
        cam.Enabled = true;
        cam.LimitLeft   = 0;
        cam.LimitTop    = 0;
        cam.LimitRight  = (int)TotalW;
        cam.LimitBottom = (int)RoomH;
        player.AddChild(cam);

        _root.AddChild(player);

        // --- HUD ---
        var hud = new CanvasLayer();
        hud.Name = "HUD";
        hud.Layer = 10;
        _root.AddChild(hud);

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
        timeLabel.AddThemeColorOverride("font_color", new Color(1f,0.95f,0.7f));
        hud.AddChild(timeLabel);

        var roomLabel = new Label();
        roomLabel.Name = "RoomLabel";
        roomLabel.UniqueNameInOwner = true;
        roomLabel.Text = "Yatak Odası";
        roomLabel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        roomLabel.Position = new Vector2(-180, 8);
        roomLabel.AddThemeFontSizeOverride("font_size", 16);
        roomLabel.AddThemeColorOverride("font_color", new Color(0.8f,0.9f,1f));
        hud.AddChild(roomLabel);

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
        feedbackLabel.Position = new Vector2(-200, -60);
        feedbackLabel.CustomMinimumSize = new Vector2(400, 0);
        feedbackLabel.HorizontalAlignment = HorizontalAlignment.Center;
        feedbackLabel.AddThemeFontSizeOverride("font_size", 18);
        feedbackLabel.AddThemeColorOverride("font_color", new Color(0.4f,1f,0.4f));
        hud.AddChild(feedbackLabel);

        _root.SetScript(GD.Load("res://scripts/HomeInteriorScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/HomeInterior.tscn");
    }

    private void _Rect(Node parent, string name, Color color, Vector2 pos, Vector2 size, int z = 0)
    {
        var r = new ColorRect();
        r.Name = name; r.Color = color; r.Position = pos; r.Size = size; r.ZIndex = z;
        parent.AddChild(r);
    }

    private void _Label(Node parent, string name, string text, Vector2 pos, int fontSize, Color color)
    {
        var l = new Label();
        l.Name = name; l.Text = text; l.Position = pos;
        l.AddThemeFontSizeOverride("font_size", fontSize);
        l.AddThemeColorOverride("font_color", color);
        l.ZIndex = 2;
        parent.AddChild(l);
    }

    private void _Furniture(Node parent, string name, Color color, Vector2 pos, Vector2 size)
    {
        var r = new ColorRect();
        r.Name = name + "Rect"; r.Color = color; r.Position = pos; r.Size = size; r.ZIndex = 1;
        parent.AddChild(r);

        // StaticBody2D so player can't walk through
        var sb = new StaticBody2D();
        sb.Name = name + "Wall";
        sb.Position = pos + size / 2f;
        var col = new CollisionShape2D();
        var sh  = new RectangleShape2D();
        sh.Size = size;
        col.Shape = sh;
        sb.AddChild(col);
        _walls.AddChild(sb);
    }

    private void _InteractZone(string action, string prompt, Vector2 center, float radius)
    {
        var area = new Area2D();
        area.Name = "Zone_" + action;
        area.Position = center;
        area.SetMeta("action", action);
        area.SetMeta("prompt", prompt);

        var col = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = radius;
        col.Shape = circle;
        area.AddChild(col);

        _interactZones.AddChild(area);
    }

    private void _RoomZone(string roomName, Vector2 center, float hw, float hh)
    {
        var area = new Area2D();
        area.Name = "Room_" + roomName.Replace(" ", "");
        area.Position = center;
        area.SetMeta("room", roomName);

        var col = new CollisionShape2D();
        var rect = new RectangleShape2D();
        rect.Size = new Vector2(hw * 2, hh * 2);
        col.Shape = rect;
        area.AddChild(col);

        _roomZones.AddChild(area);
    }

    private void _Wall(string name, Vector2 pos, Vector2 size)
    {
        var sb = new StaticBody2D();
        sb.Name = name;
        sb.Position = pos + size / 2f;
        var col = new CollisionShape2D();
        var sh  = new RectangleShape2D();
        sh.Size = size;
        col.Shape = sh;
        sb.AddChild(col);
        _walls.AddChild(sb);
    }

    private void _RoomDivider(float x, string baseName)
    {
        float gapTop    = RoomH * 0.35f;  // door start (y)
        float gapBottom = RoomH * 0.65f;  // door end (y)
        float wallThickness = 16f;

        // Wall above door
        _Wall(baseName + "Top", new Vector2(x - wallThickness/2, 0), new Vector2(wallThickness, gapTop));
        // Wall below door
        _Wall(baseName + "Bot", new Vector2(x - wallThickness/2, gapBottom), new Vector2(wallThickness, RoomH - gapBottom));

        // Visual wall panels
        var wTop = new ColorRect();
        wTop.Name = baseName + "VisTop";
        wTop.Color = ColWall;
        wTop.Position = new Vector2(x - 8, 0);
        wTop.Size     = new Vector2(16, gapTop);
        wTop.ZIndex = 1;
        _root.AddChild(wTop);

        var wBot = new ColorRect();
        wBot.Name = baseName + "VisBot";
        wBot.Color = ColWall;
        wBot.Position = new Vector2(x - 8, gapBottom);
        wBot.Size     = new Vector2(16, RoomH - gapBottom);
        wBot.ZIndex = 1;
        _root.AddChild(wBot);

        // Door frame visual
        var door = new ColorRect();
        door.Name  = baseName + "Door";
        door.Color = ColDoor;
        door.Position = new Vector2(x - 8, gapTop);
        door.Size     = new Vector2(16, gapBottom - gapTop);
        door.ZIndex = 1;
        _root.AddChild(door);
    }
}
