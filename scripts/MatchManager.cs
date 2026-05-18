using Godot;
using System.Collections.Generic;

/// res://scripts/MatchManager.cs
/// Maç durum makinesi — skor, süre, kale, taç, devre, bitiş.
/// İnsan oyuncusu SABITTIR — asla değişmez.
public partial class MatchManager : Node2D
{
    public static MatchManager? Instance { get; private set; }

    public const float PITCH_W  = FieldPlayer.PITCH_W;
    public const float PITCH_H  = FieldPlayer.PITCH_H;
    public const float GOAL_TOP = FieldPlayer.GOAL_TOP;
    public const float GOAL_BOT = FieldPlayer.GOAL_BOT;
    public static readonly Vector2 CENTER = new(PITCH_W / 2f, PITCH_H / 2f);

    // ─── Maç durumu ─────────────────────────────────────────────
    private enum MatchState { KickOff, Playing, Goal, ThrowIn, HalfTime, FullTime }
    private MatchState _state      = MatchState.KickOff;
    private float      _stateTimer = 0f;
    private enum Team { Red, Blue }
    private Team  _lastGoalTeam;

    // ─── Skor & süre ────────────────────────────────────────────
    public int   RedScore  { get; private set; } = 0;
    public int   BlueScore { get; private set; } = 0;
    private float _elapsed      = 0f;
    private float _halfDuration = 180f; // 3 dak/yarı
    private bool  _secondHalf   = false;

    // ─── Oyuncular ──────────────────────────────────────────────
    private readonly List<FieldPlayer> _redPlayers = new();
    private FieldPlayer? _humanPlayer;          // Sabit — değişmez

    // ─── Asist ─────────────────────────────────────────────────
    private FieldPlayer? _lastAssist;
    private float        _assistTimer = 0f;

    // ─── UI ─────────────────────────────────────────────────────
    private Label?       _scoreLabel;
    private Label?       _timerLabel;
    private Label?       _stateLabel;
    private ProgressBar? _staminaBar;
    private ProgressBar? _powerBar;
    private ProgressBar? _energyBar;
    private Label?       _ratingLabel;
    private Label?       _hintLabel;
    private Camera2D?    _camera;
    private MiniMap?     _miniMap;

    // ─── Oyuncu puanı ───────────────────────────────────────────
    private float _playerRating = 6.0f;
    private float _hintTimer    = 0f;

    // ─── Taç ────────────────────────────────────────────────────
    private Vector2          _throwInPos;
    private FieldPlayer.Team _throwInTeam;

    // ─────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        Instance = this;

        foreach (Node n in GetTree().GetNodesInGroup("team_red"))
            if (n is FieldPlayer fp) _redPlayers.Add(fp);

        // İnsan oyuncusu: FwdRed1 (slot 4, index 4). Yoksa 0.
        _humanPlayer = _redPlayers.Count > 4 ? _redPlayers[4] : _redPlayers[0];

        _scoreLabel  = GetNodeOrNull<Label>("%ScoreLabel");
        _timerLabel  = GetNodeOrNull<Label>("%TimerLabel");
        _stateLabel  = GetNodeOrNull<Label>("%StateLabel");
        _staminaBar  = GetNodeOrNull<ProgressBar>("%StaminaBar");
        _powerBar    = GetNodeOrNull<ProgressBar>("%PowerBar");
        _energyBar   = GetNodeOrNull<ProgressBar>("%EnergyBar");
        _ratingLabel = GetNodeOrNull<Label>("%RatingLabel");
        _hintLabel   = GetNodeOrNull<Label>("%HintLabel");
        _camera      = GetNodeOrNull<Camera2D>("%MatchCamera");
        _miniMap     = GetNodeOrNull<MiniMap>("%MiniMap");

        if (GameManager.Instance.Energy < 80) GameManager.Instance.Energy = 80;
        if (_camera != null) _camera.Position = CENTER;

