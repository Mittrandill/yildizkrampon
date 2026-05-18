using Godot;

/// Scene builder — dotnet build && godot --headless --script scenes/BuildWorld.cs
/// Stardew Valley tarzı mahalle dünyası.
public partial class BuildWorld : SceneBuilderBase
{
    private const int TS   = 32;    // tile boyutu (piksel)
    private const int MW   = 60;    // yatay tile sayısı
    private const int MH   = 40;    // dikey tile sayısı
    private const float WW = MW * TS;   // 1920
    private const float WH = MH * TS;  // 1280

    public override void _Initialize()
    {
        GD.Print("Generating: World (Stardew Valley style)");

        var temp = new Node();
        var root = new WorldScene(); root.Name = "World";
        temp.AddChild(root);

        _BuildGround(root);
        _BuildPlayer(root);
        _BuildCamera(root);
        _BuildHUD(root);
        _PlaceTrees(root);
        _BuildPlayerHouse(root);

        temp.RemoveChild(root); temp.Free();
        PackAndSave(root, "res://scenes/World.tscn");
        Quit();
    }

    // ─── Zemin (renkli tile grid) ────────────────────────────────

    private void _BuildGround(Node root)
    {
        // Tüm zemin: çim rengi
        var grass = new ColorRect
        {
            Color    = new Color(0.27f, 0.60f, 0.25f),
            Size     = new Vector2(WW, WH),
            Position = Vector2.Zero,
            ZIndex   = -10
        };
        root.AddChild(grass);

        // Yol şeritleri (çakıl/toprak rengi)
        _Path(root, new Vector2(WW / 2f - 16, 0),     new Vector2(32, WH));   // dikey ana yol
        _Path(root, new Vector2(0, WH / 2f - 16),     new Vector2(WW, 32));   // yatay ana yol

        // Kaldırım kenarları
        _Border(root);

        // Koyu yeşil çim doku deseni (şeritler)
        for (int x = 0; x < MW; x += 2)
        {
            var strip = new ColorRect
            {
                Color    = new Color(0.24f, 0.56f, 0.22f, 0.45f),
                Position = new Vector2(x * TS, 0),
                Size     = new Vector2(TS, WH),
                ZIndex   = -9
            };
            root.AddChild(strip);
        }
    }

    private void _Path(Node root, Vector2 pos, Vector2 size)
    {
        var r = new ColorRect
        {
            Color    = new Color(0.60f, 0.48f, 0.32f),
            Position = pos,
            Size     = size,
            ZIndex   = -8
        };
        root.AddChild(r);
    }

    private void _Border(Node root)
    {
        Color bc = new Color(0.50f, 0.40f, 0.25f);
        int bw = 12;
        // Üst / alt yol kenarı
        _Rect(root, new Vector2(0, WH / 2f - 20), new Vector2(WW, bw), bc, -7);
        _Rect(root, new Vector2(0, WH / 2f + 8),  new Vector2(WW, bw), bc, -7);
        _Rect(root, new Vector2(WW / 2f - 20, 0), new Vector2(bw, WH), bc, -7);
        _Rect(root, new Vector2(WW / 2f + 8,  0), new Vector2(bw, WH), bc, -7);
    }

    private static void _Rect(Node p, Vector2 pos, Vector2 size, Color c, int z)
    {
        var r = new ColorRect { Color = c, Position = pos, Size = size, ZIndex = z };
        p.AddChild(r);
    }

    // ─── Çevre nesneler (ağaçlar) ────────────────────────────────

