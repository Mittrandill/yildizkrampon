using Godot;

/// Builds and manages the 6 match field regions for MatchScene.
///
/// Coordinate reference:
///   Background image: 1448 × 1086 px, displayed at 1280 × 720 (viewport fill).
///   scaleX = 1280 / 1448 ≈ 0.884  |  scaleY = 720 / 1086 ≈ 0.663
///   All world-space coordinates below are in the resulting 1280 × 720 frame.
///
/// Region signal API (connect via Area2D children):
///   body_entered / body_exited on each named Area2D child.
///
/// Query API (for AI / gameplay code):
///   IsInRegion(Vector2 worldPos, string regionName) → bool
///   + convenience helpers: IsInPlayableField, IsInLeftGoal, etc.
public partial class FieldRegions : Node2D
{
    // ── Region name constants ────────────────────────────────────────────────
    public const string PLAYABLE_FIELD  = "PlayableField";
    public const string LEFT_GOAL       = "LeftGoalArea";
    public const string RIGHT_GOAL      = "RightGoalArea";
    public const string LEFT_PENALTY    = "LeftPenaltyArea";
    public const string RIGHT_PENALTY   = "RightPenaltyArea";
    public const string CENTER_KICKOFF  = "CenterKickoffArea";

    // ── Field geometry — world space (1280 × 720) ────────────────────────────
    // ┌──────────────────────────────────────────────────────────────────────┐
    // │  NOTE: Tune these to match the rendered image by opening MatchScene  │
    // │  in the Godot editor with F1 overlay visible.                        │
    // └──────────────────────────────────────────────────────────────────────┘

    // Inner grass (inside all 4 fences)
    public static readonly Vector2[] PolyPlayableField =
    {
        new(170, 155), new(1108, 155),
        new(1108, 553), new(170, 553),
    };

    // Left goal mouth — net box behind the left fence
    public static readonly Vector2[] PolyLeftGoal =
    {
        new(90, 272), new(170, 272),
        new(170, 398), new(90, 398),
    };

    // Right goal mouth
    public static readonly Vector2[] PolyRightGoal =
    {
        new(1108, 272), new(1192, 272),
        new(1192, 398), new(1108, 398),
    };

    // Left penalty area (large box)
    public static readonly Vector2[] PolyLeftPenalty =
    {
        new(170, 207), new(298, 207),
        new(298, 446), new(170, 446),
    };

    // Right penalty area
    public static readonly Vector2[] PolyRightPenalty =
    {
        new(982, 207), new(1108, 207),
        new(1108, 446), new(982, 446),
    };

    // Centre kickoff circle — 16-gon, slightly elliptical to match 4:3→16:9 warp
    // Centre: (640, 354), rx=88 (horizontal), ry=66 (vertical)
    public static readonly Vector2[] PolyCenterCircle = MakeEllipse(new Vector2(640, 354), 88, 66, 16);

    // ── Key positions (for spawn, AI, etc.) ──────────────────────────────────
    public static readonly Vector2 FieldCenter       = new(640, 354);
    public static readonly Vector2 LeftGoalCenter    = new(128,  335);
    public static readonly Vector2 RightGoalCenter   = new(1152, 335);
    public static readonly Vector2 LeftPenaltySpot   = new(224, 327);
    public static readonly Vector2 RightPenaltySpot  = new(1058, 327);

    // Playable field extents (for clamping / boundary checks)
    public const float FieldLeft   = 170f;
    public const float FieldRight  = 1108f;
    public const float FieldTop    = 155f;
    public const float FieldBottom = 553f;

    // ── Debug colour palette ─────────────────────────────────────────────────
    private record RegionDef(string Name, Vector2[] Poly, Color Fill, string Label);

    private static readonly RegionDef[] Defs =
    {
        new(PLAYABLE_FIELD, PolyPlayableField,  new Color(0.00f, 0.88f, 0.12f, 0.16f), "PlayableField"),
        new(LEFT_GOAL,      PolyLeftGoal,       new Color(0.12f, 0.28f, 0.95f, 0.42f), "LeftGoal"),
        new(RIGHT_GOAL,     PolyRightGoal,      new Color(0.12f, 0.28f, 0.95f, 0.42f), "RightGoal"),
        new(LEFT_PENALTY,   PolyLeftPenalty,    new Color(0.96f, 0.86f, 0.10f, 0.28f), "LeftPenalty"),
        new(RIGHT_PENALTY,  PolyRightPenalty,   new Color(0.96f, 0.86f, 0.10f, 0.28f), "RightPenalty"),
        new(CENTER_KICKOFF, PolyCenterCircle,   new Color(0.88f, 0.10f, 0.92f, 0.25f), "CenterCircle"),
    };

    // ── Build ────────────────────────────────────────────────────────────────
    public override void _Ready()
    {
        Name    = "RegionOverlay";
        ZIndex  = -5;

        foreach (var def in Defs)
        {
            var area = new Area2D { Name = def.Name };

            // Monitoring collision (layer 8 = match_regions in future layer setup)
            var col = new CollisionPolygon2D { Polygon = def.Poly };
            area.AddChild(col);

            // Debug visualisation
            var dbg = new DebugRegionDraw
            {
                Points      = def.Poly,
                Fill        = def.Fill,
                Border      = new Color(def.Fill.R, def.Fill.G, def.Fill.B, 0.88f),
                RegionLabel = def.Label,
            };
            area.AddChild(dbg);

            AddChild(area);
        }
    }

    // ── Query helpers ────────────────────────────────────────────────────────

    /// Returns true if worldPos lies inside the named region.
    public bool IsInRegion(Vector2 worldPos, string regionName)
    {
        var area = GetNodeOrNull<Area2D>(regionName);
        if (area == null) return false;
        foreach (var child in area.GetChildren())
        {
            if (child is CollisionPolygon2D cpoly)
                return Geometry2D.IsPointInPolygon(worldPos, cpoly.Polygon);
        }
        return false;
    }

    public bool IsInPlayableField(Vector2 pos)    => IsInRegion(pos, PLAYABLE_FIELD);
    public bool IsInLeftGoal(Vector2 pos)          => IsInRegion(pos, LEFT_GOAL);
    public bool IsInRightGoal(Vector2 pos)         => IsInRegion(pos, RIGHT_GOAL);
    public bool IsInLeftPenaltyArea(Vector2 pos)   => IsInRegion(pos, LEFT_PENALTY);
    public bool IsInRightPenaltyArea(Vector2 pos)  => IsInRegion(pos, RIGHT_PENALTY);
    public bool IsInCenterCircle(Vector2 pos)      => IsInRegion(pos, CENTER_KICKOFF);

    /// Clamp position inside playable field bounds (for boundary collisions).
    public static Vector2 ClampToField(Vector2 pos) =>
        new(Mathf.Clamp(pos.X, FieldLeft, FieldRight),
            Mathf.Clamp(pos.Y, FieldTop,  FieldBottom));

    /// Returns the goal centre for a side ("left" / "right").
    public static Vector2 GoalCenter(string side) =>
        side == "left" ? LeftGoalCenter : RightGoalCenter;

    // ── Geometry ─────────────────────────────────────────────────────────────

    private static Vector2[] MakeEllipse(Vector2 c, float rx, float ry, int n)
    {
        var pts = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.Tau / n;
            pts[i] = c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
        }
        return pts;
    }
}
