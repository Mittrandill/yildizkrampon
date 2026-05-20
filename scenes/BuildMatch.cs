using Godot;

/// Run: dotnet build && godot --headless --script scenes/BuildMatch.cs
public partial class BuildMatch : SceneBuilderBase
{
    // Pitch dimensions centred at (0,0)
    private const float PW  = 960f;  // inner pitch width  (line to line)
    private const float PH  = 520f;  // inner pitch height
    private const float FW  = 1000f; // full grass width
    private const float FH  = 560f;  // full grass height
    private const float GH  = 120f;  // goal opening height
    private const float GDEPTH = 52f; // goal depth
    private const float LW  = 3f;    // chalk line width

    public override void _Initialize()
    {
        GD.Print("Generating: Match (FIFA top-down)");

        var temp = new Node();
        var root = new MatchScene(); root.Name = "Match";
        temp.AddChild(root);

        _BuildBackdrop(root);
        _BuildGrass(root);
        _BuildPitchLines(root);
        _BuildGoals(root);
        _BuildWalls(root);
        _BuildCornerFlags(root);
        _BuildPlayers(root);
        _BuildBall(root);
        _BuildCamera(root);
        _BuildHUD(root);
        root.AddChild(new MatchManager { Name = "MatchManager" });

        temp.RemoveChild(root); temp.Free();
        PackAndSave(root, "res://scenes/Match.tscn");
        Quit();
    }

    // ─── Backdrop ────────────────────────────────────────────────────────────────

    private static void _BuildBackdrop(Node root)
    {
        root.AddChild(new ColorRect
        {
            Color    = new Color(0.10f, 0.12f, 0.08f),
            Size     = new Vector2(2400f, 1400f),
            Position = new Vector2(-1200f, -700f),
            ZIndex   = -20
        });
    }

    // ─── Grass ───────────────────────────────────────────────────────────────────

    private static void _BuildGrass(Node root)
    {
        root.AddChild(new ColorRect
        {
            Color    = new Color(0.22f, 0.52f, 0.18f),
            Size     = new Vector2(FW, FH),
            Position = new Vector2(-FW / 2f, -FH / 2f),
            ZIndex   = -15
        });

        // Alternating mowed stripes
        int   n  = 10;
        float sw = FW / n;
        for (int i = 0; i < n; i += 2)
        {
            root.AddChild(new ColorRect
            {
                Color    = new Color(0.18f, 0.46f, 0.15f),
                Position = new Vector2(-FW / 2f + i * sw, -FH / 2f),
                Size     = new Vector2(sw, FH),
                ZIndex   = -14
            });
        }

        // Thin inset shadow border
        foreach (var (p, s) in new (Vector2, Vector2)[]
        {
            (new Vector2(-FW/2f,        -FH/2f),       new Vector2(FW,  4f)),
            (new Vector2(-FW/2f,         FH/2f - 4f),  new Vector2(FW,  4f)),
            (new Vector2(-FW/2f,        -FH/2f),       new Vector2( 4f, FH)),
            (new Vector2( FW/2f - 4f,  -FH/2f),       new Vector2( 4f, FH)),
        })
        {
            root.AddChild(new ColorRect
                { Color = new Color(0f, 0f, 0f, 0.3f), Position = p, Size = s, ZIndex = -13 });
        }
    }

    // ─── Pitch lines ─────────────────────────────────────────────────────────────