    private void _PlaceTrees(Node root)
    {
        // Dört köşe bölgesine küçük ağaçlar
        var positions = new (float x, float y)[]
        {
            (3,2),(5,3),(7,2),(10,4),(12,2),(15,3),(18,2),(22,4),
            (3,35),(6,36),(9,35),(13,37),(17,35),(21,36),(25,35),
            (45,2),(48,3),(51,2),(54,3),(57,2),
            (44,35),(47,36),(50,35),(53,36),(56,35),
            // Bloklara ağaç seti
            (3,8),(3,12),(3,16),(3,22),(3,27),(3,32),
            (56,8),(56,12),(56,16),(56,22),(56,27),(56,32),
        };

        foreach (var (tx, ty) in positions)
        {
            // Gövde
            var trunk = new ColorRect
            {
                Color    = new Color(0.45f, 0.30f, 0.12f),
                Position = new Vector2(tx * TS + 12, ty * TS + 16),
                Size     = new Vector2(8, 14),
                ZIndex   = 0
            };
            root.AddChild(trunk);

            // Tepe (koyu yeşil daire benzeri)
            _TreeTop(root, new Vector2(tx * TS + TS / 2f, ty * TS + 10));
        }
    }

    private void _TreeTop(Node root, Vector2 center)
    {
        // 3 farklı boyutta üst üste çemberler
        foreach (var (r, c) in new (float r, Color c)[]
        {
            (20f, new Color(0.15f, 0.45f, 0.12f)),
            (14f, new Color(0.20f, 0.55f, 0.15f)),
            (8f,  new Color(0.28f, 0.65f, 0.20f)),
        })
        {
            var node = new Polygon2D();
            node.ZIndex = 1;
            int seg = 16;
            var pts = new Vector2[seg];
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.Tau;
                pts[i] = center + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            node.Polygon = pts; node.Color = c;
            root.AddChild(node);
        }
    }

    // ─── Oyuncu ──────────────────────────────────────────────────

    private void _BuildPlayer(Node root)
    {
        var p = new Player(); p.Name = "Player"; p.UniqueNameInOwner = true;
        p.Position = new Vector2(WW / 2f, WH / 2f);
        p.ZIndex = 5;

        var col = new CollisionShape2D();
        var cap = new CapsuleShape2D { Radius = 8f, Height = 20f };
        col.Shape = cap;
        col.Position = new Vector2(0, 4f);
        p.AddChild(col);

        root.AddChild(p);

        // Dünya sınır duvarları
        _Wall(root, "WallTop",    new Vector2(WW / 2f, -10), new Vector2(WW + 40, 20));
        _Wall(root, "WallBottom", new Vector2(WW / 2f, WH + 10), new Vector2(WW + 40, 20));
        _Wall(root, "WallLeft",   new Vector2(-10, WH / 2f), new Vector2(20, WH + 40));
        _Wall(root, "WallRight",  new Vector2(WW + 10, WH / 2f), new Vector2(20, WH + 40));
    }

    private static void _Wall(Node p, string name, Vector2 center, Vector2 size)
    {
        var sb = new StaticBody2D { Name = name, CollisionLayer = 16u, CollisionMask = 1u };
        var col = new CollisionShape2D();
        var sh  = new RectangleShape2D { Size = size };
        col.Shape = sh; col.Position = center; sb.AddChild(col);
        p.AddChild(sb);
    }

    // ─── Kamera ──────────────────────────────────────────────────

    private void _BuildCamera(Node root)
    {
        var cam = new Camera2D
        {
            Name                    = "Camera",
            UniqueNameInOwner       = true,
            Zoom                    = new Vector2(0.75f, 0.75f),
            PositionSmoothingEnabled = true,
            PositionSmoothingSpeed   = 8f,
            LimitLeft   = 0,
            LimitTop    = 0,
            LimitRight  = (int)WW,
            LimitBottom = (int)WH
        };
        root.AddChild(cam);
    }

    // ─── HUD ─────────────────────────────────────────────────────

