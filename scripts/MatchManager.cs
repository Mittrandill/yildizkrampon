using Godot;
using System.Collections.Generic;

public partial class MatchManager : Node
{
    public static MatchManager? Instance { get; private set; }

    // ── Constants ───────────────────────────────────────────────────────────────

    private const float HALF_DURATION = 180f; // 3 minutes per half
    private const float PW_HALF       = 480f; // half pitch width
    private const float PH_HALF       = 260f; // half pitch height
    private const float SET_PIECE_DELAY = 1.5f;

    // ── State ───────────────────────────────────────────────────────────────────

    private enum Phase { Playing, SetPiece, HalfTime, MatchOver }

    private Phase _phase         = Phase.Playing;
    private int   _half          = 1;         // 1 or 2
    private float _halfTimer     = HALF_DURATION;
    private int[] _score         = { 0, 0 };  // [blue, red]
    private bool  _blueAttacksRight = true;   // blue attacks right in first half
    private float _setPieceTimer = 0f;
    private System.Action? _setPieceAction;

    public bool IsSetPiece => _phase == Phase.SetPiece || _phase == Phase.HalfTime;

    // ── HUD refs ────────────────────────────────────────────────────────────────

    private Label? _scoreLabel;
    private Label? _timerLabel;
    private Label? _announce;

    // ── Players refs ────────────────────────────────────────────────────────────

    private MatchPlayer?      _human;
    private List<FootballAI>  _allAI  = new();
    private List<FootballAI>  _blueAI = new();
    private List<FootballAI>  _redAI  = new();

    // ── Override Ready ─────────────────────────────────────────────────────────

    public override void _Ready()
    {
        Instance    = this;
        _scoreLabel = GetNodeOrNull<Label>("%ScoreLabel");
        _timerLabel = GetNodeOrNull<Label>("%TimerLabel");
        _announce   = GetNodeOrNull<Label>("%GoalAnnounce");

        // Collect player references
        _human = GetNodeOrNull<MatchPlayer>("%MatchPlayer");
        foreach (var node in GetTree().GetNodesInGroup("team_blue"))
            if (node is FootballAI ai) { _blueAI.Add(ai); _allAI.Add(ai); }
        foreach (var node in GetTree().GetNodesInGroup("team_red"))
            if (node is FootballAI ai) { _redAI.Add(ai); _allAI.Add(ai); }

        _ApplyAttackDirections();
        _DoKickOff(blueKicksOff: true);
        _UpdateHUD();
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // ── Process ─────────────────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (_phase == Phase.SetPiece)
        {
            _setPieceTimer -= dt;
            if (_setPieceTimer <= 0f)
            {
                _phase = Phase.Playing;
                // Save + clear BEFORE invoke so any chained _StartSetPiece call
                // inside the action can set its own new action without being wiped.
                var action = _setPieceAction;
                _setPieceAction = null;
                action?.Invoke();
                if (_phase == Phase.Playing) // only unfreeze if action didn't chain into another set piece
                    _SetInputFrozen(false);
            }
            return;
        }

        if (_phase != Phase.Playing) return;

        _halfTimer -= dt;
        if (_halfTimer <= 0f)
        {
            _halfTimer = 0f;
            if (_half == 1)
                _StartHalfTime();
            else
                _EndMatch();
        }

        _CheckOutOfBounds();
        _UpdateHUD();
    }

    // ── Out-of-bounds detection ─────────────────────────────────────────────────

    private void _CheckOutOfBounds()
    {
        if (Football.Instance == null || Football.Instance.Frozen) return;
        var bp = Football.Instance.GlobalPosition;

        if (Mathf.Abs(bp.Y) > PH_HALF + 2f)
        {
            _TriggerThrowIn(bp);
            return;
        }

        if (Mathf.Abs(bp.X) > PW_HALF + 2f)
        {
            bool inGoalY = Mathf.Abs(bp.Y) < 63f; // GH/2 + margin
            if (inGoalY)
            {
                // Ball crossed the goal line inside the post width → goal!
                _ScoreGoal(isLeft: bp.X < 0f);
                return;
            }

            int  lastTeam     = Football.Instance.LastTouchedTeam;
            bool exitedLeft   = bp.X < 0f;
            bool exitedRight  = bp.X > 0f;

            // Goal kick if last touched by attacker, corner if defender
            bool attackerTouchedLeft  = exitedLeft  && (lastTeam == (_blueAttacksRight ? 0 : 1));
            bool attackerTouchedRight = exitedRight && (lastTeam == (_blueAttacksRight ? 1 : 0));

            if (attackerTouchedLeft || attackerTouchedRight)
                _TriggerGoalKick(exitedLeft);
            else
                _TriggerCorner(exitedLeft, bp.Y < 0f);
        }
    }

    // ── Throw-in ────────────────────────────────────────────────────────────────

