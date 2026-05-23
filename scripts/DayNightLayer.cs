using Godot;
using System.Collections.Generic;

/// Stardew Valley–style pixel-art day/night overlay.
/// Drop into any Node2D scene:  AddChild(new DayNightLayer());
///
/// Creates:
///   • CanvasModulate  — smooth ambient tint on the main canvas.
///   • Sky strip       — gradient background, drifting clouds, sun, moon, stars
///                       (pinned to screen via CanvasLayer, so it never scrolls).
///   • Season overlay  — world-space tint for snow / summer heat (optional).
///   • Clock panel     — compact Stardew-style HUD panel.
public partial class DayNightLayer : Node2D
{
    // ── Settings ──────────────────────────────────────────────────────────────
    [Export] public bool ShowSky { get; set; } = true;

    /// Screen-space rect for the sky strip (ref resolution 1280×720).
    [Export] public Rect2 SkyRect { get; set; } = new Rect2(0f, 0f, 1280f, 80f);

    // ── Internal nodes ────────────────────────────────────────────────────────
    private CanvasModulate? _mod;
    private CanvasLayer?    _hud;

    // Sky
    private ColorRect?  _skyBg;
    private Polygon2D?  _sun;
    private Polygon2D?  _sunGlow;
    private Polygon2D?  _moon;
    private Polygon2D?  _moonShadow;
    private Node2D?     _stars;
    private Node2D?     _cloudRoot;

    // Clouds
    private readonly List<ColorRect> _clouds = new();
    private readonly float[]         _cloudX     = new float[4];
    private readonly float[]         _cloudY     = new float[4];
    private readonly float[]         _cloudSpeed = new float[4];
    private readonly float[]         _cloudW     = new float[4];

    // Clock
    private Label?     _clock;
    private ColorRect? _clockBg;

    // Seasonal overlay (world-map only)
    private ColorRect? _seasonOverlay;

