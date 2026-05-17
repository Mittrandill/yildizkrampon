using Godot;

/// res://scripts/TrainingScene.cs
/// Shooting drill mini-game: time your shot to hit the target.
public partial class TrainingScene : Node2D
{
    private float _markerPos = 0f;
    private float _markerSpeed = 1.8f;
    private bool _active = false;
    private int _shotsLeft = 5;
    private int _hits = 0;

    private ColorRect? _marker;
    private ColorRect? _targetZone;
    private Label? _instructionLabel;
    private Label? _resultLabel;
    private Label? _shotsLabel;

    private const float BarWidth = 400f;
    private const float TargetStart = 0.38f;
    private const float TargetEnd = 0.62f;

    public override void _Ready()
    {
        SequenceManager.Instance.FadeIn();
        GameManager.Instance.DayStep = 2;

        _marker = GetNodeOrNull<ColorRect>("%Marker");
        _targetZone = GetNodeOrNull<ColorRect>("%TargetZone");
        _instructionLabel = GetNodeOrNull<Label>("%InstructionLabel");
        _resultLabel = GetNodeOrNull<Label>("%ResultLabel");
        _shotsLabel = GetNodeOrNull<Label>("%ShotsLabel");

        if (_instructionLabel != null)
            _instructionLabel.Text = "Şut antrenmanı!\nTop hedef bölgesindeyken SPACE'e bas.";

        // Intro dialogue then start drill
        DialogueManager.Instance.Show(
            "Kemal Hoca",
            "Şut çalışması yapacağız. Hazır mısın?",
            _StartDrill
        );
    }

    private void _StartDrill()
    {
        _active = true;
        _shotsLeft = 5;
        _hits = 0;
        if (_resultLabel != null) _resultLabel.Text = "";
        _UpdateShotsLabel();
    }

    public override void _Process(double delta)
    {
        if (!_active || _marker == null) return;

        _markerPos += _markerSpeed * (float)delta;
        if (_markerPos > 1f) { _markerPos = 0f; }

        float x = 80f + _markerPos * BarWidth;
        _marker.Position = new Vector2(x - 5, _marker.Position.Y);

        if (Input.IsActionJustPressed("action"))
            _Shoot();
    }

    private void _Shoot()
    {
        if (!_active) return;

        bool hit = _markerPos >= TargetStart && _markerPos <= TargetEnd;
        if (hit)
        {
            _hits++;
            if (_resultLabel != null) _resultLabel.Text = "GOL! ⚽";
        }
        else
        {
            if (_resultLabel != null) _resultLabel.Text = "Kaçtı!";
        }

        GameManager.Instance.Fatigue = Mathf.Min(100, GameManager.Instance.Fatigue + 3);
        GameManager.Instance.Energy = Mathf.Max(0, GameManager.Instance.Energy - 5);

        _shotsLeft--;
        _UpdateShotsLabel();

        if (_shotsLeft <= 0)
        {
            _active = false;
            _FinishDrill();
        }
    }

    private void _UpdateShotsLabel()
    {
        if (_shotsLabel != null)
            _shotsLabel.Text = $"Şutlar: {_shotsLeft} kaldı";
    }

    private void _FinishDrill()
    {
        int bonusMultiplier = GameManager.Instance.BoughtProteinBar ? 2 : 1;
        int shotGain = _hits * 2 * bonusMultiplier;
        int techGain = _hits * bonusMultiplier;

        GameManager.Instance.ShotPower += shotGain;
        GameManager.Instance.Technique += techGain;
        GameManager.Instance.ClampStats();
        GameManager.Instance.RecalcOverall();

        string proteinNote = GameManager.Instance.BoughtProteinBar ? " (x2 bonus!)" : "";
        GameManager.Instance.AddEvent($"Şut antrenmanı: {_hits}/5 isabet{proteinNote} (+{shotGain} Şut, +{techGain} Teknik)");

        string msg = $"{_hits}/5 gol attın!\nŞut Gücü +{shotGain}, Teknik +{techGain}{proteinNote}";
        DialogueManager.Instance.Show("Kemal Hoca", msg, () => SequenceManager.Instance.GoToNextScene());
    }
}