    private void _BuildHUD(Node root)
    {
        var hud = new CanvasLayer { Name = "HUD", Layer = 10 };
        root.AddChild(hud);

        // Üst sol: enerji + para
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.Position = new Vector2(10, 10);

        var sf = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.08f, 0.05f, 0.80f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        };
        panel.AddThemeStyleboxOverride("panel", sf);
        hud.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);

        var energyRow = new HBoxContainer();
        vbox.AddChild(energyRow);
        var eLbl = new Label { Text = "⚡ ENERJİ" };
        eLbl.AddThemeFontSizeOverride("font_size", 12);
        eLbl.AddThemeColorOverride("font_color", new Color(0.5f, 1f, 0.5f));
        energyRow.AddChild(eLbl);

        var moneyRow = new HBoxContainer();
        vbox.AddChild(moneyRow);
        var mLbl = new Label { Text = "💰 PARA: 0₺" };
        mLbl.AddThemeFontSizeOverride("font_size", 12);
        mLbl.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.3f));
        moneyRow.AddChild(mLbl);

        // Gün göstergesi (sağ üst)
        var dayLbl = new Label { Text = "GÜN 1" };
        dayLbl.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        dayLbl.Position = new Vector2(-80, 14);
        dayLbl.AddThemeFontSizeOverride("font_size", 16);
        dayLbl.AddThemeColorOverride("font_color", Colors.White);
        hud.AddChild(dayLbl);

        // Kontroller (alt)
        var ctrlLbl = new Label { Text = "WASD: Hareket   E: Etkileşim   SHIFT: Koş" };
        ctrlLbl.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        ctrlLbl.Position = new Vector2(10, -28);
        ctrlLbl.AddThemeFontSizeOverride("font_size", 11);
        ctrlLbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        hud.AddChild(ctrlLbl);

        // Ev kapısı hint (başlangıçta gizli, WorldScene açar)
        var houseHint = new Label { Name = "HouseHint", Text = "E  Eve Gir" };
        houseHint.UniqueNameInOwner = true;
        houseHint.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        houseHint.Position = new Vector2(10, -48);
        houseHint.AddThemeFontSizeOverride("font_size", 14);
        houseHint.AddThemeColorOverride("font_color", Colors.White);
        houseHint.Visible = false;
        hud.AddChild(houseHint);
    }

    // ─── Ev binası ───────────────────────────────────────────────

    private void _BuildPlayerHouse(Node root)
    {
        // Ev konumu: sol üst kadran, tile (6,7) = (192, 224)
        // WorldScene.HouseDoorWorld = (288, 368) ile senkron
        float hx = 192f, hy = 224f;
        float hw = 192f, hh = 160f;  // 6x5 tile

        // Zemin / bahçe
        var garden = new ColorRect
        {
            Color    = new Color(0.32f, 0.58f, 0.22f, 0.5f),
            Position = new Vector2(hx - 8, hy - 8),
            Size     = new Vector2(hw + 16, hh + 24),
            ZIndex   = -6
        };
        root.AddChild(garden);

        // Duvarlar (krem/beyaz Ege plasteri)
        var wall = new ColorRect
        {
            Color    = new Color(0.94f, 0.92f, 0.86f),
            Position = new Vector2(hx, hy + 32),
            Size     = new Vector2(hw, hh - 32),
            ZIndex   = 2
        };
        root.AddChild(wall);

        // Çatı (terracotta)
        var roofBody = new ColorRect
        {
            Color    = new Color(0.75f, 0.38f, 0.20f),
            Position = new Vector2(hx - 12, hy + 12),
            Size     = new Vector2(hw + 24, 44),
            ZIndex   = 3
        };
        root.AddChild(roofBody);

        // Çatı üçgeni (Polygon2D)
        var roofTop = new Polygon2D { Color = new Color(0.65f, 0.30f, 0.14f), ZIndex = 3 };
        roofTop.Polygon = new Vector2[]
        {
            new Vector2(hx - 16, hy + 12),
            new Vector2(hx + hw + 16, hy + 12),
            new Vector2(hx + hw / 2f, hy - 20)
        };
        root.AddChild(roofTop);

        // Sol pencere
        _HouseWindow(root, hx + 28, hy + 52);
        // Sağ pencere
        _HouseWindow(root, hx + hw - 68, hy + 52);

        // Kapı (koyu ahşap, merkez alt)
        var door = new ColorRect
        {
            Color    = new Color(0.38f, 0.24f, 0.10f),
            Position = new Vector2(hx + hw / 2f - 20, hy + hh - 64),
            Size     = new Vector2(40, 64),
            ZIndex   = 4
        };
        root.AddChild(door);

        // Kapı kolu
        var knob = new ColorRect
        {
            Color    = new Color(0.78f, 0.64f, 0.20f),
            Position = new Vector2(hx + hw / 2f + 6, hy + hh - 36),
            Size     = new Vector2(6, 8),
            ZIndex   = 5
        };
        root.AddChild(knob);

        // Kapı çerçevesi
        var doorFrame = new ColorRect
        {
            Color    = new Color(0.55f, 0.40f, 0.20f),
            Position = new Vector2(hx + hw / 2f - 24, hy + hh - 68),
            Size     = new Vector2(48, 68),
            ZIndex   = 3
        };
        root.AddChild(doorFrame);
        door.ZIndex = 4;   // kapı çerçevenin önünde

        // Çiçek saksıları (kapı yanı)
        _HousePot(root, hx + hw / 2f - 40, hy + hh - 30);
        _HousePot(root, hx + hw / 2f + 30, hy + hh - 30);

        // Ev ismi levhası
        var signBg = new ColorRect
        {
            Color    = new Color(0.55f, 0.38f, 0.18f),
            Position = new Vector2(hx + hw / 2f - 36, hy + 16),
            Size     = new Vector2(72, 18),
            ZIndex   = 4
        };
        root.AddChild(signBg);

        // Statik çarpışma duvarları (evin etrafı)
        _HouseWall(root, "HouseWallN", new Vector2(hx + hw / 2f, hy + 32),        new Vector2(hw, 10));
        _HouseWall(root, "HouseWallL", new Vector2(hx - 5, hy + 32 + hh / 2f),    new Vector2(10, hh));
        _HouseWall(root, "HouseWallR", new Vector2(hx + hw + 5, hy + 32 + hh / 2f), new Vector2(10, hh));
        // Alt kısım: kapı dışında doldur
        _HouseWall(root, "HouseWallBL", new Vector2(hx + 44, hy + hh + 8),        new Vector2(68, 10));
        _HouseWall(root, "HouseWallBR", new Vector2(hx + hw - 44, hy + hh + 8),   new Vector2(68, 10));
    }

    private void _HouseWindow(Node root, float x, float y)
    {
        // Çerçeve
        var frame = new ColorRect
        {
            Color    = new Color(0.22f, 0.48f, 0.78f),
            Position = new Vector2(x - 4, y - 4),
            Size     = new Vector2(48, 40),
            ZIndex   = 4
        };
        root.AddChild(frame);
        // Cam / ışık
        var glass = new ColorRect
        {
            Color    = new Color(1.0f, 0.95f, 0.70f, 0.45f),
            Position = new Vector2(x, y),
            Size     = new Vector2(40, 32),
            ZIndex   = 5
        };
        root.AddChild(glass);
        // Çapraz çizgi
        var cross1 = new ColorRect
        {
            Color    = new Color(0.22f, 0.48f, 0.78f),
            Position = new Vector2(x + 18, y),
            Size     = new Vector2(4, 32),
            ZIndex   = 6
        };
        root.AddChild(cross1);
        var cross2 = new ColorRect
        {
            Color    = new Color(0.22f, 0.48f, 0.78f),
            Position = new Vector2(x, y + 14),
            Size     = new Vector2(40, 4),
            ZIndex   = 6
        };
        root.AddChild(cross2);
    }

    private void _HousePot(Node root, float x, float y)
    {
        var pot = new ColorRect
        {
            Color    = new Color(0.75f, 0.40f, 0.22f),
            Position = new Vector2(x, y),
            Size     = new Vector2(10, 10),
            ZIndex   = 5
        };
        root.AddChild(pot);
        var plant = new ColorRect
        {
            Color    = new Color(0.25f, 0.60f, 0.22f),
            Position = new Vector2(x - 2, y - 8),
            Size     = new Vector2(14, 10),
            ZIndex   = 6
        };
        root.AddChild(plant);
    }

    private static void _HouseWall(Node p, string name, Vector2 center, Vector2 size)
    {
        var sb = new StaticBody2D { Name = name, CollisionLayer = 16u, CollisionMask = 1u };
        var col = new CollisionShape2D();
        col.Shape = new RectangleShape2D { Size = size };
        col.Position = center;
        sb.AddChild(col);
        p.AddChild(sb);
    }
}
