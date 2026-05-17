using Godot;

/// res://scripts/MatchManager.cs
/// Controls 5v5 match state: score, timer, goal detection, match end.
public partial class MatchManager : Node
{
    [Signal] public delegate void GoalScoredEventHandler(int team);
    [Signal] public delegate void MatchEndedEventHandler(int playerTeamScore, int opponentScore);

    public int PlayerScore { get; private set; } = 0;
    public int OpponentScore { get; private set; } = 0;

    private float _matchDuration = 120f;  // 2 minutes
    private float _elapsed = 0f;
    private bool _matchActive = false;
    private Vector2 _ballStart = new(640, 360);

    private Label? _scoreLabel;
    private Label? _timerLabel;
    private Label? _energyLabel;
    private Label? _moraleLabel;
    private Label? _fatigueLabel;

    private RigidBody2D? _ball;

    public override void _Ready()
    {
        _ball = GetTree().GetFirstNodeInGroup("ball") as RigidBody2D;
        _scoreLabel = GetNodeOrNull<Label>("%ScoreLabel");
        _timerLabel = GetNodeOrNull<Label>("%TimerLabel");
        _energyLabel = GetNodeOrNull<Label>("HUD/HUDControl/StatsPanel/EnergyHUD");
        _moraleLabel = GetNodeOrNull<Label>("HUD/HUDControl/StatsPanel/MoraleHUD");
        _fatigueLabel = GetNodeOrNull<Label>("HUD/HUDControl/StatsPanel/FatigueHUD");

        var goalPlayer = GetNodeOrNull<Area2D>("%GoalPlayer");
        var goalOpponent = GetNodeOrNull<Area2D>("%GoalOpponent");

        if (goalPlayer != null)
            goalPlayer.BodyEntered += (_) => _OnGoal(1);
        if (goalOpponent != null)
            goalOpponent.BodyEntered += (_) => _OnGoal(0);

        _StartMatch();
    }

    public override void _Process(double delta)
    {
        if (!_matchActive) return;
        _elapsed += (float)delta;
        float remaining = Mathf.Max(0, _matchDuration - _elapsed);

        if (_timerLabel != null)
            _timerLabel.Text = $"{(int)(remaining / 60):D2}:{(int)(remaining % 60):D2}";

        var gm = GameManager.Instance;
        if (_energyLabel != null) _energyLabel.Text = $"Enerji: {gm.Energy}";
        if (_moraleLabel != null) _moraleLabel.Text = $"Moral: {gm.Morale}";
        if (_fatigueLabel != null) _fatigueLabel.Text = $"Yorg: {gm.Fatigue}";

        if (remaining <= 0)
            _EndMatch();
    }

    private void _StartMatch()
    {
        _matchActive = true;
        _elapsed = 0f;
        _UpdateScore();
    }

    private void _OnGoal(int scoringTeam)
    {
        if (!_matchActive) return;
        if (scoringTeam == 0)
            PlayerScore++;
        else
            OpponentScore++;

        EmitSignal(SignalName.GoalScored, scoringTeam);
        GameManager.Instance.MatchGoalsScored = PlayerScore;
        _UpdateScore();
        _ResetBall();
    }

    private void _ResetBall()
    {
        if (_ball == null) return;
        _ball.LinearVelocity = Vector2.Zero;
        _ball.AngularVelocity = 0f;
        _ball.GlobalPosition = _ballStart;
    }

    private void _UpdateScore()
    {
        if (_scoreLabel != null)
            _scoreLabel.Text = $"{PlayerScore} - {OpponentScore}";
    }

    private void _EndMatch()
    {
        _matchActive = false;
        EmitSignal(SignalName.MatchEnded, PlayerScore, OpponentScore);

        if (PlayerScore > 0)
            GameManager.Instance.AddEvent($"Golcü! {PlayerScore} gol attın");
        if (PlayerScore > OpponentScore)
        {
            GameManager.Instance.Morale = Mathf.Min(100, GameManager.Instance.Morale + 15);
            GameManager.Instance.AddEvent("Maçı kazandın! (+15 Moral)");
        }
        else
        {
            GameManager.Instance.Morale = Mathf.Max(0, GameManager.Instance.Morale - 5);
        }
        GameManager.Instance.Fatigue = Mathf.Min(100, GameManager.Instance.Fatigue + 20);
        GameManager.Instance.Energy = Mathf.Max(0, GameManager.Instance.Energy - 20);

        CallDeferred(MethodName._ShowMatchEndUI, PlayerScore, OpponentScore);
    }

    private void _ShowMatchEndUI(int playerScore, int opponentScore)
    {
        DialogueManager.Instance.Show(
            "Maç Bitti",
            $"Skor: {playerScore} - {opponentScore}\nDevam etmek için Enter'a bas.",
            () => SequenceManager.Instance.GoToNextScene()
        );
    }
}
