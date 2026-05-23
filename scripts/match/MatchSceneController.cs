using Godot;

/// Controller for MatchScene — the pixel-art neighbourhood football match.
///
/// Scene layer hierarchy (ZIndex):
///   -20  Background  — static pixel-art field image (field_bg.png)
///    -5  RegionOverlay (FieldRegions) — Area2D collision regions + debug draw
///     0  YSortLayer  — players, ball, any gameplay nodes (Y-sorted depth)
///    20  Foreground  — near-side fence, goal-front details (always in front)
///    30  HUD (CanvasLayer) — scoreboard, timers, hints
///
/// Extension points for future systems:
///   • Add actors/ball as children of YSortLayer (auto Y-sorted).
///   • Use Regions.IsInLeftGoal(pos) etc. for goal detection.
///   • Connect Area2D signals (body_entered) on Regions.GetNode<Area2D>("LeftGoalArea").
///   • Use FieldRegions static constants for spawn positions.
///   • F1 toggles the debug region overlay at runtime.
public partial class MatchSceneController : Node2D
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Export] public bool ShowDebugOverlay { get; set; } = true;

    // ── Public API for gameplay sub-systems ───────────────────────────────────
    public FieldRegions? Regions    { get; private set; }
    public Node2D?       YSortLayer { get; private set; }
    public Node2D?       Foreground { get; private set; }

    // ── Background asset path ─────────────────────────────────────────────────
    private const string FIELD_BG = "res://assets/match/field_bg.png";

    // ── _Ready ────────────────────────────────────────────────────────────────
    public override void _Ready()
    {
        BuildBackground();

        Regions = new FieldRegions();
        AddChild(Regions);
        Regions.Visible = ShowDebugOverlay;

        // Y-sorted gameplay layer — add all actors + ball here
        YSortLayer = new Node2D { Name = "YSortLayer", YSortEnabled = true, ZIndex = 0 };
        AddChild(YSortLayer);

        // Foreground — rendered above players (near fence, goal front details)
        Foreground = new Node2D { Name = "Foreground", ZIndex = 20 };
        AddChild(Foreground);
        BuildForegroundWalls();

        // HUD CanvasLayer
        BuildHUD();

        // Day/night tint — no sky strip for match scenes
        var dn = new DayNightLayer { ShowSky = false };
        AddChild(dn);
    }

    // ── Input ─────────────────────────────────────────────────────────────────
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.F1)
        {
            if (Regions != null)
            {
                Regions.Visible = !Regions.Visible;
                GD.Print($"[MatchScene] Debug overlay: {(Regions.Visible ? "ON" : "OFF")}");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── Background ────────────────────────────────────────────────────────────
    // The field_bg.png (1448 × 1086) is scaled to fill the 1280 × 720 viewport.
    // scaleX = 1280 / 1448 ≈ 0.884  |  scaleY = 720 / 1086 ≈ 0.663
    // Region polygons in FieldRegions.cs are calibrated for this transform.
    private void BuildBackground()
    {
        var bg = new Node2D { Name = "Background", ZIndex = -20 };
        AddChild(bg);

        Texture2D tex;
        if (ResourceLoader.Exists(FIELD_BG))
        {
            tex = GD.Load<Texture2D>(FIELD_BG);
        }
        else
        {
            // Fallback procedural green field — used only if image is missing
            GD.PushWarning($"[MatchScene] Field background not found at {FIELD_BG}");
            var img = Image.CreateEmpty(1280, 720, false, Image.Format.Rgb8);
            img.Fill(new Color(0.24f, 0.55f, 0.13f));
            tex = ImageTexture.CreateFromImage(img);
        }

        var sz  = tex.GetSize();
        var spr = new Sprite2D
        {
            Name          = "FieldBg",
            Texture       = tex,
            Centered      = false,
            Position      = Vector2.Zero,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            // Non-uniform scale to fill 1280 × 720 exactly
            Scale = sz.X > 0 && sz.Y > 0
                        ? new Vector2(1280f / sz.X, 720f / sz.Y)
                        : Vector2.One,
        };
        bg.AddChild(spr);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── Foreground collision walls ─────────────────────────────────────────────
    // Invisible StaticBody2D walls along the inner fence edges so the ball
    // and players are stopped by the fence geometry.
    // These must match FieldRegions boundary constants.
    private void BuildForegroundWalls()
    {
        if (Foreground == null) return;

        const float L = FieldRegions.FieldLeft;
        const float R = FieldRegions.FieldRight;
        const float T = FieldRegions.FieldTop;
        const float B = FieldRegions.FieldBottom;
        const float W = R - L; // field width
        const float H = B - T; // field height
        const float t = 8f;    // wall thickness (px)

        AddWall(Foreground, "WallNorth", new Rect2(L, T,     W, t));  // top fence
        AddWall(Foreground, "WallSouth", new Rect2(L, B - t, W, t));  // bottom fence
        AddWall(Foreground, "WallWest",  new Rect2(L, T,     t, H));  // left fence
        AddWall(Foreground, "WallEast",  new Rect2(R - t, T, t, H));  // right fence

        // Goal back-walls (stop the ball inside net)
        float gTop = 272f, gBot = 398f, gH = gBot - gTop;
        AddWall(Foreground, "GoalBackLeft",  new Rect2(90f,          gTop, t, gH));
        AddWall(Foreground, "GoalBackRight", new Rect2(1192f - t,    gTop, t, gH));
        AddWall(Foreground, "GoalTopLeft",   new Rect2(90f,          gTop, 80f, t));
        AddWall(Foreground, "GoalBotLeft",   new Rect2(90f,          gBot - t, 80f, t));
        AddWall(Foreground, "GoalTopRight",  new Rect2(1112f,        gTop, 80f, t));
        AddWall(Foreground, "GoalBotRight",  new Rect2(1112f,        gBot - t, 80f, t));
    }

    private static void AddWall(Node2D parent, string wallName, Rect2 rect)
    {
        var body = new StaticBody2D { Name = wallName };
        body.AddChild(new CollisionShape2D
        {
            Shape    = new RectangleShape2D { Size = rect.Size },
            Position = rect.Position + rect.Size * 0.5f,
        });
        parent.AddChild(body);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── HUD ───────────────────────────────────────────────────────────────────
    private Label? _scoreLabel;
    private Label? _hintLabel;

    private void BuildHUD()
    {
        var hud = new CanvasLayer { Name = "HUD", Layer = 10 };
        AddChild(hud);

        // Score display (top centre)
        _scoreLabel = new Label
        {
            Name              = "ScoreLabel",
            Text              = "0 — 0",
            HorizontalAlignment = HorizontalAlignment.Center,
            LayoutMode        = 1,
            AnchorsPreset     = (int)Control.LayoutPreset.TopWide,
            OffsetTop         = 8f,
            OffsetBottom      = 36f,
        };
        hud.AddChild(_scoreLabel);

        // Interaction hint (bottom centre)
        _hintLabel = new Label
        {
            Name              = "HintLabel",
            Visible           = false,
            Text              = "[E] Etkileş",
            HorizontalAlignment = HorizontalAlignment.Center,
            LayoutMode        = 1,
            AnchorsPreset     = (int)Control.LayoutPreset.BottomWide,
            OffsetBottom      = -8f,
            OffsetTop         = -36f,
        };
        hud.AddChild(_hintLabel);

        // Debug overlay toggle hint (top-left)
        var dbgHint = new Label
        {
            Name   = "DbgHint",
            Text   = "[F1] Bölgeleri Göster/Gizle",
            LayoutMode   = 1,
            AnchorsPreset = (int)Control.LayoutPreset.TopLeft,
            OffsetLeft  = 8f,
            OffsetTop   = 8f,
            OffsetRight = 260f,
            OffsetBottom = 28f,
            Modulate = new Color(1, 1, 1, 0.55f),
        };
        hud.AddChild(dbgHint);
    }

    // ── Public helpers ────────────────────────────────────────────────────────

    public void UpdateScore(int leftGoals, int rightGoals)
    {
        if (_scoreLabel != null)
            _scoreLabel.Text = $"{leftGoals}  —  {rightGoals}";
    }

    public void ShowHint(string text)
    {
        if (_hintLabel == null) return;
        _hintLabel.Text    = text;
        _hintLabel.Visible = true;
    }

    public void HideHint()
    {
        if (_hintLabel != null) _hintLabel.Visible = false;
    }
}
