using Godot;

/// Pixel-art day/night visual overlay — drop into any Node2D scene.
///
/// Creates:
///   • CanvasModulate  — smooth ambient tint on the whole scene (not the HUD).
///   • Sky strip       — pixel-art sky gradient + sun + moon + stars at
///                       the top of the screen (screen-fixed via CanvasLayer).
///   • Weather overlay — season-specific world tint (snow for Winter, etc.).
///   • Clock label     — compact HUD clock showing time, season, day.
///
/// Usage:
///   var layer = new DayNightLayer();
///   AddChild(layer);
///
/// To hide the sky strip (match scene, interior scenes):
///   layer.ShowSky = false;   // set BEFORE adding to tree, or use [Export]
public partial class DayNightLayer : Node2D
{
    // ── Settings ──────────────────────────────────────────────────────────────
    [Export] public bool ShowSky { get; set; } = true;

    /// Screen-space rect for the sky strip (CanvasLayer coords, 1280×720 ref).
    [Export] public Rect2 SkyRect { get; set; } = new Rect2(0, 0, 1280, 72);

    // ── Nodes ─────────────────────────────────────────────────────────────────
    private CanvasModulate? _mod;
    private CanvasLayer?    _hud;   // layer 8 — below match HUD (10) and game UI (9)
    private ColorRect?      _skyBg;
    private Polygon2D?      _sun;
    private Polygon2D?      _moon;
    private Node2D?         _stars;
    private Label?          _clock;
    private ColorRect?      _snowOverlay;  // world-space seasonal overlay

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    public override void _Ready()
    {
        // CanvasModulate: tints everything on the MAIN canvas (not CanvasLayer).
        _mod = new CanvasModulate { Name = "DayNightMod", Color = Colors.White };
        AddChild(_mod);

        // HUD canvas layer — not affected by the modulate, stays readable.
        _hud = new CanvasLayer { Name = "DayNightHUD", Layer = 8 };
        AddChild(_hud);

        if (ShowSky) BuildSky();
        BuildClock();

        if (ShowSky)
        {
            // World-space overlay for snow / summer haze — sits just above backdrop.
            _snowOverlay = new ColorRect
            {
                Name        = "SeasonOverlay",
                Position    = new Vector2(-500f, -500f),
                Size        = new Vector2(4000f, 4000f),
                Color       = Colors.Transparent,
                ZIndex      = -16,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            AddChild(_snowOverlay);
        }

        Refresh(0f);
    }

    public override void _Process(double delta)
    {
        Refresh((float)delta);
    }

    // ── Master update ─────────────────────────────────────────────────────────
    private void Refresh(float _dt)
    {
        if (TimeManager.Instance == null) return;
        var tm = TimeManager.Instance;
        float t = tm.NormalizedTime;

        if (_mod != null)
            _mod.Color = tm.CombinedColor();

        if (ShowSky)
        {
            UpdateSky(t);
            UpdateSeasonOverlay(tm.CurrentSeason);
        }

        UpdateClock(tm);
    }

    // ── Sky ──────────────────────────────────────────────────────────────────
    private void BuildSky()
    {
        if (_hud == null) return;

        // Sky gradient background strip (fixed to top of screen).
        _skyBg = new ColorRect
        {
            Name     = "SkyBg",
            Position = SkyRect.Position,
            Size     = SkyRect.Size,
            Color    = new Color(0.38f, 0.63f, 0.96f)
        };
        _hud.AddChild(_skyBg);

        // Sun — 12-segment polygon = pixel-art circle.
        _sun  = PixelCircle(18f, new Color(1.00f, 0.94f, 0.36f), "Sun");
        _moon = PixelCircle(13f, new Color(0.88f, 0.90f, 0.98f), "Moon");
        _hud.AddChild(_sun);
        _hud.AddChild(_moon);

        // Stars — built once, alpha animated.
        _stars = new Node2D { Name = "Stars" };
        _hud.AddChild(_stars);
        BuildStars();
    }

    private void UpdateSky(float t)
    {
        if (_skyBg == null || _sun == null || _moon == null || _stars == null) return;

        _skyBg.Color = SkyGradientColor(t);

        // Sun arc: horizon is below the sky strip.
        float cx  = SkyRect.Position.X + SkyRect.Size.X * 0.5f;
        float cy  = SkyRect.End.Y + 24f;    // horizon baseline
        float arc = SkyRect.Size.X * 0.44f;

        // Sun rises at t=0.208 (05:00), sets at t=0.792 (19:00).
        float sunT = Mathf.InverseLerp(0.208f, 0.792f, t);
        float sunA = Mathf.Pi * (1f - sunT);
        _sun.Position = new Vector2(cx + Mathf.Cos(sunA) * arc, cy - Mathf.Sin(sunA) * arc);
        _sun.Visible  = t > 0.190f && t < 0.810f;
        _sun.Modulate = new Color(1f, 1f, 1f,
            t < 0.240f ? (t - 0.190f) / 0.050f :
            t > 0.770f ? (0.810f - t) / 0.040f : 1f);

        // Moon: opposite arc (visible when sun is below horizon).
        float moonNorm = t < 0.208f ? t + 1f : t;
        float moonT    = Mathf.InverseLerp(0.792f, 1.208f, moonNorm);
        float moonA    = Mathf.Pi * (1f - moonT);
        _moon.Position = new Vector2(cx + Mathf.Cos(moonA) * arc, cy - Mathf.Sin(moonA) * arc);
        _moon.Visible  = t < 0.210f || t > 0.780f;

        // Star fade: fade in after 19:00, fade out before 05:30.
        float starA = t < 0.200f ? 1f
                    : t < 0.260f ? (0.260f - t) / 0.060f
                    : t > 0.770f ? (t - 0.770f) / 0.060f : 0f;
        _stars.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(starA, 0f, 1f));
    }

