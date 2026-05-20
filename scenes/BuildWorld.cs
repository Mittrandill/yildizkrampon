using Godot;

/// Scene builder — dotnet build && godot --headless --script scenes/BuildWorld.cs
public partial class BuildWorld : SceneBuilderBase
{
    private const int   TS = 32;
    private const int   MW = 60;
    private const int   MH = 40;
    private const float WW = MW * TS;  // 1920
    private const float WH = MH * TS; // 1280

    public override void _Initialize()
    {
        GD.Print("Generating: World");

        var temp = new Node();
        var root = new WorldScene(); root.Name = "World";
        temp.AddChild(root);

        _BuildGround(root);
        _BuildPlayer(root);
        _BuildHUD(root);
        _PlaceTrees(root);
        _BuildPitch(root);

        temp.RemoveChild(root); temp.Free();
        PackAndSave(root, "res://scenes/World.tscn");
        Quit();
    }

    // ─── Ground ──────────────────────────────────────────────────────────────────

    private void _BuildGround(Node root)
    {
        root.AddChild(new ColorRect
        {
            Color    = new Color(0.27f, 0.60f, 0.25f),
            Size     = new Vector2(WW, WH),
            Position = Vector2.Zero,
            ZIndex   = -10
        });

        // Roads
        _R(root, new Vector2(WW/2f-16, 0),    new Vector2(32, WH),  new Color(0.60f, 0.48f, 0.32f), -8);
        _R(root, new Vector2(0, WH/2f-16),    new Vector2(WW, 32),  new Color(0.60f, 0.48f, 0.32f), -8);

        // Road kerbs
        Color bc = new Color(0.50f, 0.40f, 0.25f);
        _R(root, new Vector2(0,        WH/2f-20), new Vector2(WW, 12), bc, -7);
        _R(root, new Vector2(0,        WH/2f+8),  new Vector2(WW, 12), bc, -7);
        _R(root, new Vector2(WW/2f-20, 0),        new Vector2(12, WH), bc, -7);
        _R(root, new Vector2(WW/2f+8,  0),        new Vector2(12, WH), bc, -7);

        // Grass stripes
        for (int x = 0; x < MW; x += 2)
        {
            root.AddChild(new ColorRect
            {
                Color    = new Color(0.24f, 0.56f, 0.22f, 0.45f),
                Position = new Vector2(x * TS, 0),
                Size     = new Vector2(TS, WH),
                ZIndex   = -9
            });
        }
    }

    private static void _R(Node p, Vector2 pos, Vector2 size, Color c, int z)
        => p.AddChild(new ColorRect { Color = c, Position = pos, Size = size, ZIndex = z });

    // ─── Player + Camera ─────────────────────────────────────────────────────────

    private void _BuildPlayer(Node root)
    {
        var p = new Player { Name = "Player" };
        p.UniqueNameInOwner = true;
        p.Position          = new Vector2(WW / 2f, WH / 2f);
        p.ZIndex            = 5;
        p.CollisionLayer    = 1u;
        p.CollisionMask     = 16u;

        var col = new CollisionShape2D();
        col.Shape    = new CapsuleShape2D { Radius = 8f, Height = 20f };
        col.Position = new Vector2(0, 4f);
        p.AddChild(col);

        // Camera as child of player so it follows automatically
        p.AddChild(new Camera2D
        {
            Name                     = "Camera",
            UniqueNameInOwner        = true,
            Zoom                     = new Vector2(0.75f, 0.75f),
            PositionSmoothingEnabled = true,
            PositionSmoothingSpeed   = 8f,
            LimitLeft   = 0, LimitTop    = 0,
            LimitRight  = (int)WW, LimitBottom = (int)WH
        });

        root.AddChild(p);

        // World boundary walls
        _Wall(root, "WallTop",    new Vector2(WW/2f,   -10), new Vector2(WW+40,  20));
        _Wall(root, "WallBottom", new Vector2(WW/2f, WH+10), new Vector2(WW+40,  20));
        _Wall(root, "WallLeft",   new Vector2(  -10, WH/2f), new Vector2(   20, WH+40));
        _Wall(root, "WallRight",  new Vector2(WW+10, WH/2f), new Vector2(   20, WH+40));
    }

    private static void _Wall(Node p, string name, Vector2 center, Vector2 size)
    {
        var sb  = new StaticBody2D { Name = name, CollisionLayer = 16u, CollisionMask = 0u };
        var col = new CollisionShape2D();
        col.Shape    = new RectangleShape2D { Size = size };
        col.Position = center;
        sb.AddChild(col);
        p.AddChild(sb);
    }