    private static void _BuildPitchLines(Node root)
    {
        float hw = PW / 2f, hh = PH / 2f;
        var   wh = Colors.White;

        // Outer pitch border
        _Rect(root, new Vector2(-hw,    -hh),      new Vector2(PW, LW), wh);
        _Rect(root, new Vector2(-hw,     hh - LW), new Vector2(PW, LW), wh);
        _Rect(root, new Vector2(-hw,    -hh),      new Vector2(LW, PH), wh);
        _Rect(root, new Vector2( hw-LW, -hh),      new Vector2(LW, PH), wh);

        // Centre line
        _Rect(root, new Vector2(-LW/2f, -hh), new Vector2(LW, PH), wh);

        // Centre circle + spot
        _LineCircle(root, Vector2.Zero, 66f, 48, wh, LW);
        _Spot(root, Vector2.Zero, wh, 4.5f);

        // Penalty areas
        float pad = 135f, pah = 240f;
        _Box3(root, -hw, -pah/2f,  pad, pah, wh, LW, leftOpen: false);  // left PA (right edge open at pitch line)
        _Box3(root, hw-pad, -pah/2f, pad, pah, wh, LW, leftOpen: true); // right PA

        // Goal areas
        float gad = 55f, gah = 116f;
        _Box3(root, -hw, -gah/2f,  gad, gah, wh, LW, leftOpen: false);
        _Box3(root, hw-gad, -gah/2f, gad, gah, wh, LW, leftOpen: true);

        // Penalty spots
        _Spot(root, new Vector2(-hw + 100f, 0), wh, 4f);
        _Spot(root, new Vector2( hw - 100f, 0), wh, 4f);

        // Corner arcs
        float cr = 15f;
        _Arc(root, new Vector2(-hw, -hh),   0f,  90f, cr, wh, LW);
        _Arc(root, new Vector2( hw, -hh),  90f, 180f, cr, wh, LW);
        _Arc(root, new Vector2(-hw,  hh), 270f, 360f, cr, wh, LW);
        _Arc(root, new Vector2( hw,  hh), 180f, 270f, cr, wh, LW);
    }

    // 3-sided box (one side missing — the side touching the pitch boundary)
    private static void _Box3(Node root, float x, float y, float w, float h,
                               Color c, float lw, bool leftOpen)
    {
        _Rect(root, new Vector2(x,          y),         new Vector2(w, lw), c);  // top
        _Rect(root, new Vector2(x,          y + h - lw), new Vector2(w, lw), c); // bottom
        if (leftOpen)
            _Rect(root, new Vector2(x,      y), new Vector2(lw, h), c);           // left side
        else
            _Rect(root, new Vector2(x+w-lw, y), new Vector2(lw, h), c);           // right side
    }

    private static void _Rect(Node root, Vector2 pos, Vector2 size, Color c, int z = 0)
        => root.AddChild(new ColorRect { Color = c, Position = pos, Size = size, ZIndex = z });

    private static void _Spot(Node root, Vector2 center, Color c, float r)
    {
        var pts = new Vector2[12];
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * Mathf.Tau;
            pts[i] = center + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        root.AddChild(new Polygon2D { Color = c, Polygon = pts, ZIndex = 0 });
    }

    private static void _LineCircle(Node root, Vector2 ctr, float r, int seg, Color c, float lw)
    {
        for (int i = 0; i < seg; i++)
        {
            float a0 = i / (float)seg * Mathf.Tau, a1 = (i + 1) / (float)seg * Mathf.Tau;
            var ln = new Line2D { DefaultColor = c, Width = lw, ZIndex = 0 };
            ln.AddPoint(ctr + new Vector2(Mathf.Cos(a0)*r, Mathf.Sin(a0)*r));
            ln.AddPoint(ctr + new Vector2(Mathf.Cos(a1)*r, Mathf.Sin(a1)*r));
            root.AddChild(ln);
        }
    }

    private static void _Arc(Node root, Vector2 corner, float a0, float a1, float r, Color c, float lw)
    {
        var ln = new Line2D { DefaultColor = c, Width = lw, ZIndex = 0 };
        for (int i = 0; i <= 10; i++)
        {
            float t = i / 10f;
            float a = Mathf.DegToRad(Mathf.Lerp(a0, a1, t));
            ln.AddPoint(corner + new Vector2(Mathf.Cos(a)*r, Mathf.Sin(a)*r));
        }
        root.AddChild(ln);
    }

    // ─── Goals ───────────────────────────────────────────────────────────────────

