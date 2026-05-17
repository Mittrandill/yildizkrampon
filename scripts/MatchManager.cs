using Godot;
using System.Collections.Generic;

/// res://scripts/MatchManager.cs
/// Maç durumu makinesi — skor, süre, out, yarı, bitiş, otomatik oyuncu değişimi.
public partial class MatchManager : Node2D
{
    public static MatchManager? Instance { get; private set; }

    // Saha sabitleri
    public const float PITCH_W      = FieldPlayer.PITCH_W;
    public const float PITCH_H      = FieldPlayer.PITCH_H;
    public const float GOAL_TOP     = FieldPlayer.GOAL_TOP;
    public const float GOAL_BOT     = FieldPlayer.GOAL_BOT;
    public static readonly Vector2 CENTER = new(PITCH_W / 2f, PITCH_H / 2f);

    // Maç durumu
    private enum MatchState { KickOff, Playing, Goal, ThrowIn, HalfTime, FullTime }
    private MatchState _state = MatchState.KickOff;
    private float      _stateTimer = 0f;

    // Skor & süre
    public int   RedScore    { get; private set; } = 0;
    public int   BlueScore   { get; private set; } = 0;
    private float _elapsed   = 0f;
    private float _halfDuration = 180f; // 3 dakika / yarı
    private bool  _secondHalf   = false;
    private bool  _sidesFlipped = false;

    // Oyuncular
    private readonly List<FieldPlayer>    _redPlayers  = new();
    private readonly List<FieldPlayer>    _bluePlayers = new();
    private FieldPlayer? _humanPlayer;

    // Asist takibi
    private FieldPlayer? _lastAssistCandidate;
    private float        _assistTimer = 0f;

    // UI referansları
    private Label?      _scoreLabel;
    private Label?      _timerLabel;
    private Label?      _stateLabel;
    private ProgressBar? _staminaBar;
    private ProgressBar? _powerBar;
    private Camera2D?   _camera;

    // Mini-harita renk göstergesi
    private MiniMap?    _miniMap;

    // İnsan oyuncusunun son pozisyonu (switch threshold)
    private float _switchTimer = 0f;
    private const float SWITCH_INTERVAL  = 0.5f;
    private const float SWITCH_THRESHOLD = 100f;

    public override void _Ready()
    {
        Instance = this;

        // Oyuncuları topla
        foreach (Node n in GetTree().GetNodesInGroup("team_red"))
            if (n is FieldPlayer fp) _redPlayers.Add(fp);
        foreach (Node n in GetTree().GetNodesInGroup("team_blue"))
            if (n is FieldPlayer fp) _bluePlayers.Add(fp);

        // İlk insan oyuncusu (FWD1 — slot 4)
        _humanPlayer = _redPlayers.Count > 4 ? _redPlayers[4] : _redPlayers[0];

        // UI bul
        _scoreLabel  = GetNodeOrNull<Label>("%ScoreLabel");
        _timerLabel  = GetNodeOrNull<Label>("%TimerLabel");
        _stateLabel  = GetNodeOrNull<Label>("%StateLabel");
        _staminaBar  = GetNodeOrNull<ProgressBar>("%StaminaBar");
        _powerBar    = GetNodeOrNull<ProgressBar>("%PowerBar");
        _camera      = GetNodeOrNull<Camera2D>("%MatchCamera");
        _miniMap     = GetNodeOrNull<MiniMap>("%MiniMap");

        _UpdateScoreUI();
        _SetState(MatchState.KickOff);

        // Goal Area2D sinyalleri
        var goalLeft  = GetNodeOrNull<Area2D>("%GoalLeft");
        var goalRight = GetNodeOrNull<Area2D>("%GoalRight");
        if (goalLeft  != null) goalLeft.BodyEntered  += _ => _OnGoal(Team.Blue);  // sol gole giren → mavi gol atar
        if (goalRight != null) goalRight.BodyEntered += _ => _OnGoal(Team.Red);
    }

    public override void _Process(double delta)
    {
        if (_stateTimer > 0f) { _stateTimer -= (float)delta; return; }

        switch (_state)
        {
            case MatchState.KickOff:
                if (Input.IsActionJustPressed("action") || Input.IsActionJustPressed("interact"))
                    _SetState(MatchState.Playing);
                break;

            case MatchState.Playing:
                _UpdatePlaying((float)delta);
                break;

            case MatchState.Goal:
                _SetupKickOff(scoringTeam: _lastGoalTeam);
                _SetState(MatchState.Playing);
                break;

            case MatchState.ThrowIn:
                _DoThrowIn();
                _SetState(MatchState.Playing);
                break;

            case MatchState.HalfTime:
                if (!_secondHalf)
                {
                    _secondHalf   = true;
                    _elapsed      = 0f;
                    _FlipSides();
                    _SetupKickOff(scoringTeam: null);
                    _SetState(MatchState.Playing);
                }
                break;

            case MatchState.FullTime:
                if (Input.IsActionJustPressed("action") || Input.IsActionJustPressed("interact"))
                    _EndMatch();
                break;
        }
    }

