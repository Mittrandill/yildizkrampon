using Godot;

/// In-game time-of-day and seasonal cycle.
/// Registered as autoload — persists across all scenes.
///
/// Time runs automatically at SecondsPerGameHour real-seconds per game-hour.
/// Default: 30 s/h → full day in 12 real minutes; each season lasts 7 days.
public partial class TimeManager : Node
{
    // ── Enums ─────────────────────────────────────────────────────────────────
    public enum TimeOfDay { Morning, Noon, Evening, Night }
    public enum Season    { Spring, Summer, Autumn, Winter }

    // ── Singleton ─────────────────────────────────────────────────────────────
    public static TimeManager Instance { get; private set; } = null!;

    // ── Settings ──────────────────────────────────────────────────────────────
    /// Real seconds that equal one in-game hour.  Lower = faster clock.
    [Export] public float SecondsPerGameHour { get; set; } = 30f;

    /// How many in-game days before the season changes.
    [Export] public int DaysPerSeason { get; set; } = 7;

    // ── State ─────────────────────────────────────────────────────────────────
    /// 0.0 = 00:00 midnight, 1.0 = next midnight.
    public float NormalizedTime  { get; set; } = 0.375f;   // starts at 09:00
    public int   TotalDays       { get; private set; } = 1;
    public int   DayInSeason     { get; private set; } = 1;
    public Season  CurrentSeason   { get; private set; } = Season.Spring;
    public TimeOfDay CurrentTimeOfDay => TimeOfDayFor(NormalizedTime);

    // ── Gameplay multipliers (read by MatchActor and MatchBall) ───────────────

    /// Multiplier applied to every UseStamina() call (> 1 = drains faster).
    public float StaminaDrainMultiplier
    {
        get
        {
            float m = 1f;
            if (CurrentTimeOfDay == TimeOfDay.Noon)    m *= 1.25f;  // midday heat
            if (CurrentSeason    == Season.Summer)     m *= 1.20f;  // summer heat
            return m;
        }
    }

    /// Multiplier for ball Friction constant (> 1 = more resistance, < 1 = slippery).
    public float BallFrictionMultiplier
    {
        get
        {
            float m = 1f;
            if (CurrentTimeOfDay == TimeOfDay.Morning) m *= 1.15f;  // wet dew
            if (CurrentSeason    == Season.Winter)     m *= 0.50f;  // icy pitch
            return m;
        }
    }

    /// True when conditions produce a lateral wind on the ball.
    public bool HasWind => CurrentSeason == Season.Autumn
                        || CurrentTimeOfDay == TimeOfDay.Evening;

    // ── Signals ───────────────────────────────────────────────────────────────
    [Signal] public delegate void TimeOfDayChangedEventHandler(int period);
    [Signal] public delegate void SeasonChangedEventHandler(int season);
    [Signal] public delegate void DayPassedEventHandler(int totalDay);