    // ── Stardew Valley sky palette ────────────────────────────────────────────
    //   Time values match TimeManager.TimeOfDay boundaries.
    private static readonly (float t, Color c)[] SkyCurve =
    {
        (0.000f, new Color(0.106f, 0.122f, 0.196f)), // 00:00 midnight
        (0.180f, new Color(0.145f, 0.165f, 0.290f)), // 04:19 deep night
        (0.205f, new Color(0.784f, 0.376f, 0.235f)), // 04:55 dawn burst
        (0.240f, new Color(0.910f, 0.627f, 0.314f)), // 05:46 golden sunrise
        (0.300f, new Color(0.565f, 0.784f, 0.863f)), // 07:12 clear morning
        (0.500f, new Color(0.408f, 0.682f, 0.847f)), // noon — classic Stardew blue
        (0.650f, new Color(0.471f, 0.706f, 0.816f)), // 15:36 warm afternoon
        (0.710f, new Color(0.910f, 0.580f, 0.314f)), // 17:02 golden hour
        (0.760f, new Color(0.882f, 0.345f, 0.125f)), // 18:14 sunset
        (0.800f, new Color(0.627f, 0.188f, 0.408f)), // 19:12 dusk
        (0.840f, new Color(0.220f, 0.145f, 0.392f)), // 20:10 twilight
        (0.900f, new Color(0.125f, 0.125f, 0.235f)), // 21:36 night
        (1.000f, new Color(0.106f, 0.122f, 0.196f)), // midnight
    };

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    public override void _Ready()
    {
        _mod = new CanvasModulate { Name = "DayNightMod", Color = Colors.White };
        AddChild(_mod);

        _hud = new CanvasLayer { Name = "DayNightHUD", Layer = 8 };
        AddChild(_hud);

        if (ShowSky)
        {
            BuildSkyBg();
            BuildClouds();
            BuildSun();
            BuildMoon();
            BuildStars();

            // World-space season overlay (behind everything at ZIndex -16).
            _seasonOverlay = new ColorRect
            {
                Name        = "SeasonOverlay",
                Position    = new Vector2(-2000f, -2000f),
                Size        = new Vector2(6000f, 6000f),
                Color       = Colors.Transparent,
                ZIndex      = -16,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            AddChild(_seasonOverlay);
        }

        BuildClock();
        Refresh(0f);
    }

    public override void _Process(double delta)
    {
        Refresh((float)delta);
    }

    // ── Master update ─────────────────────────────────────────────────────────
    private void Refresh(float dt)
    {
        var tm = TimeManager.Instance;
        if (tm == null) return;
        float t = tm.NormalizedTime;

        // Ambient modulate — very smooth Lerp so it never pops.
        if (_mod != null)
            _mod.Color = _mod.Color.Lerp(tm.CombinedColor(), 0.8f * dt + 0.001f);

        if (ShowSky)
        {
            UpdateSkyBg(t);
            UpdateClouds(t, dt);
            UpdateSun(t);
            UpdateMoon(t);
            UpdateStars(t);
            UpdateSeasonOverlay(tm.CurrentSeason);
        }

        UpdateClock(tm);
    }

    // ── Sky background ────────────────────────────────────────────────────────
    private void BuildSkyBg()
    {
        _skyBg = new ColorRect
        {
            Name     = "SkyBg",
            Position = SkyRect.Position,
            Size     = SkyRect.Size,
            Color    = new Color(0.408f, 0.682f, 0.847f)
        };
        _hud!.AddChild(_skyBg);
    }

    private void UpdateSkyBg(float t)
    {
        if (_skyBg == null) return;
        _skyBg.Color = _skyBg.Color.Lerp(SampleCurve(SkyCurve, t), 0.02f);
    }

    // ── Clouds ────────────────────────────────────────────────────────────────
    // Cloud shape: a 2D grid of 4×4 px blocks forming a bumpy cloud silhouette.
    // Represented as a list of ColorRect children of a common root.

    private static readonly Vector2I[] CloudShape =
    {
        //  col, row pairs (each unit = 8 px in HUD coords)
        new( 1, 0), new( 2, 0),
        new( 0, 1), new( 1, 1), new( 2, 1), new( 3, 1),
        new( 0, 2), new( 1, 2), new( 2, 2), new( 3, 2), new( 4, 2),
        new( 1, 3), new( 2, 3), new( 3, 3),
    };

    private void BuildClouds()
    {
        _cloudRoot = new Node2D { Name = "Clouds" };
        _hud!.AddChild(_cloudRoot);

        float[] startX = { 80f, 380f, 680f, 1020f };
        float[] startY = { 6f, 14f, 8f, 18f };
        float[] speeds  = { 9f, 13f, 7f, 11f };
        float[] widths  = { 1f, 1.2f, 0.8f, 1.1f };   // scale multipliers

        for (int ci = 0; ci < 4; ci++)
        {
            _cloudX[ci]     = startX[ci];
            _cloudY[ci]     = startY[ci];
            _cloudSpeed[ci] = speeds[ci];
            _cloudW[ci]     = widths[ci];

            var root = new Node2D { Name = $"Cloud{ci}", Position = new Vector2(_cloudX[ci], _cloudY[ci]) };
            _cloudRoot.AddChild(root);

            const int bk = 8; // block pixel size in HUD coords
            Color cloudMain   = new(0.941f, 0.929f, 0.882f);  // warm cream-white
            Color cloudShadow = new(0.780f, 0.769f, 0.729f);  // darker underside

            foreach (var cell in CloudShape)
            {
                bool isShadow = cell.Y >= 2;
                var rect = new ColorRect
                {
                    Position = new Vector2(cell.X * bk, cell.Y * bk),
                    Size     = new Vector2(bk, bk),
                    Color    = isShadow ? cloudShadow : cloudMain
                };
                root.AddChild(rect);
                if (ci == 0) _clouds.Add(rect); // track just for reference
            }

            _cloudRoot.GetChild<Node2D>(ci).Scale = new Vector2(widths[ci], 1f);
        }
    }

    private void UpdateClouds(float t, float dt)
    {
        if (_cloudRoot == null) return;

        // Clouds invisible at night.
        float cloudAlpha = t < 0.20f ? 0f
                         : t < 0.27f ? (t - 0.20f) / 0.07f
                         : t > 0.78f ? (0.85f - t) / 0.07f
                         : 1f;
        _cloudRoot.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(cloudAlpha, 0f, 1f));

        // Warm tint at sunrise/sunset.
        Color tint = t is > 0.20f and < 0.30f
            ? new Color(1f, 0.80f, 0.65f) :
            t is > 0.70f and < 0.82f
            ? new Color(1f, 0.72f, 0.55f) : Colors.White;
        _cloudRoot.SelfModulate = tint;

        // Drift each cloud independently.
        for (int ci = 0; ci < 4; ci++)
        {
            _cloudX[ci] -= _cloudSpeed[ci] * dt;
            float cloudWidth = 5 * 8 * _cloudW[ci]; // approx pixel width of cloud
            if (_cloudX[ci] < -cloudWidth - 20f)
                _cloudX[ci] = SkyRect.End.X + 20f;

            var cloudNode = _cloudRoot.GetChild<Node2D>(ci);
            cloudNode.Position = new Vector2(_cloudX[ci], _cloudY[ci]);
        }
    }