        _UpdateScoreUI();
        _SetState(MatchState.KickOff);

        var goalLeft  = GetNodeOrNull<Area2D>("%GoalLeft");
        var goalRight = GetNodeOrNull<Area2D>("%GoalRight");
        if (goalLeft  != null) goalLeft.BodyEntered  += _ => _OnGoal(Team.Blue);
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
                _SetupKickOff();
                _SetState(MatchState.Playing);
                break;

            case MatchState.ThrowIn:
                _DoThrowIn();
                _SetState(MatchState.Playing);
                break;

            case MatchState.HalfTime:
                _secondHalf = true; _elapsed = 0f;
                _FlipSides();
                _SetupKickOff();
                _SetState(MatchState.Playing);
                break;

            case MatchState.FullTime:
                if (Input.IsActionJustPressed("action") || Input.IsActionJustPressed("interact"))
                    WorldManager.Instance.GoTo("WorldMap");
                break;
        }
    }

    // ─── Oynuyor ────────────────────────────────────────────────

    private void _UpdatePlaying(float delta)
    {
        _elapsed += delta;
        float total   = _secondHalf ? _halfDuration + _elapsed : _elapsed;
        if (_timerLabel != null)
            _timerLabel.Text = $"{(int)(total / 60):D2}:{(int)(total % 60):D2}";

        if (!_secondHalf && _elapsed >= _halfDuration)
        {
            _SetState(MatchState.HalfTime);
            if (_stateLabel != null) _stateLabel.Text = "DEVRE ARI";
            _stateTimer = 3f; return;
        }
        if (_secondHalf && _elapsed >= _halfDuration)
        {
            _SetState(MatchState.FullTime);
            if (_stateLabel != null) _stateLabel.Text = "MAÇ BİTTİ";
            _ShowResult(); return;
        }

        var ball = Football.Instance;
        if (ball != null && !ball.IsControlled) _CheckOutOfBounds(ball);
        if (_assistTimer > 0f) _assistTimer -= delta;

        _UpdateCamera(ball);
        _UpdateHUD(ball, delta);
    }

    private void _UpdateHUD(Football? ball, float delta)
    {
        if (_energyBar != null) _energyBar.Value = GameManager.Instance.Energy;

        if (_humanPlayer != null)
        {
            float r = 6.0f + _humanPlayer.GoalsScored * 0.8f
                           + _humanPlayer.PassesAttempted * 0.04f
                           + _humanPlayer.TacklesWon * 0.15f;
            _playerRating = Mathf.Clamp(r, 1f, 10f);
            if (_ratingLabel != null) _ratingLabel.Text = $"{_playerRating:F1} ★";
        }

        _hintTimer -= delta;
        if (_hintTimer <= 0f && ball != null)
        {
            _UpdateHint(ball);
            _hintTimer = 3.5f;
        }
    }

    private void _UpdateHint(Football ball)
    {
        if (_hintLabel == null || _humanPlayer == null) return;

        bool hasBall  = _humanPlayer.HasBall;
        float distGol = _humanPlayer.GlobalPosition.DistanceTo(
            new Vector2(PITCH_W, (GOAL_TOP + GOAL_BOT) / 2f));

        _hintLabel.Text = hasBall && distGol < 380f ? "ŞUTA GEÇ! [SPACE]" :
                          hasBall                    ? "F: Pas Ver" :
                          !ball.IsControlled         ? "TOPA KOŞ!" :
                                                       "SHIFT: Sprint";
    }

    // ─── Gol ────────────────────────────────────────────────────

    private void _OnGoal(Team scoringTeam)
    {
        if (_state != MatchState.Playing) return;

        if (scoringTeam == Team.Red) RedScore++;
        else                         BlueScore++;

        // İnsan gol attıysa
        var ball = Football.Instance;
        if (scoringTeam == Team.Red && ball?.LastToucher == _humanPlayer)
        {
            _humanPlayer!.GoalsScored++;
            _playerRating = Mathf.Clamp(_playerRating + 0.8f, 1f, 10f);
        }

        _lastGoalTeam = scoringTeam;
        _UpdateScoreUI();
        if (_stateLabel != null) _stateLabel.Text = scoringTeam == Team.Red ? "GOL! KIRMIZI!" : "GOL! MAVİ!";
        _stateTimer = 2.5f;
        _SetState(MatchState.Goal);
    }

    // ─── Kick-off ───────────────────────────────────────────────

    private void _SetupKickOff()
    {
        Football.Instance?.ResetTo(CENTER);
        foreach (Node n in GetTree().GetNodesInGroup("field_players"))
            if (n is FieldPlayer fp && fp.HasMeta("start_pos"))
                fp.GlobalPosition = fp.GetMeta("start_pos").AsVector2();
        foreach (Node n in GetTree().GetNodesInGroup("goalkeepers"))
            if (n is GoalkeeperAI gk && gk.HasMeta("start_pos"))
                gk.GlobalPosition = gk.GetMeta("start_pos").AsVector2();
    }

    // ─── Out ────────────────────────────────────────────────────

    private void _CheckOutOfBounds(Football ball)
    {
        Vector2 pos = ball.GlobalPosition;

        if (pos.Y < -20f || pos.Y > PITCH_H + 20f)
        {
            _throwInPos  = new Vector2(pos.X, pos.Y < 0 ? 2f : PITCH_H - 2f);
            _throwInTeam = ball.LastToucher?.PlayerTeam == FieldPlayer.Team.Red
                ? FieldPlayer.Team.Blue : FieldPlayer.Team.Red;
            if (_stateLabel != null) _stateLabel.Text = "TAÇ";
            _stateTimer = 0.7f;
            _SetState(MatchState.ThrowIn);
        }
        else if ((pos.X < -40f || pos.X > PITCH_W + 40f) && (pos.Y < GOAL_TOP || pos.Y > GOAL_BOT))
        {
            bool left = pos.X < 0;
            ball.ResetTo(new Vector2(left ? 70f : PITCH_W - 70f, PITCH_H / 2f));
            if (_stateLabel != null) _stateLabel.Text = "KALE VURUŞU";
            _stateTimer = 0.5f;
        }
    }

    private void _DoThrowIn()
    {
        var ball = Football.Instance;
        if (ball == null) return;
        float clampedX = Mathf.Clamp(_throwInPos.X, 60f, PITCH_W - 60f);
        ball.ResetTo(new Vector2(clampedX, _throwInPos.Y));

        string grp = _throwInTeam == FieldPlayer.Team.Red ? "team_red" : "team_blue";
        FieldPlayer? nearest = null; float nd = float.MaxValue;
        foreach (Node n in GetTree().GetNodesInGroup(grp))
        {
            if (n is not FieldPlayer fp) continue;
            float d = fp.GlobalPosition.DistanceTo(ball.GlobalPosition);
            if (d < nd) { nd = d; nearest = fp; }
        }
        if (nearest != null) ball.GiveControl(nearest);
    }

    // ─── Kamera ─────────────────────────────────────────────────

    private void _UpdateCamera(Football? ball)
    {
        if (_camera == null) return;

        Vector2 focus;
        if (_humanPlayer != null && ball != null)
        {
            // Oyuncu ağırlıklı + topa doğru önceden bakış
            Vector2 midPoint = _humanPlayer.GlobalPosition * 0.5f + ball.GlobalPosition * 0.5f;
            Vector2 lookahead = ball.LinearVelocity * 0.15f;
            focus = midPoint + lookahead;
        }
        else if (_humanPlayer != null)
            focus = _humanPlayer.GlobalPosition;
        else if (ball != null)
            focus = ball.GlobalPosition;
        else return;

        _camera.Position = _camera.Position.Lerp(focus, 0.10f);
    }

    // ─── Yardımcılar ────────────────────────────────────────────

    public bool IsHumanControlled(FieldPlayer p) => p == _humanPlayer;

    public void SetStaminaBar(float v)  { if (_staminaBar != null) _staminaBar.Value = v; }
    public void SetPowerBar(float v)    { if (_powerBar   != null) _powerBar.Value   = v; }
    public void OnAssistOpportunity(FieldPlayer t) { _lastAssist = t; _assistTimer = 3.5f; }

    private void _UpdateScoreUI()
    {
        if (_scoreLabel != null) _scoreLabel.Text = $"{RedScore}  —  {BlueScore}";
    }

    private void _SetState(MatchState s)
    {
        _state = s;
        if (s == MatchState.KickOff && _stateLabel != null)
            _stateLabel.Text = "▶  BAŞLAMAK İÇİN  SPACE  VEYA  E  BASIN  ◀";
        else if (s == MatchState.Playing && _stateLabel != null)
            _stateLabel.Text = "";
    }

    private void _FlipSides()
    {
        foreach (Node n in GetTree().GetNodesInGroup("field_players"))
            if (n is FieldPlayer fp)
                fp.GlobalPosition = new Vector2(PITCH_W - fp.GlobalPosition.X, fp.GlobalPosition.Y);
        foreach (Node n in GetTree().GetNodesInGroup("goalkeepers"))
            if (n is GoalkeeperAI gk)
                gk.GlobalPosition = new Vector2(PITCH_W - gk.GlobalPosition.X, gk.GlobalPosition.Y);
    }

    // ─── Maç sonu ───────────────────────────────────────────────

    private void _ShowResult()
    {
        _ApplyMatchStats();

        int goals   = _humanPlayer?.GoalsScored    ?? 0;
        int passes  = _humanPlayer?.PassesAttempted ?? 0;
        int tackles = _humanPlayer?.TacklesWon      ?? 0;

        if (RedScore > BlueScore)      _playerRating = Mathf.Clamp(_playerRating + 0.5f, 1f, 10f);
        else if (RedScore < BlueScore) _playerRating = Mathf.Clamp(_playerRating - 0.5f, 1f, 10f);

        string result = RedScore > BlueScore ? "KAZANDIN!" :
                        RedScore < BlueScore ? "KAYBETTİN" : "BERABERLİK";

        string highlight = goals >= 2  ? $"MAÇIN ADAMI — {goals} GOL!" :
                           goals == 1  ? "1 GOL ATTIN!" :
                           tackles >= 2 ? $"{tackles} TOPU ALDIM!" :
                           passes >= 5  ? "YARATICI OYUN!" : "İyi mücadele!";

        DialogueManager.Instance.Show(
            $"MAÇ BİTTİ — {result}",
            $"Skor: {RedScore} — {BlueScore}  |  Puan: {_playerRating:F1} ★\n" +
            $"{highlight}\n" +
            $"Gol: {goals}  Pas: {passes}  Top Alma: {tackles}\n" +
            $"Devam etmek için Enter'a bas.",
            () => WorldManager.Instance.GoTo("WorldMap")
        );
    }

    private void _ApplyMatchStats()
    {
        var gm = GameManager.Instance;
        if (RedScore > BlueScore)      { gm.Morale = Mathf.Min(100, gm.Morale + 15); gm.AddEvent("Maçı kazandın! Moral +15"); }
        else if (RedScore < BlueScore) { gm.Morale = Mathf.Max(0, gm.Morale - 5); }

        int goals = _humanPlayer?.GoalsScored ?? 0;
        if (goals > 0) { gm.ShotPower = Mathf.Min(99, gm.ShotPower + goals / 2); }

        gm.Fatigue = Mathf.Min(100, gm.Fatigue + 25);
        gm.Energy  = Mathf.Max(0,   gm.Energy  - 20);
        GameTime.Instance?.AdvanceTime(180f);
    }
}