    private static void _BuildGoals(Node root)
    {
        float hw = PW / 2f, ghalf = GH / 2f;

        _GoalVisual(root, -hw - GDEPTH, -ghalf, GDEPTH, GH, isLeft: true);
        root.AddChild(_GoalArea("GoalLeft",  new Vector2(-hw - GDEPTH / 2f, 0), new Vector2(GDEPTH - 4, GH - 4)));

        _GoalVisual(root, hw, -ghalf, GDEPTH, GH, isLeft: false);
        root.AddChild(_GoalArea("GoalRight", new Vector2( hw + GDEPTH / 2f, 0), new Vector2(GDEPTH - 4, GH - 4)));
    }

    private static void _GoalVisual(Node root, float x, float y, float w, float h, bool isLeft)
    {
        root.AddChild(new ColorRect
        {
            Color    = new Color(0.78f, 0.78f, 0.78f, 0.22f),
            Position = new Vector2(x, y),
            Size     = new Vector2(w, h),
            ZIndex   = 1
        });

        // Net grid
        for (float ny = y + 10f; ny < y + h - 2f; ny += 10f)
            _Rect(root, new Vector2(x, ny), new Vector2(w, 1f), new Color(1f, 1f, 1f, 0.3f), 1);
        for (float nx = x + 10f; nx < x + w - 2f; nx += 10f)
            _Rect(root, new Vector2(nx, y), new Vector2(1f, h), new Color(1f, 1f, 1f, 0.3f), 1);

        // Posts
        float pW = 4f;
        var   pc = Colors.White;
        _Rect(root, new Vector2(x,       y),           new Vector2(w, pW), pc, 3);
        _Rect(root, new Vector2(x,       y + h - pW),  new Vector2(w, pW), pc, 3);
        if (isLeft)
            _Rect(root, new Vector2(x, y), new Vector2(pW, h), pc, 3);
        else
            _Rect(root, new Vector2(x + w - pW, y), new Vector2(pW, h), pc, 3);
    }

    private static Area2D _GoalArea(string name, Vector2 center, Vector2 size)
    {
        var area = new Area2D { Name = name };
        area.UniqueNameInOwner = true;
        area.CollisionLayer    = 8u;
        area.CollisionMask     = 4u;
        var col = new CollisionShape2D();
        col.Shape    = new RectangleShape2D { Size = size };
        col.Position = center;
        area.AddChild(col);
        return area;
    }

    // ─── Walls ───────────────────────────────────────────────────────────────────