    private void BuildStars()
    {
        if (_stars == null) return;
        var rng = new RandomNumberGenerator();
        rng.Seed = 1337;
        for (int i = 0; i < 88; i++)
        {
            float x = rng.RandfRange(SkyRect.Position.X + 4f, SkyRect.End.X - 4f);
            float y = rng.RandfRange(SkyRect.Position.Y + 2f, SkyRect.End.Y * 0.88f);
            int   s = rng.RandiRange(1, 3);
            float a = rng.RandfRange(0.45f, 1.0f);
            _stars.AddChild(new ColorRect
            {
                Position = new Vector2(x, y),
                Size     = new Vector2(s, s),
                Color    = new Color(1f, 1f, 1f, a)
            });
        }
    }

    private static Color SkyGradientColor(float t)
    {
        var kf = new (float t, Color c)[]
        {
            (0.000f, new Color(0.05f, 0.05f, 0.18f)),
            (0.190f, new Color(0.09f, 0.09f, 0.26f)),
            (0.210f, new Color(0.66f, 0.34f, 0.18f)),  // dawn glow
            (0.250f, new Color(0.84f, 0.58f, 0.36f)),  // sunrise
            (0.330f, new Color(0.50f, 0.72f, 0.94f)),  // morning sky
            (0.500f, new Color(0.36f, 0.62f, 0.96f)),  // noon sky
            (0.670f, new Color(0.42f, 0.66f, 0.92f)),  // afternoon
            (0.750f, new Color(0.88f, 0.46f, 0.20f)),  // sunset
            (0.800f, new Color(0.56f, 0.22f, 0.36f)),  // dusk
            (0.840f, new Color(0.12f, 0.12f, 0.32f)),  // twilight
            (1.000f, new Color(0.05f, 0.05f, 0.18f)),
        };
        for (int i = 0; i < kf.Length - 1; i++)
            if (t >= kf[i].t && t < kf[i + 1].t)
                return kf[i].c.Lerp(kf[i + 1].c, (t - kf[i].t) / (kf[i + 1].t - kf[i].t));
        return kf[^1].c;
    }

    // ── Seasonal world overlay ────────────────────────────────────────────────
    private void UpdateSeasonOverlay(TimeManager.Season season)
    {
        if (_snowOverlay == null) return;
        _snowOverlay.Color = season switch
        {
            TimeManager.Season.Winter => new Color(0.90f, 0.94f, 1.00f, 0.14f), // light snow tint
            TimeManager.Season.Summer => new Color(1.00f, 0.96f, 0.78f, 0.04f), // faint heat shimmer
            _                         => Colors.Transparent
        };
    }

    // ── Clock HUD ─────────────────────────────────────────────────────────────
    private void BuildClock()
    {
        if (_hud == null) return;
        float topY = ShowSky ? SkyRect.End.Y + 4f : 8f;

        var bg = new ColorRect
        {
            Position  = new Vector2(8f, topY),
            Size      = new Vector2(258f, 26f),
            Color     = new Color(0.04f, 0.05f, 0.04f, 0.76f)
        };
        _hud.AddChild(bg);

        _clock = new Label { Position = bg.Position + new Vector2(6f, 4f) };
        _clock.AddThemeFontSizeOverride("font_size", 13);
        _clock.AddThemeColorOverride("font_color", Colors.White);
        _hud.AddChild(_clock);
    }

    private void UpdateClock(TimeManager tm)
    {
        if (_clock == null) return;
        string emoji = TimeManager.SeasonEmoji(tm.CurrentSeason);
        _clock.Text = $"⏰ {tm.TimeString()}  {emoji} {tm.SeasonName()}  🗓 Gün {tm.TotalDays}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Polygon2D PixelCircle(float radius, Color color, string name)
    {
        const int seg = 12;   // 12 segments = chunky pixel-art circle
        var poly = new Polygon2D { Name = name, Color = color };
        var pts  = new Vector2[seg];
        for (int i = 0; i < seg; i++)
        {
            float a = Mathf.Tau * i / seg;
            pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }
        poly.Polygon = pts;
        return poly;
    }
}