    private void _TriggerThrowIn(Vector2 ballPos)
    {
        int throwTeam = Football.Instance!.LastTouchedTeam == 0 ? 1 : 0; // opposite team
        float clampX  = Mathf.Clamp(ballPos.X, -PW_HALF + 20f, PW_HALF - 20f);
        float edgeY   = ballPos.Y > 0f ? PH_HALF : -PH_HALF;
        var   throwPos = new Vector2(clampX, edgeY);
        float inwardY = ballPos.Y > 0f ? -200f : 200f;

        Football.Instance.ResetTo(throwPos);
        Football.Instance.Frozen = true;

        _StartSetPiece(SET_PIECE_DELAY, () =>
        {
            Football.Instance.Frozen = false;
            Football.Instance.Kick(new Vector2(0f, inwardY).Normalized() * 350f, throwTeam);
        });

        _RepositionForThrowIn(throwTeam, throwPos);
        _ShowAnnounce("TARAÇ!", Colors.White);
    }

    // ── Goal kick ───────────────────────────────────────────────────────────────

    private void _TriggerGoalKick(bool leftSide)
    {
        // GK of the defending side takes the kick
        bool blueDefendsLeft = !_blueAttacksRight;
        bool blueGKKicks     = leftSide == blueDefendsLeft;
        int  kickTeam        = blueGKKicks ? 0 : 1;
        float gkX = leftSide ? -440f : 440f;
        var   gkPos = new Vector2(gkX, 0f);

        Football.Instance!.ResetTo(new Vector2(leftSide ? -430f : 430f, 0f));
        Football.Instance.Frozen = true;

        _StartSetPiece(SET_PIECE_DELAY, () =>
        {
            Football.Instance.Frozen = false;
            float targetX = leftSide ? 200f : -200f;
            float spread  = (float)GD.RandRange(-80.0, 80.0);
            var dir = (new Vector2(targetX, spread) - Football.Instance.GlobalPosition).Normalized();
            Football.Instance.Kick(dir * 580f, kickTeam);
        });

        _ShowAnnounce("KALE VURUŞU", Colors.White);
    }

    // ── Corner ──────────────────────────────────────────────────────────────────

    private void _TriggerCorner(bool leftSide, bool topSide)
    {
        // Attacking team wins corner
        bool blueAttacks    = _blueAttacksRight ? !leftSide : leftSide;
        int  cornerTeam     = blueAttacks ? 0 : 1;
        float cx = leftSide ? -PW_HALF : PW_HALF;
        float cy = topSide  ? -PH_HALF : PH_HALF;
        var   cornerPos = new Vector2(cx, cy);

        Football.Instance!.ResetTo(cornerPos);
        Football.Instance.Frozen = true;

        _StartSetPiece(SET_PIECE_DELAY, () =>
        {
            Football.Instance.Frozen = false;
            // Kick toward goal with aerial component
            float targetX = leftSide ? 400f : -400f;
            float targetY = (float)GD.RandRange(-80.0, 80.0);
            var dir = (new Vector2(targetX, targetY) - cornerPos).Normalized();
            Football.Instance.AerialKick(dir * 480f, 180f, cornerTeam);
        });

        _RepositionForCorner(cornerTeam, cornerPos);
        _ShowAnnounce("KÖŞE VURUŞU!", Colors.White);
    }

    // ── Goal scored ─────────────────────────────────────────────────────────────

    private void _ScoreGoal(bool isLeft)
    {
        if (_phase != Phase.Playing) return;

        // Ball entered left goal → right side scored (and vice versa)
        bool blueScored = isLeft ? !_blueAttacksRight : _blueAttacksRight;
        if (blueScored) _score[0]++; else _score[1]++;
        _UpdateHUD();

        Football.Instance!.Frozen = true; // freeze ball immediately

        var color = blueScored ? new Color(0.5f, 0.75f, 1f) : new Color(1f, 0.4f, 0.4f);
        _ShowAnnounce(blueScored ? "← GOL!  MAVİ" : "GOL!  KIRMIZI →", color);

        // Scored-against team kicks off
        bool redKicksOff = blueScored;
        _StartSetPiece(2.5f, () =>
        {
            if (_announce != null) _announce.Visible = false;
            _DoKickOff(blueKicksOff: !redKicksOff);
        });
    }

    // ── Kick-off ────────────────────────────────────────────────────────────────

    private void _DoKickOff(bool blueKicksOff)
    {
        Football.Instance?.ResetTo(Vector2.Zero);
        if (Football.Instance != null) Football.Instance.Frozen = true;

        _RepositionForKickOff(blueKicksOff);
        _StartSetPiece(SET_PIECE_DELAY, () =>
        {
            if (Football.Instance != null) Football.Instance.Frozen = false;
        });
    }