    private static void _BuildWalls(Node root)
    {
        float hw    = PW / 2f;
        float hh    = PH / 2f;
        float ghalf = GH / 2f;
        float gb    = hw + GDEPTH + 8f;
        float sideH = hh - ghalf - 4f;

        // Top / bottom across full width
        _Wall(root, "WallTop",    new Vector2(0, -(hh+10)), new Vector2(gb*2+20, 20));
        _Wall(root, "WallBottom", new Vector2(0,  (hh+10)), new Vector2(gb*2+20, 20));

        // Left side walls (above & below goal)
        float cy = ghalf + sideH / 2f + 4f;
        _Wall(root, "LeftWallTop",  new Vector2(-(hw+4), -cy), new Vector2(12, sideH));
        _Wall(root, "LeftWallBot",  new Vector2(-(hw+4),  cy), new Vector2(12, sideH));
        _Wall(root, "LeftGoalBack", new Vector2(-(hw+GDEPTH+4), 0), new Vector2(12, GH + 10));

        // Right side walls
        _Wall(root, "RightWallTop",  new Vector2( (hw+4), -cy), new Vector2(12, sideH));
        _Wall(root, "RightWallBot",  new Vector2( (hw+4),  cy), new Vector2(12, sideH));
        _Wall(root, "RightGoalBack", new Vector2( (hw+GDEPTH+4), 0), new Vector2(12, GH + 10));
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

    // ─── Players ─────────────────────────────────────────────────────────────────

    private static void _BuildPlayers(Node root)
    {
        var ctr = new Node { Name = "Players" };
        root.AddChild(ctr);

        // Human player (blue team FWD, starts centre-left)
        var human = new MatchPlayer { Name = "MatchPlayer", ZIndex = 5 };
        human.UniqueNameInOwner = true;
        human.Position = new Vector2(-50f, 30f);
        ctr.AddChild(human);

        // Blue AI: GK, DEF_L, DEF_R, MID  (human covers FWD slot)
        var blueSlots = new (string n, Vector2 p, FootballAI.Role r)[]
        {
            ("BlueGK",  new Vector2(-455f,   0f), FootballAI.Role.GK),
            ("BlueCB0", new Vector2(-290f, -95f), FootballAI.Role.DEF_L),
            ("BlueCB1", new Vector2(-290f,  95f), FootballAI.Role.DEF_R),
            ("BlueMF",  new Vector2(-130f, -72f), FootballAI.Role.MID),
        };
        foreach (var (n, pos, role) in blueSlots)
        {
            var ai = new FootballAI { Name = n, ZIndex = 5, Position = pos };
            ai.IsBlueTeam  = true;
            ai.BasePos     = pos;
            ai.PlayerRole  = role;
            ai.AttacksRight = true; // blue attacks right by default
            ai.AddToGroup("team_blue");
            ctr.AddChild(ai);
        }

        // Red AI: GK, DEF_L, DEF_R, MID, FWD
        var redSlots = new (string n, Vector2 p, FootballAI.Role r)[]
        {
            ("RedGK",  new Vector2(455f,   0f), FootballAI.Role.GK),
            ("RedCB0", new Vector2(290f, -95f), FootballAI.Role.DEF_L),
            ("RedCB1", new Vector2(290f,  95f), FootballAI.Role.DEF_R),
            ("RedMF",  new Vector2(130f, -72f), FootballAI.Role.MID),
            ("RedFW",  new Vector2(210f,   0f), FootballAI.Role.FWD),
        };
        foreach (var (n, pos, role) in redSlots)
        {
            var ai = new FootballAI { Name = n, ZIndex = 5, Position = pos };
            ai.IsBlueTeam  = false;
            ai.BasePos     = pos;
            ai.PlayerRole  = role;
            ai.AttacksRight = false; // red attacks left by default
            ai.AddToGroup("team_red");
            ctr.AddChild(ai);
        }
    }

    // ─── Ball ────────────────────────────────────────────────────────────────────

    private static void _BuildBall(Node root)
    {
        var ball = new Football { Name = "Ball", ZIndex = 6 };
        ball.CollisionLayer = 4u;
        ball.CollisionMask  = 16u;
        ball.Position = Vector2.Zero;
        root.AddChild(ball);
    }

    // ─── Corner Flags ────────────────────────────────────────────────────────────

    private static void _BuildCornerFlags(Node root)
    {
        float hw = PW / 2f, hh = PH / 2f;
        Color flagColor = new Color(0.0f, 0.9f, 0.85f);
        foreach (var (cx2, cy2) in new (float, float)[]
        {
            (-hw, -hh), (hw, -hh), (-hw, hh), (hw, hh)
        })
        {
            // Flag pole
            root.AddChild(new ColorRect
            {
                Color    = Colors.White,
                Position = new Vector2(cx2 - 1f, cy2 - 12f),
                Size     = new Vector2(2f, 14f),
                ZIndex   = 5
            });
            // Flag pennant
            var pts = new Vector2[]
            {
                new Vector2(cx2 + 1f, cy2 - 12f),
                new Vector2(cx2 + 8f, cy2 - 9f),
                new Vector2(cx2 + 1f, cy2 - 6f),
            };
            root.AddChild(new Polygon2D { Polygon = pts, Color = flagColor, ZIndex = 5 });
            // Corner spot
            var spot = new Polygon2D { Color = flagColor, ZIndex = 4 };
            var spts = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.Tau;
                spts[i] = new Vector2(cx2 + Mathf.Cos(a)*4f, cy2 + Mathf.Sin(a)*4f);
            }
            spot.Polygon = spts;
            root.AddChild(spot);
        }
    }