    private void _UpdatePlaying(float delta)
    {
        _elapsed += delta;

        // Süreyi göster
        float totalTime = _secondHalf ? _halfDuration + _elapsed : _elapsed;
        float remaining = _halfDuration * 2f - totalTime;
        if (_timerLabel != null)
            _timerLabel.Text = $"{(int)(totalTime / 60):D2}:{(int)(totalTime % 60):D2}";

        // Yarı bitti mi?
        if (!_secondHalf && _elapsed >= _halfDuration)
        {
            _SetState(MatchState.HalfTime);
            if (_stateLabel != null) _stateLabel.Text = "DEVRE ARI";
            _stateTimer = 3f;
            return;
        }
        if (_secondHalf && _elapsed >= _halfDuration)
        {
            _SetState(MatchState.FullTime);
            if (_stateLabel != null) _stateLabel.Text = "MAÇ BİTTİ";
            _ShowResult();
            return;
        }

        // Out kontrolü
        var ball = Football.Instance;
        if (ball != null && !ball.IsControlled)
            _CheckOutOfBounds(ball);

        // Asist sayacı
        if (_assistTimer > 0f) _assistTimer -= delta;

        // Kamera
        _UpdateCamera(ball);

        // Otomatik oyuncu değişimi
        _switchTimer += delta;
        if (_switchTimer >= SWITCH_INTERVAL)
        {
            _switchTimer = 0f;
            _AutoSwitch();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  GOL, OUT, KickOff
    // ─────────────────────────────────────────────────────────────

    private enum Team { Red, Blue }
    private Team _lastGoalTeam;

    private void _OnGoal(Team scoringTeam)
    {
        if (_state != MatchState.Playing) return;

        if (scoringTeam == Team.Red) RedScore++;
        else                         BlueScore++;

        _lastGoalTeam = scoringTeam;
        _UpdateScoreUI();

        // Asist
        var ball = Football.Instance;
        if (_assistTimer > 0f && _lastAssistCandidate != null
            && ball?.LastToucher?.PlayerTeam == FieldPlayer.Team.Red
            && scoringTeam == Team.Red)
        {
            // Asist var (gelecekte rating'e ekle)
        }

        if (_stateLabel != null) _stateLabel.Text = scoringTeam == Team.Red ? "GOL! KIRMIZI!" : "GOL! MAVİ!";
        _stateTimer = 2.5f;
        _SetState(MatchState.Goal);
    }

    private void _SetupKickOff(Team? scoringTeam)
    {
        Football.Instance?.ResetTo(CENTER);
        // Oyuncuları başlangıç pozisyonlarına yerleştir
        foreach (Node n in GetTree().GetNodesInGroup("field_players"))
        {
            if (n is FieldPlayer fp) fp.GlobalPosition = fp.GetMeta("start_pos").AsVector2();
        }
        foreach (Node n in GetTree().GetNodesInGroup("goalkeepers"))
        {
            if (n is GoalkeeperAI gk) gk.GlobalPosition = gk.GetMeta("start_pos").AsVector2();
        }
    }

    private void _CheckOutOfBounds(Football ball)
    {
        Vector2 pos = ball.GlobalPosition;
        if (pos.Y < -20f || pos.Y > PITCH_H + 20f)
        {
            // Taç
            _lastThrowInPos = new Vector2(pos.X, pos.Y < 0 ? 0f : PITCH_H);
            _throwInTeam    = ball.LastToucher?.PlayerTeam == FieldPlayer.Team.Red
                              ? FieldPlayer.Team.Blue : FieldPlayer.Team.Red;
            _SetState(MatchState.ThrowIn);
            _stateTimer = 0.8f;
            if (_stateLabel != null) _stateLabel.Text = "TAÇ";
        }
        else if ((pos.X < -40f || pos.X > PITCH_W + 40f) &&
                 (pos.Y < GOAL_TOP || pos.Y > GOAL_BOT))
        {
            // Kale vuruşu veya köşe vuruşu
            bool leftSide = pos.X < 0;
            _HandleByline(ball, leftSide);
        }
    }

    private Vector2 _lastThrowInPos;
    private FieldPlayer.Team _throwInTeam;

    private void _DoThrowIn()
    {
        // En yakın taç takımı oyuncusunu bul ve topa ver
        var ball = Football.Instance;
        if (ball == null) return;
        ball.ResetTo(new Vector2(Mathf.Clamp(_lastThrowInPos.X, 60f, PITCH_W - 60f), _lastThrowInPos.Y));

        string group = _throwInTeam == FieldPlayer.Team.Red ? "team_red" : "team_blue";
        FieldPlayer? nearest = null;
        float nd = float.MaxValue;
        foreach (Node n in GetTree().GetNodesInGroup(group))
        {
            if (n is not FieldPlayer fp) continue;
            float d = fp.GlobalPosition.DistanceTo(ball.GlobalPosition);
            if (d < nd) { nd = d; nearest = fp; }
        }
        if (nearest != null) ball.GiveControl(nearest);
    }

    private void _HandleByline(Football ball, bool leftSide)
    {
        // Basit: kale vuruşu veya köşe → kaleci veya ilgili takıma ver
        float resetX = leftSide ? 60f : PITCH_W - 60f;
        ball.ResetTo(new Vector2(resetX, PITCH_H / 2f));
        if (_stateLabel != null) _stateLabel.Text = leftSide ? "KALE VURUŞU" : "KALE VURUŞU";
        _stateTimer = 0.5f;
    }

    // ─────────────────────────────────────────────────────────────
    //  OTOMATİK OYUNCU DEĞİŞİMİ
    // ─────────────────────────────────────────────────────────────

    public bool IsHumanControlled(FieldPlayer p) => p == _humanPlayer;

    private void _AutoSwitch()
    {
        if (_humanPlayer?.HasBall == true) return; // top varken değiştirme

        var ball = Football.Instance;
        if (ball == null) return;

        FieldPlayer? best    = null;
        float        bestD   = float.MaxValue;
        float        curD    = _humanPlayer?.GlobalPosition.DistanceTo(ball.GlobalPosition) ?? float.MaxValue;

        foreach (var fp in _redPlayers)
        {
            float d = fp.GlobalPosition.DistanceTo(ball.GlobalPosition);
            if (d < bestD) { bestD = d; best = fp; }
        }

        if (best != null && best != _humanPlayer && bestD < curD - SWITCH_THRESHOLD)
            _humanPlayer = best;
    }

    // ─────────────────────────────────────────────────────────────
    //  UI & KAMERA
    // ─────────────────────────────────────────────────────────────

    public void SetStaminaBar(float value)
    {
        if (_staminaBar != null) _staminaBar.Value = value;
    }

    public void SetPowerBar(float value)
    {
        if (_powerBar != null) _powerBar.Value = value;
    }

    public void OnAssistOpportunity(FieldPlayer target)
    {
        _lastAssistCandidate = target;
        _assistTimer = 3.5f;
    }

    private void _UpdateCamera(Football? ball)
    {
        if (_camera == null || ball == null) return;
        // Kamera topu düzgünce takip etsin
        _camera.Position = _camera.Position.Lerp(ball.GlobalPosition, 0.08f);
    }

    private void _UpdateScoreUI()
    {
        if (_scoreLabel != null)
            _scoreLabel.Text = $"{RedScore}  —  {BlueScore}";
    }

    private void _SetState(MatchState s)
    {
        _state = s;
        if (s == MatchState.KickOff && _stateLabel != null)
            _stateLabel.Text = "Başlamak için E'ye bas";
        else if (s == MatchState.Playing && _stateLabel != null)
            _stateLabel.Text = "";
    }

    private void _FlipSides()
    {
        // Takımlar yer değiştirsin (devre arası)
        foreach (Node n in GetTree().GetNodesInGroup("field_players"))
        {
            if (n is FieldPlayer fp)
                fp.GlobalPosition = new Vector2(PITCH_W - fp.GlobalPosition.X, fp.GlobalPosition.Y);
        }
        foreach (Node n in GetTree().GetNodesInGroup("goalkeepers"))
        {
            if (n is GoalkeeperAI gk)
                gk.GlobalPosition = new Vector2(PITCH_W - gk.GlobalPosition.X, gk.GlobalPosition.Y);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  MAÇ SONU
    // ─────────────────────────────────────────────────────────────

    private void _ShowResult()
    {
        _ApplyMatchStats();
        string result = RedScore > BlueScore ? "KAZANDIN!" : RedScore < BlueScore ? "KAYBETTİN" : "BERABERLİK";
        DialogueManager.Instance.Show(
            $"Maç Bitti — {result}",
            $"Skor: {RedScore} — {BlueScore}\n" +
            $"Golcü: {_humanPlayer?.GoalsScored ?? 0} gol\n" +
            $"Devam etmek için Enter'a bas.",
            () => WorldManager.Instance.GoTo("WorldMap")
        );
    }

    private void _ApplyMatchStats()
    {
        var gm = GameManager.Instance;

        if (RedScore > BlueScore)
        {
            gm.Morale   = Mathf.Min(100, gm.Morale  + 15);
            gm.AddEvent("Maçı kazandın! Moral +15");
        }
        else if (RedScore < BlueScore)
        {
            gm.Morale   = Mathf.Max(0, gm.Morale  - 5);
        }

        int goals = _humanPlayer?.GoalsScored ?? 0;
        if (goals > 0)
        {
            gm.ShotPower = Mathf.Min(99, gm.ShotPower + goals / 2);
            gm.AddEvent($"{goals} gol attın! Şut Gücü +" + goals / 2);
        }

        gm.Fatigue = Mathf.Min(100, gm.Fatigue + 25);
        gm.Energy  = Mathf.Max(0,   gm.Energy  - 20);
        GameTime.Instance?.AdvanceTime(180f); // Maç = 3 saat oyun süresi
    }

    private void _EndMatch()
    {
        WorldManager.Instance.GoTo("WorldMap");
    }
}