    // ─── HUD ─────────────────────────────────────────────────────────────────────

    private void _BuildHUD(Node root)
    {
        var hud = new CanvasLayer { Name = "HUD", Layer = 10 };
        root.AddChild(hud);

        // Top-left panel: energy + money
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.Position = new Vector2(10, 10);
        var sf = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.06f, 0.04f, 0.82f),
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        };
        panel.AddThemeStyleboxOverride("panel", sf);
        hud.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);

        var eLbl = new Label { Text = "ENERJI 100" };
        eLbl.AddThemeFontSizeOverride("font_size", 12);
        eLbl.AddThemeColorOverride("font_color", new Color(0.5f, 1f, 0.5f));
        vbox.AddChild(eLbl);

        var mLbl = new Label { Text = "PARA: 0" };
        mLbl.AddThemeFontSizeOverride("font_size", 12);
        mLbl.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.3f));
        vbox.AddChild(mLbl);

        // Top-right: day counter
        var dayLbl = new Label { Text = "GUN 1" };
        dayLbl.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        dayLbl.Position = new Vector2(-80, 14);
        dayLbl.AddThemeFontSizeOverride("font_size", 16);
        dayLbl.AddThemeColorOverride("font_color", Colors.White);
        hud.AddChild(dayLbl);

        // Bottom-left: controls
        var ctrlLbl = new Label { Text = "WASD: Hareket   E: Etkilesim   SHIFT: Kos" };
        ctrlLbl.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        ctrlLbl.Position = new Vector2(10, -28);
        ctrlLbl.AddThemeFontSizeOverride("font_size", 11);
        ctrlLbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        hud.AddChild(ctrlLbl);

        // Pitch hint (hidden by default, shown when near gate)
        var pitchHint = new Label { Name = "PitchHint", Text = "E  Maca Gir" };
        pitchHint.UniqueNameInOwner = true;
        pitchHint.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        pitchHint.Position = new Vector2(10, -50);
        pitchHint.AddThemeFontSizeOverride("font_size", 15);
        pitchHint.AddThemeColorOverride("font_color", Colors.White);
        pitchHint.Visible = false;
        hud.AddChild(pitchHint);
    }

    // ─── Trees ───────────────────────────────────────────────────────────────────

    private void _PlaceTrees(Node root)
    {
        var positions = new (float x, float y)[]
        {
            (3,2),(5,3),(7,2),(10,4),(12,2),(15,3),(18,2),(22,4),
            (3,35),(6,36),(9,35),(13,37),(17,35),(21,36),(25,35),
            (45,2),(48,3),(51,2),(54,3),(57,2),
            (44,35),(47,36),(50,35),(53,36),(56,35),
            (3,8),(3,12),(3,16),(3,22),(3,27),(3,32),
            (56,8),(56,12),(56,16),(56,22),(56,27),(56,32),
        };

        foreach (var (tx, ty) in positions)
        {
            root.AddChild(new ColorRect
            {
                Color    = new Color(0.45f, 0.30f, 0.12f),
                Position = new Vector2(tx*TS+12, ty*TS+16),
                Size     = new Vector2(8, 14),
                ZIndex   = 0
            });
            _TreeTop(root, new Vector2(tx*TS + TS/2f, ty*TS + 10));
        }
    }

    private void _TreeTop(Node root, Vector2 center)
    {
        foreach (var (r, c) in new (float, Color)[]
        {
            (20f, new Color(0.15f, 0.45f, 0.12f)),
            (14f, new Color(0.20f, 0.55f, 0.15f)),
            ( 8f, new Color(0.28f, 0.65f, 0.20f)),
        })
        {
            int seg = 16;
            var pts = new Vector2[seg];
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.Tau;
                pts[i] = center + new Vector2(Mathf.Cos(a)*r, Mathf.Sin(a)*r);
            }
            root.AddChild(new Polygon2D { Polygon = pts, Color = c, ZIndex = 1 });
        }
    }

    // ─── Football Pitch ──────────────────────────────────────────────────────────
    //
    //  World coords:
    //    grass:  (px, py) → (px+pw, py+ph)  = (1130,200)→(1620,510)
    //    fence:  (fx, fy) → (fx+fw, fy+fh)  = (1116,186)→(1634,524)
    //    gate centre: (cx, fy+fh) = (1375, 524)

    private const float px  = 1130f;  // grass left
    private const float py  = 200f;   // grass top
    private const float pw  = 490f;   // grass width
    private const float ph  = 310f;   // grass height
    private const float cx  = px + pw / 2f;   // = 1375
    private const float cy  = py + ph / 2f;   // = 355
    private const float fp  = 14f;    // fence pad
    private const float fx  = px - fp;
    private const float fy  = py - fp;
    private const float fw  = pw + fp * 2f;
    private const float fh  = ph + fp * 2f;
    private const float gw  = 62f;    // gate opening width
    private const float gd  = 22f;    // goal depth (how far goals protrude)
    private const float gh  = 80f;    // goal opening height

    public static readonly Vector2 PitchGateWorld = new Vector2(cx, fy + fh); // (1375, 524)

    private void _BuildPitch(Node root)
    {
        _PitchSprite(root);
        _PitchFenceCollision(root);
    }

    // Single pixel-art sprite replaces all procedural pitch visuals.
    // Sprite is 620x465. Gate in reference is ~68% down (y≈316px in sprite).
    // Align that to game gate at y=524: sprite top = 524-316 = 208, center = 208+232 = 440.
    private void _PitchSprite(Node root)
    {
        var tex = GD.Load<Texture2D>("res://assets/places/pitch_world_sprite.png");
        if (tex == null) { GD.PrintErr("pitch_world_sprite.png not found"); return; }
        root.AddChild(new Sprite2D
        {
            Texture       = tex,
            Position      = new Vector2(cx, 440f),
            ZIndex        = 3,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        });
    }

    // Stone pavement surround
    private void _PitchPavement(Node root)
    {
        _R(root, new Vector2(fx-24, fy-24), new Vector2(fw+48, fh+48),
           new Color(0.58f, 0.56f, 0.52f), 1);
        // Slightly lighter inner area
        _R(root, new Vector2(fx-8, fy-8), new Vector2(fw+16, fh+16),
           new Color(0.62f, 0.60f, 0.56f), 2);
    }

    // Chain-link fence visual (4 thick sides)
    private void _PitchFenceVisual(Node root)
    {
        Color fc = new Color(0.22f, 0.26f, 0.30f);
        int   fz = 3;
        float ft = 7f; // fence thickness visual

        // Top
        _R(root, new Vector2(fx, fy), new Vector2(fw, ft), fc, fz);
        // Bottom with gate gap
        _R(root, new Vector2(fx, fy+fh-ft), new Vector2(cx-fx-gw/2f, ft), fc, fz);
        _R(root, new Vector2(cx+gw/2f, fy+fh-ft), new Vector2(fx+fw-(cx+gw/2f), ft), fc, fz);
        // Left
        _R(root, new Vector2(fx, fy), new Vector2(ft, fh), fc, fz);
        // Right
        _R(root, new Vector2(fx+fw-ft, fy), new Vector2(ft, fh), fc, fz);

        // Fence horizontal rails (cross-hatch suggestion)
        for (float ry = fy + 20f; ry < fy + fh - 14f; ry += 22f)
        {
            _R(root, new Vector2(fx+ft, ry), new Vector2(fw-ft*2, 1f),
               new Color(0.28f, 0.32f, 0.36f, 0.6f), fz);
        }
        for (float rx = fx + 20f; rx < fx + fw - 14f; rx += 22f)
        {
            _R(root, new Vector2(rx, fy+ft), new Vector2(1f, fh-ft*2),
               new Color(0.28f, 0.32f, 0.36f, 0.6f), fz);
        }
    }

    // Green grass field
    private void _PitchGrass(Node root)
    {
        _R(root, new Vector2(px, py), new Vector2(pw, ph), new Color(0.22f, 0.52f, 0.20f), 4);

        // Mow stripes (alternate)
        int   n  = 8;
        float sw = pw / n;
        for (int i = 0; i < n; i += 2)
        {
            _R(root, new Vector2(px + i*sw, py), new Vector2(sw, ph),
               new Color(0.18f, 0.47f, 0.17f), 5);
        }
    }

    // White chalk lines
    private void _PitchLines(Node root)
    {
        Color wh = Colors.White;
        float lw = 2f, z = 6;
        float inset = 8f;
        float lx = px + inset, ly = py + inset;
        float lw2 = pw - inset*2, lh2 = ph - inset*2;

        // Border
        _R(root, new Vector2(lx,       ly),          new Vector2(lw2, lw), wh, (int)z);
        _R(root, new Vector2(lx,       ly+lh2-lw),   new Vector2(lw2, lw), wh, (int)z);
        _R(root, new Vector2(lx,       ly),           new Vector2(lw, lh2), wh, (int)z);
        _R(root, new Vector2(lx+lw2-lw, ly),          new Vector2(lw, lh2), wh, (int)z);

        // Centre line (vertical)
        _R(root, new Vector2(cx - lw/2f, ly), new Vector2(lw, lh2), wh, (int)z);

        // Centre circle
        _PitchCircle(root, new Vector2(cx, cy), 44f, 32, wh, lw, (int)z);

        // Centre spot
        _PitchSpot(root, new Vector2(cx, cy), wh, 3.5f);

        // Penalty areas (depth 75px, height 180px)
        float pad = 75f, pah = 180f;
        _R(root, new Vector2(lx,          cy-pah/2f),     new Vector2(pad, lw), wh, (int)z);
        _R(root, new Vector2(lx,          cy+pah/2f-lw),  new Vector2(pad, lw), wh, (int)z);
        _R(root, new Vector2(lx+pad-lw,   cy-pah/2f),     new Vector2(lw, pah), wh, (int)z);
        _R(root, new Vector2(lx+lw2-pad,  cy-pah/2f),     new Vector2(pad, lw), wh, (int)z);
        _R(root, new Vector2(lx+lw2-pad,  cy+pah/2f-lw),  new Vector2(pad, lw), wh, (int)z);
        _R(root, new Vector2(lx+lw2-pad,  cy-pah/2f),     new Vector2(lw, pah), wh, (int)z);

        // Goal areas (depth 32px, height 90px)
        float gad = 32f, gah2 = 90f;
        _R(root, new Vector2(lx,            cy-gah2/2f),    new Vector2(gad, lw), wh, (int)z);
        _R(root, new Vector2(lx,            cy+gah2/2f-lw), new Vector2(gad, lw), wh, (int)z);
        _R(root, new Vector2(lx+gad-lw,     cy-gah2/2f),    new Vector2(lw, gah2), wh, (int)z);
        _R(root, new Vector2(lx+lw2-gad,    cy-gah2/2f),    new Vector2(gad, lw), wh, (int)z);
        _R(root, new Vector2(lx+lw2-gad,    cy+gah2/2f-lw), new Vector2(gad, lw), wh, (int)z);
        _R(root, new Vector2(lx+lw2-gad,    cy-gah2/2f),    new Vector2(lw, gah2), wh, (int)z);

        // Penalty spots
        _PitchSpot(root, new Vector2(lx+55f, cy), wh, 3f);
        _PitchSpot(root, new Vector2(lx+lw2-55f, cy), wh, 3f);
    }

    private static void _PitchCircle(Node root, Vector2 center, float r, int seg,
                                      Color c, float lw, int z)
    {
        for (int i = 0; i < seg; i++)
        {
            float a0 = i / (float)seg * Mathf.Tau, a1 = (i+1) / (float)seg * Mathf.Tau;
            var ln = new Line2D { DefaultColor = c, Width = lw, ZIndex = z };
            ln.AddPoint(center + new Vector2(Mathf.Cos(a0)*r, Mathf.Sin(a0)*r));
            ln.AddPoint(center + new Vector2(Mathf.Cos(a1)*r, Mathf.Sin(a1)*r));
            root.AddChild(ln);
        }
    }

    private static void _PitchSpot(Node root, Vector2 center, Color c, float r)
    {
        var pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.Tau;
            pts[i] = center + new Vector2(Mathf.Cos(a)*r, Mathf.Sin(a)*r);
        }
        root.AddChild(new Polygon2D { Polygon = pts, Color = c, ZIndex = 6 });
    }

    // Goals (small white rectangles on left/right)
    private void _PitchGoals(Node root)
    {
        Color wh = Colors.White;
        float gy = cy - gh/2f;

        // Left goal
        _R(root, new Vector2(px-gd, gy),       new Vector2(gd,  2f), wh, 7);  // top post
        _R(root, new Vector2(px-gd, gy+gh-2f), new Vector2(gd,  2f), wh, 7);  // bottom post
        _R(root, new Vector2(px-gd, gy),       new Vector2(2f, gh),  wh, 7);  // back post
        // Net
        _R(root, new Vector2(px-gd+2, gy+2), new Vector2(gd-4, gh-4),
           new Color(0.85f, 0.85f, 0.85f, 0.25f), 6);

        // Right goal
        _R(root, new Vector2(px+pw,     gy),       new Vector2(gd,  2f), wh, 7);
        _R(root, new Vector2(px+pw,     gy+gh-2f), new Vector2(gd,  2f), wh, 7);
        _R(root, new Vector2(px+pw+gd-2, gy),      new Vector2(2f, gh),  wh, 7);
        _R(root, new Vector2(px+pw+2,   gy+2), new Vector2(gd-4, gh-4),
           new Color(0.85f, 0.85f, 0.85f, 0.25f), 6);
    }

    // Corner floodlights
    private void _PitchLights(Node root)
    {
        Color pole = new Color(0.28f, 0.30f, 0.34f);
        Color lamp = new Color(1f, 0.95f, 0.80f, 0.9f);
        foreach (var (lx2, ly2) in new (float, float)[]
        {
            (fx+2, fy+2), (fx+fw-8, fy+2),
            (fx+2, fy+fh-58), (fx+fw-8, fy+fh-58)
        })
        {
            // Pole
            _R(root, new Vector2(lx2, ly2), new Vector2(5, 52), pole, 8);
            // Lamp head
            var pts = new Vector2[8];
            var lc2 = new Vector2(lx2+2.5f, ly2-4f);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.Tau;
                pts[i] = lc2 + new Vector2(Mathf.Cos(a)*7f, Mathf.Sin(a)*5f);
            }
            root.AddChild(new Polygon2D { Polygon = pts, Color = lamp, ZIndex = 9 });
        }
    }

    // Benches along left side
    private void _PitchBenches(Node root)
    {
        Color bench = new Color(0.48f, 0.32f, 0.16f);
        Color leg   = new Color(0.36f, 0.24f, 0.10f);
        float bx    = fx - 28f;
        foreach (float by in new float[] { cy - 90f, cy - 20f, cy + 50f })
        {
            _R(root, new Vector2(bx,    by),    new Vector2(22f, 8f), bench, 3);
            _R(root, new Vector2(bx+2,  by+8),  new Vector2(4f,  6f), leg,   3);
            _R(root, new Vector2(bx+16, by+8),  new Vector2(4f,  6f), leg,   3);
        }
    }

    // Gate opening + frame
    private void _PitchGate(Node root)
    {
        // Gate frame posts
        Color gateColor = new Color(0.20f, 0.24f, 0.28f);
        _R(root, new Vector2(cx-gw/2f-3, fy+fh-16), new Vector2(5f, 20f), gateColor, 8);
        _R(root, new Vector2(cx+gw/2f-2, fy+fh-16), new Vector2(5f, 20f), gateColor, 8);
        // Horizontal cross bar
        _R(root, new Vector2(cx-gw/2f-1, fy+fh-6), new Vector2(gw+2, 3f), gateColor, 9);

        // Small path leading south from gate
        _R(root, new Vector2(cx-12, fy+fh), new Vector2(24, 28),
           new Color(0.58f, 0.52f, 0.42f), 2);
    }

    // "YILDIZ KRAMPON" sign below gate
    private void _PitchSign(Node root)
    {
        float sx = cx - 56f, sy = fy + fh + 30f;
        _R(root, new Vector2(sx-4, sy-4), new Vector2(120f, 26f), new Color(0.20f, 0.18f, 0.10f), 4);
        var lbl = new Label { Text = "YILDIZ KRAMPON" };
        lbl.Position = new Vector2(sx, sy);
        lbl.AddThemeFontSizeOverride("font_size", 11);
        lbl.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        lbl.ZIndex = 5;
        root.AddChild(lbl);

        // "MAC VAR!" graffiti above north fence
        var graffiti = new Label { Text = "MAC VAR!" };
        graffiti.Position = new Vector2(cx - 36f, fy - 26f);
        graffiti.AddThemeFontSizeOverride("font_size", 12);
        graffiti.AddThemeColorOverride("font_color", new Color(0.3f, 0.5f, 0.9f));
        graffiti.ZIndex = 5;
        root.AddChild(graffiti);
    }

    // Fence collision walls (player can't walk through fence; gate is gap at bottom)
    private void _PitchFenceCollision(Node root)
    {
        // North fence
        _Wall(root, "PFenceN", new Vector2(cx,             fy+3),       new Vector2(fw,    10));
        // South fence (two halves with gate gap)
        float halfW = (fw - gw) / 2f;
        _Wall(root, "PFenceSL", new Vector2(fx+halfW/2f,   fy+fh-3),   new Vector2(halfW, 10));
        _Wall(root, "PFenceSR", new Vector2(fx+fw-halfW/2f, fy+fh-3),  new Vector2(halfW, 10));
        // East / West fence
        _Wall(root, "PFenceW", new Vector2(fx+3,       cy),             new Vector2(10, fh));
        _Wall(root, "PFenceE", new Vector2(fx+fw-3,    cy),             new Vector2(10, fh));
    }
}