    private void _RepositionForKickOff(bool blueKicksOff)
    {
        // Blue kicks off: human + BlueMF near center, others in own halves
        if (_human != null)
        {
            float hx = blueKicksOff ? -50f : 160f;
            _human.GlobalPosition = new Vector2(hx, 30f);
        }

        foreach (var ai in _allAI)
        {
            bool isBlue = ai.IsBlueTeam;
            bool kicks  = isBlue ? blueKicksOff : !blueKicksOff;
            var  tb     = ai.TacticalBase();

            // All players must be in own half
            float sign      = isBlue ? (_blueAttacksRight ? -1f : 1f) : (_blueAttacksRight ? 1f : -1f);
            float minX      = sign * 30f;  // at least 30px into own half
            float finalX    = (sign < 0f) ? Mathf.Min(tb.X, minX) : Mathf.Max(tb.X, minX);
            ai.GlobalPosition = new Vector2(finalX, tb.Y);
        }
    }

    // ── Half time ───────────────────────────────────────────────────────────────

    private void _StartHalfTime()
    {
        _phase = Phase.HalfTime;
        _SetInputFrozen(true);
        if (Football.Instance != null) Football.Instance.Frozen = true;
        _ShowAnnounce("İLK YARI BİTTİ", Colors.Yellow);
        GetTree().CreateTimer(3.0).Timeout += _BeginSecondHalf;
    }

    private void _BeginSecondHalf()
    {
        _half            = 2;
        _halfTimer       = HALF_DURATION;
        _blueAttacksRight = !_blueAttacksRight;
        _ApplyAttackDirections();
        if (_announce != null) _announce.Visible = false;
        _phase = Phase.Playing;
        _SetInputFrozen(false);
        if (Football.Instance != null) Football.Instance.Frozen = false;
        _DoKickOff(blueKicksOff: false); // red kicks off second half
    }

    // ── Match over ──────────────────────────────────────────────────────────────

    private void _EndMatch()
    {
        _phase = Phase.MatchOver;
        _SetInputFrozen(true);
        string result = _score[0] > _score[1] ? "MAVİ KAZANDI!" :
                        _score[1] > _score[0] ? "KIRMIZI KAZANDI!" : "BERABERE!";
        _ShowAnnounce($"MAÇ BİTTİ — {result}", Colors.White);
        GetTree().CreateTimer(3.5).Timeout += () => WorldManager.Instance?.GoTo("World");
    }

    // ── Reposition helpers ──────────────────────────────────────────────────────

    private void _RepositionForThrowIn(int throwTeam, Vector2 throwPos)
    {
        foreach (var ai in _allAI)
        {
            var tb = ai.TacticalBase();
            ai.GlobalPosition = new Vector2(
                Mathf.Clamp(tb.X, -PW_HALF + 30f, PW_HALF - 30f),
                Mathf.Clamp(tb.Y, -PH_HALF + 30f, PH_HALF - 30f)
            );
        }
    }

    private void _RepositionForCorner(int cornerTeam, Vector2 cornerPos)
    {
        string attackGroup = cornerTeam == 0 ? "team_blue" : "team_red";
        string defGroup    = cornerTeam == 0 ? "team_red"  : "team_blue";

        // Attackers into box
        float boxX = cornerPos.X < 0f ? -350f : 350f;
        float[] ays = { -40f, 0f, 40f };
        int i = 0;
        foreach (var node in GetTree().GetNodesInGroup(attackGroup))
        {
            if (node is FootballAI ai && ai.PlayerRole != FootballAI.Role.GK)
            {
                ai.GlobalPosition = new Vector2(boxX, ays[i % ays.Length]);
                i++;
            }
        }

        // Defenders mark
        int j = 0;
        foreach (var node in GetTree().GetNodesInGroup(defGroup))
        {
            if (node is FootballAI ai && ai.PlayerRole != FootballAI.Role.GK)
            {
                float dy = ays[j % ays.Length] + 15f;
                ai.GlobalPosition = new Vector2(boxX + (cornerPos.X < 0f ? 30f : -30f), dy);
                j++;
            }
        }
    }

    // ── Utilities ───────────────────────────────────────────────────────────────

    private void _StartSetPiece(float delay, System.Action onDone)
    {
        _phase            = Phase.SetPiece;
        _setPieceTimer    = delay;
        _setPieceAction   = onDone;
        _SetInputFrozen(true);
    }

    private void _SetInputFrozen(bool frozen)
    {
        if (_human != null) _human.InputFrozen = frozen;
        // AI stops via IsSetPiece property check in their own _PhysicsProcess
    }

    private void _ApplyAttackDirections()
    {
        foreach (var ai in _blueAI) ai.AttacksRight = _blueAttacksRight;
        foreach (var ai in _redAI)  ai.AttacksRight = !_blueAttacksRight;
    }

    private void _ShowAnnounce(string text, Color color)
    {
        if (_announce == null) return;
        _announce.Text = text;
        _announce.AddThemeColorOverride("font_color", color);
        _announce.Visible = true;
    }

    private void _UpdateHUD()
    {
        if (_scoreLabel != null) _scoreLabel.Text = $"{_score[0]}  —  {_score[1]}";
        if (_timerLabel != null)
        {
            int m = (int)_halfTimer / 60, s = (int)_halfTimer % 60;
            _timerLabel.Text = $"{_half}.Y {m}:{s:D2}";
        }
    }
}