    // ── Sun ───────────────────────────────────────────────────────────────────
    private void BuildSun()
    {
        // Outer glow ring (slightly larger, semi-transparent).
        _sunGlow = PixelPolygon(26f, 16, new Color(1.00f, 0.88f, 0.40f, 0.40f), "SunGlow");
        _hud!.AddChild(_sunGlow);

        // Main sun body — warm Stardew golden-yellow.
        _sun = PixelPolygon(20f, 16, new Color(1.00f, 0.875f, 0.220f), "Sun");
        _hud.AddChild(_sun);
    }

    private void UpdateSun(float t)
    {
        if (_sun == null || _sunGlow == null) return;

        // Arc: sun rises at 0.21 (east/right), sets at 0.79 (west/left).
        float cx   = SkyRect.Position.X + SkyRect.Size.X * 0.5f;
        float cy   = SkyRect.End.Y + 28f;       // pivot at horizon
        float arc  = SkyRect.Size.X  * 0.42f;

        float sunT  = Mathf.InverseLerp(0.210f, 0.790f, t);
        float angle = Mathf.Pi * (1f - sunT);
        var   pos   = new Vector2(cx + Mathf.Cos(angle) * arc, cy - Mathf.Sin(angle) * arc);
        _sun.Position     = pos;
        _sunGlow.Position = pos;

        bool visible = t > 0.200f && t < 0.800f;
        _sun.Visible     = visible;
        _sunGlow.Visible = visible;

        float fadeAlpha = t < 0.240f ? (t - 0.200f) / 0.040f
                        : t > 0.760f ? (0.800f - t) / 0.040f : 1f;

        // Sunset colouring: sun turns deeper orange near horizon.
        Color sunColor = t is > 0.680f and < 0.800f
            ? new Color(1.00f, 0.580f, 0.180f)
            : t is > 0.200f and < 0.270f
            ? new Color(1.00f, 0.640f, 0.220f)
            : new Color(1.00f, 0.875f, 0.220f);

        _sun.Color     = sunColor;
        _sun.Modulate  = new Color(1f, 1f, 1f, Mathf.Clamp(fadeAlpha, 0f, 1f));
        _sunGlow.Modulate = _sun.Modulate;
    }

    // ── Moon ──────────────────────────────────────────────────────────────────
    private void BuildMoon()
    {
        // Moon disc — cool blue-white.
        _moon = PixelPolygon(15f, 14, new Color(0.863f, 0.902f, 0.969f), "Moon");
        _hud!.AddChild(_moon);

        // Shadow overlay polygon creates a crescent effect.
        _moonShadow = PixelPolygon(13f, 14, new Color(0.145f, 0.165f, 0.310f, 0.72f), "MoonShadow");
        _hud.AddChild(_moonShadow);
    }

    private void UpdateMoon(float t)
    {
        if (_moon == null || _moonShadow == null) return;

        float cx  = SkyRect.Position.X + SkyRect.Size.X * 0.5f;
        float cy  = SkyRect.End.Y + 28f;
        float arc = SkyRect.Size.X * 0.42f;

        float norm = t < 0.210f ? t + 1f : t;
        float moonT = Mathf.InverseLerp(0.790f, 1.210f, norm);
        float angle = Mathf.Pi * (1f - moonT);
        var pos = new Vector2(cx + Mathf.Cos(angle) * arc, cy - Mathf.Sin(angle) * arc);
        _moon.Position       = pos;
        _moonShadow.Position = pos + new Vector2(-5f, -3f);  // offset for crescent

        bool visible = t < 0.215f || t > 0.775f;
        _moon.Visible       = visible;
        _moonShadow.Visible = visible;
    }

