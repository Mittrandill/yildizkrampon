using Godot;

/// res://scripts/GameTime.cs
/// Autoload — oyun içi saat ve gün döngüsü.
/// 1 gerçek saniye = 1 oyun dakikası. Aktiviteler zaman ilerletir; diyalog/menü duraklatır.
public partial class GameTime : Node
{
    public static GameTime Instance { get; private set; } = null!;

    [Signal] public delegate void HourChangedEventHandler(int hour, int minute);
    [Signal] public delegate void DayChangedEventHandler(int day);
    [Signal] public delegate void PeriodChangedEventHandler(string period);

    public int   Day    { get; private set; } = 1;
    public float Hour   { get; private set; } = 7.5f;   // 07:30
    public bool  Paused { get; set; }         = true;   // Başlangıçta duraklı — sahne açılınca Resume()

    private int    _prevHourInt  = 7;
    private string _prevPeriod   = "";

    // 1 gerçek saniye = 1 oyun dakikası
    private const float GameMinutesPerRealSecond = 1f;

    public string TimeString => $"{(int)Hour:D2}:{(int)((Hour % 1) * 60):D2}";
    public string DayString  => $"Gün {Day}";

    public string PeriodName
    {
        get
        {
            if (Hour < 10f) return "Sabah";
            if (Hour < 14f) return "Öğle";
            if (Hour < 18f) return "Öğleden Sonra";
            return "Akşam";
        }
    }

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _Process(double delta)
    {
        if (Paused) return;

        float prev = Hour;
        Hour += (float)delta * GameMinutesPerRealSecond / 60f;

        int prevH = (int)prev;
        int curH  = (int)Hour;
        if (curH != prevH)
            EmitSignal(SignalName.HourChanged, curH, 0);

        string period = PeriodName;
        if (period != _prevPeriod)
        {
            _prevPeriod = period;
            EmitSignal(SignalName.PeriodChanged, period);
        }

        if (Hour >= 22f)
            SleepToMorning();
    }

    /// Aktivite tamamlandığında çağrılır (ör: Yemek yeme = +30 dakika)
    public void AdvanceTime(float gameMinutes)
    {
        Hour += gameMinutes / 60f;
    }

    public void SleepToMorning()
    {
        Day++;
        Hour    = 7.5f;
        Paused  = true;
        EmitSignal(SignalName.DayChanged, Day);
    }

    public void Pause()  { Paused = true; }
    public void Resume() { Paused = false; }

    /// NPC o konumda şu an var mı?
    public bool IsNPCAt(string npc, string location)
    {
        int h = (int)Hour;
        return (npc, location) switch
        {
            ("Anne",     "Mutfak"       ) => h is >= 6  and < 10,
            ("Anne",     "HomeInterior" ) => true,
            ("Baba",     "HomeInterior" ) => h is < 8   or  >= 18,
            ("Eren",     "MahalleSahasi") => h is >= 15 and < 18,
            ("Baran",    "CayBahcesi"   ) => h is >= 15 and < 18,
            ("KemalHoca","MahalleSahasi") => h is >= 14 and < 18,
            ("KemalHoca","SporTesisi"   ) => h is >= 8  and < 14,
            ("RizaAbi",  "BakkalInterior") => h is >= 8 and < 20,
            _ => false
        };
    }
}