    private TimeOfDay _prevTod;
    private Season    _prevSeason;
    private bool      _paused;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        _prevTod    = CurrentTimeOfDay;
        _prevSeason = CurrentSeason;
    }

    public override void _Process(double delta)
    {
        if (_paused) return;

        NormalizedTime += (float)delta / (SecondsPerGameHour * 24f);
        if (NormalizedTime >= 1f)
        {
            NormalizedTime -= 1f;
            TotalDays++;
            DayInSeason++;
            EmitSignal(SignalName.DayPassed, TotalDays);

            if (DayInSeason > DaysPerSeason)
            {
                DayInSeason   = 1;
                CurrentSeason = (Season)(((int)CurrentSeason + 1) % 4);
            }
        }

        var tod = CurrentTimeOfDay;
        if (tod != _prevTod)
        {
            EmitSignal(SignalName.TimeOfDayChanged, (int)tod);
            _prevTod = tod;
        }
        if (CurrentSeason != _prevSeason)
        {
            EmitSignal(SignalName.SeasonChanged, (int)CurrentSeason);
            _prevSeason = CurrentSeason;
        }
    }

    public void Pause()  => _paused = true;
    public void Resume() => _paused = false;

    /// Jump the clock forward by this many in-game hours.
    public void AdvanceHours(float hours)
        => NormalizedTime = (NormalizedTime + hours / 24f) % 1f;

    // ── Time-of-day classification ────────────────────────────────────────────
    public static TimeOfDay TimeOfDayFor(float t)
    {
        // Morning  05:00-10:00  → 0.208-0.417
        // Noon     10:00-16:00  → 0.417-0.667
        // Evening  16:00-20:00  → 0.667-0.833
        // Night    otherwise
        if (t >= 0.208f && t < 0.417f) return TimeOfDay.Morning;
        if (t >= 0.417f && t < 0.667f) return TimeOfDay.Noon;
        if (t >= 0.667f && t < 0.833f) return TimeOfDay.Evening;
        return TimeOfDay.Night;
    }

    // ── Color helpers (used by DayNightLayer and NeighborhoodMatchController) ─

    /// Smooth ambient tint for CanvasModulate, interpolated from time keyframes.
    public static Color AmbientColor(float t)
    {
        var kf = new (float t, Color c)[]
        {
            (0.000f, new Color(0.15f, 0.17f, 0.38f)), // 00:00 midnight
            (0.167f, new Color(0.17f, 0.19f, 0.44f)), // 04:00 deep night
            (0.208f, new Color(0.74f, 0.44f, 0.26f)), // 05:00 dawn glow
            (0.250f, new Color(1.00f, 0.86f, 0.68f)), // 06:00 sunrise
            (0.333f, new Color(1.00f, 0.96f, 0.88f)), // 08:00 morning
            (0.417f, new Color(1.00f, 0.99f, 0.95f)), // 10:00 late morning
            (0.500f, new Color(1.00f, 1.00f, 1.00f)), // 12:00 noon — full white
            (0.625f, new Color(1.00f, 0.97f, 0.90f)), // 15:00 afternoon
            (0.667f, new Color(1.00f, 0.80f, 0.54f)), // 16:00 golden hour
            (0.750f, new Color(0.96f, 0.52f, 0.26f)), // 18:00 sunset
            (0.792f, new Color(0.54f, 0.30f, 0.50f)), // 19:00 dusk
            (0.833f, new Color(0.22f, 0.24f, 0.50f)), // 20:00 blue hour
            (1.000f, new Color(0.15f, 0.17f, 0.38f)), // 24:00 midnight
        };
        for (int i = 0; i < kf.Length - 1; i++)
        {
            if (t >= kf[i].t && t < kf[i + 1].t)
            {
                float lt = (t - kf[i].t) / (kf[i + 1].t - kf[i].t);
                return kf[i].c.Lerp(kf[i + 1].c, lt);
            }
        }
        return kf[^1].c;
    }

    /// Seasonal colour modifier — multiplied on top of the ambient colour.
    public static Color SeasonTint(Season s) => s switch
    {
        Season.Spring => new Color(1.00f, 1.00f, 1.00f), // neutral / fresh
        Season.Summer => new Color(1.00f, 0.98f, 0.84f), // warm
        Season.Autumn => new Color(1.00f, 0.88f, 0.68f), // orange warmth
        Season.Winter => new Color(0.86f, 0.91f, 1.00f), // cool blue
        _             => new Color(1f, 1f, 1f)
    };

    /// Final CanvasModulate color = ambient × season tint.
    public Color CombinedColor()
    {
        Color a = AmbientColor(NormalizedTime);
        Color s = SeasonTint(CurrentSeason);
        return new Color(a.R * s.R, a.G * s.G, a.B * s.B, 1f);
    }

    // ── Display helpers ───────────────────────────────────────────────────────
    public string TimeString()
    {
        float mins = NormalizedTime * 1440f;
        return $"{(int)(mins / 60f) % 24:D2}:{(int)(mins % 60f):D2}";
    }

    public string SeasonName() => CurrentSeason switch
    {
        Season.Spring => "İlkbahar",
        Season.Summer => "Yaz",
        Season.Autumn => "Sonbahar",
        Season.Winter => "Kış",
        _             => "?"
    };

    public string TimeOfDayName() => CurrentTimeOfDay switch
    {
        TimeOfDay.Morning => "Sabah",
        TimeOfDay.Noon    => "Öğle",
        TimeOfDay.Evening => "Akşam Üzeri",
        TimeOfDay.Night   => "Gece",
        _                 => "?"
    };

    public static string SeasonEmoji(Season s) => s switch
    {
        Season.Spring => "🌸",
        Season.Summer => "☀",
        Season.Autumn => "🍂",
        Season.Winter => "❄",
        _             => "?"
    };
}