    // ─── Camera ──────────────────────────────────────────────────────────────────

    private static void _BuildCamera(Node root)
    {
        root.AddChild(new Camera2D
        {
            Name                     = "Camera",
            Zoom                     = new Vector2(1.0f, 1.0f),
            PositionSmoothingEnabled = false,
        });
    }

    // ─── HUD ─────────────────────────────────────────────────────────────────────

    private static void _BuildHUD(Node root)
    {
        var hud = new CanvasLayer { Name = "HUD", Layer = 10 };
        root.AddChild(hud);

        // Score panel (top centre)
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.Position = new Vector2(470f, 8f);
        var sf = new StyleBoxFlat
        {
            BgColor                = new Color(0f, 0f, 0f, 0.78f),
            CornerRadiusTopLeft    = 8, CornerRadiusTopRight    = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 18, ContentMarginRight = 18,
            ContentMarginTop  = 8,  ContentMarginBottom = 8,
        };
        panel.AddThemeStyleboxOverride("panel", sf);
        hud.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        panel.AddChild(vbox);

        // Team row
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 22);
        row.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(row);
        _HudLabel(row, "MAVİ",    new Color(0.5f, 0.78f, 1f),  13);
        _HudLabel(row, "vs",      new Color(0.5f, 0.5f,  0.5f), 10);
        _HudLabel(row, "KIRMIZI", new Color(1f,   0.45f, 0.45f), 13);

        var score = new Label { Name = "ScoreLabel", Text = "0  —  0" };
        score.UniqueNameInOwner   = true;
        score.HorizontalAlignment = HorizontalAlignment.Center;
        score.AddThemeColorOverride("font_color", Colors.White);
        score.AddThemeFontSizeOverride("font_size", 32);
        vbox.AddChild(score);

        var timer = new Label { Name = "TimerLabel", Text = "3:00" };
        timer.UniqueNameInOwner   = true;
        timer.HorizontalAlignment = HorizontalAlignment.Center;
        timer.AddThemeColorOverride("font_color", new Color(0.72f, 0.72f, 0.72f));
        timer.AddThemeFontSizeOverride("font_size", 15);
        vbox.AddChild(timer);

        // Goal announcement (centre screen)
        var ann = new Label { Name = "GoalAnnounce", Text = "" };
        ann.UniqueNameInOwner   = true;
        ann.SetAnchorsPreset(Control.LayoutPreset.Center);
        ann.Position            = new Vector2(-220f, -22f);
        ann.HorizontalAlignment = HorizontalAlignment.Center;
        ann.AddThemeFontSizeOverride("font_size", 40);
        ann.Visible = false;
        hud.AddChild(ann);

        // Controls hint
        var ctrl = new Label { Text = "WASD: Hareket   SPACE: Şut/Kafa   SHIFT: Sprint   CTRL: Kayma   ESC: Çık" };
        ctrl.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        ctrl.Position = new Vector2(8f, -22f);
        ctrl.AddThemeColorOverride("font_color", new Color(0.52f, 0.52f, 0.52f));
        ctrl.AddThemeFontSizeOverride("font_size", 11);
        hud.AddChild(ctrl);

        // Stamina label bottom-left above controls
        var staLbl = new Label { Text = "KONDISYON" };
        staLbl.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        staLbl.Position = new Vector2(8f, -52f);
        staLbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        staLbl.AddThemeFontSizeOverride("font_size", 10);
        hud.AddChild(staLbl);
    }

    private static void _HudLabel(Node parent, string text, Color color, int size)
    {
        var lbl = new Label { Text = text };
        lbl.AddThemeColorOverride("font_color", color);
        lbl.AddThemeFontSizeOverride("font_size", size);
        parent.AddChild(lbl);
    }
}
