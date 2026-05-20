using Godot;

public partial class MatchManager : Node
{
    private Label? _scoreLabel;
    private Label? _timerLabel;
    private Label? _announce;
    private int[]  _score     = {0, 0};   // [blue, red]
    private float  _timeLeft  = 180f;
    private bool   _over      = false;
    private bool   _paused    = false;

    public override void _Ready()
    {
        _scoreLabel = GetNodeOrNull<Label>("%ScoreLabel");
        _timerLabel = GetNodeOrNull<Label>("%TimerLabel");
        _announce   = GetNodeOrNull<Label>("%GoalAnnounce");

        var gl = GetNodeOrNull<Area2D>("%GoalLeft");
        var gr = GetNodeOrNull<Area2D>("%GoalRight");
        if (gl != null) gl.BodyEntered += _ => _OnGoal(redScored: true);
        if (gr != null) gr.BodyEntered += _ => _OnGoal(redScored: false);

        _UpdateHUD();
    }

    public override void _Process(double delta)
    {
        if (_over || _paused) return;
        _timeLeft -= (float)delta;
        if (_timeLeft <= 0f) { _timeLeft = 0f; _EndMatch(); }
        _UpdateHUD();
    }

    private void _OnGoal(bool redScored)
    {
        if (_over || _paused) return;
        if (redScored) _score[1]++; else _score[0]++;
        _UpdateHUD();
        _ShowAnnounce(redScored ? "GOL!  KIRMIZI →" : "← GOL!  MAVİ",
                      redScored ? new Color(1f, 0.4f, 0.4f) : new Color(0.5f, 0.75f, 1f));
        _paused = true;
        GetTree().CreateTimer(2.0).Timeout += _ResumeAfterGoal;
    }

    private void _ResumeAfterGoal()
    {
        Football.Instance?.ResetTo(Vector2.Zero);
        if (_announce != null) _announce.Visible = false;
        _paused = false;
    }

    private void _EndMatch()
    {
        _over = true;
        string result = _score[0] > _score[1] ? "MAVİ KAZANDI!" :
                        _score[1] > _score[0] ? "KIRMIZI KAZANDI!" : "BERABERE!";
        _ShowAnnounce($"MAÇ BİTTİ — {result}", Colors.White);
        GetTree().CreateTimer(3.5).Timeout += () => WorldManager.Instance?.GoTo("World");
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
            int m = (int)_timeLeft / 60, s = (int)_timeLeft % 60;
            _timerLabel.Text = $"{m}:{s:D2}";
        }
    }
}