    // ── Stars ─────────────────────────────────────────────────────────────────
    private void BuildStars()
    {
        _stars = new Node2D { Name = "Stars" };
        _hud!.AddChild(_stars);

        var rng = new RandomNumberGenerator();
        rng.Seed = 2024;
        for (int i = 0; i < 96; i++)
        {
            float x   = rng.RandfRange(SkyRect.Position.X + 2f, SkyRect.End.X - 2f);
            float y   = rng.RandfRange(SkyRect.Position.Y + 1f, SkyRect.End.Y - 4f);
            int   sz  = i % 7 == 0 ? 2 : 1;     // occasional big star
            float br  = rng.RandfRange(0.40f, 1.0f);

            // Slight colour variety: most white, some blue-tinted, some warm.
            Color col = (i % 5 == 0)
                ? new Color(0.80f, 0.85f, 1.00f, br) :
                (i % 7 == 0)
                ? new Color(1.00f, 0.95f, 0.80f, br)
                : new Color(1.00f, 1.00f, 1.00f, br);

            _stars.AddChild(new ColorRect
            {
                Position = new Vector2(x, y),
                Size     = new Vector2(sz, sz),
                Color    = col
            });
        }
    }

    private void UpdateStars(float t)
    {
        if (_stars == null) return;
        float alpha = t < 0.200f ? 1f
                    : t < 0.270f ? (0.270f - t) / 0.070f
                    : t > 0.770f ? (t - 0.770f) / 0.060f : 0f;
        _stars.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(alpha, 0f, 1f));
    }

    // ── Seasonal overlay ──────────────────────────────────────────────────────
    private void UpdateSeasonOverlay(TimeManager.Season season)
    {
        if (_seasonOverlay == null) return;
        _seasonOverlay.Color = season switch
        {
            TimeManager.Season.Winter => new Color(0.88f, 0.93f, 1.00f, 0.12f),
            TimeManager.Season.Summer => new Color(1.00f, 0.97f, 0.80f, 0.04f),
            _                         => Colors.Transparent
        };
    }

    // ── Clock panel ───────────────────────────────────────────────────────────
    private void BuildClock()
    {
        if (_hud == null) return;
        float topY = ShowSky ? SkyRect.End.Y + 6f : 8f;

        // Stardew-style rounded dark panel (approximated with ColorRect).
        _clockBg = new ColorRect
        {
            Position = new Vector2(8f, topY),
            Size     = new Vector2(272f, 24f),
            Color    = new Color(0.094f, 0.082f, 0.141f, 0.82f)  // deep Stardew dark
        };
        _hud.AddChild(_clockBg);

        // Thin accent line at top of panel (Stardew UI style).
        _hud.AddChild(new ColorRect
        {
            Position = new Vector2(8f, topY),
            Size     = new Vector2(272f, 2f),
            Color    = new Color(0.510f, 0.388f, 0.651f, 0.90f)  // purple accent
        });

        _clock = new Label { Position = new Vector2(14f, topY + 4f) };
        _clock.AddThemeFontSizeOverride("font_size", 12);
        _clock.AddThemeColorOverride("font_color", new Color(0.973f, 0.945f, 0.820f));  // Stardew cream
        _hud.AddChild(_clock);
    }

    private void UpdateClock(TimeManager tm)
    {
        if (_clock == null) return;
        string icon = TimeManager.SeasonEmoji(tm.CurrentSeason);
        _clock.Text = $"⏰ {tm.TimeString()}  {icon} {tm.SeasonName()}  {tm.TimeOfDayName()}  Gün {tm.TotalDays}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Color SampleCurve((float t, Color c)[] curve, float t)
    {
        for (int i = 0; i < curve.Length - 1; i++)
            if (t >= curve[i].t && t < curve[i + 1].t)
                return curve[i].c.Lerp(curve[i + 1].c, (t - curve[i].t) / (curve[i + 1].t - curve[i].t));
        return curve[^1].c;
    }

    private static Polygon2D PixelPolygon(float radius, int sides, Color color, string name)
    {
        var poly = new Polygon2D { Name = name, Color = color };
        var pts  = new Vector2[sides];
        for (int i = 0; i < sides; i++)
        {
            float a = Mathf.Tau * i / sides;
            pts[i]  = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }
        poly.Polygon = pts;
        return poly;
    }
}
