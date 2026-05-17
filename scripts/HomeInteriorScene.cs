using Godot;

/// res://scripts/HomeInteriorScene.cs
/// Ev iç mekanı — 4 oda yürüyüş, etkileşimli objeler, sahaya çıkış.
public partial class HomeInteriorScene : Node2D
{
    private CharacterBody2D _player = null!;
    private Label _promptLabel      = null!;
    private Label _timeLabel        = null!;
    private Label _roomLabel        = null!;
    private Label _feedbackLabel    = null!;

    private string _nearbyAction = "";
    private float  _feedbackTimer = 0f;

    private const float Speed = 160f;

    public override void _Ready()
    {
        _player      = GetNode<CharacterBody2D>("Player");
        _promptLabel = GetNode<Label>("%PromptLabel");
        _timeLabel   = GetNode<Label>("%TimeLabel");
        _roomLabel   = GetNode<Label>("%RoomLabel");
        _feedbackLabel = GetNode<Label>("%FeedbackLabel");

        _promptLabel.Visible  = false;
        _feedbackLabel.Visible = false;

        GameTime.Instance.Resume();
        GameTime.Instance.HourChanged += (h, m) => _RefreshTime();
        _RefreshTime();

        // Connect all interaction zones
        foreach (var zone in GetNode("InteractZones").GetChildren())
        {
            if (zone is Area2D area)
            {
                string action = (string)area.GetMeta("action");
                string prompt = (string)area.GetMeta("prompt");
                area.BodyEntered += body =>
                {
                    if (body == _player) _OnNear(action, prompt);
                };
                area.BodyExited += body =>
                {
                    if (body == _player && _nearbyAction == action) _OnLeave();
                };
            }
        }

        // Room detector zones
        foreach (var zone in GetNode("RoomZones").GetChildren())
        {
            if (zone is Area2D area)
            {
                string roomName = (string)area.GetMeta("room");
                area.BodyEntered += body =>
                {
                    if (body == _player) _roomLabel.Text = roomName;
                };
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        _player.Velocity = dir * Speed;
        _player.MoveAndSlide();

        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= (float)delta;
            if (_feedbackTimer <= 0f)
                _feedbackLabel.Visible = false;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact") && _nearbyAction != "")
            _DoAction(_nearbyAction);
    }

    private void _OnNear(string action, string prompt)
    {
        _nearbyAction = action;
        _promptLabel.Text = prompt;
        _promptLabel.Visible = true;
    }

    private void _OnLeave()
    {
        _nearbyAction = "";
        _promptLabel.Visible = false;
    }

    private void _DoAction(string action)
    {
        switch (action)
        {
            case "Sleep":
                GameTime.Instance.SleepToMorning();
                _ShowFeedback("Günaydın! Enerji yenilendi.");
                GameManager.Instance.Energy  = Mathf.Min(GameManager.Instance.Energy  + 60, 100);
                GameManager.Instance.Fatigue = Mathf.Max(GameManager.Instance.Fatigue - 40, 0);
                break;

            case "Study":
                if (GameManager.Instance.Energy < 10) { _ShowFeedback("Çok yorgunsun, önce uyu!"); return; }
                GameTime.Instance.AdvanceTime(60f);
                GameManager.Instance.Technique += 1;
                GameManager.Instance.Energy    -= 5;
                GameManager.Instance.ClampStats();
                _ShowFeedback("Ders çalıştın! Teknik +1");
                break;

            case "Eat":
                GameTime.Instance.AdvanceTime(30f);
                GameManager.Instance.Energy  = Mathf.Min(GameManager.Instance.Energy + 20, 100);
                GameManager.Instance.Morale  = Mathf.Min(GameManager.Instance.Morale + 5, 100);
                _ShowFeedback("Yemek yedin! Enerji +20, Moral +5");
                break;

            case "Shower":
                GameTime.Instance.AdvanceTime(30f);
                GameManager.Instance.Fatigue = Mathf.Max(GameManager.Instance.Fatigue - 15, 0);
                GameManager.Instance.Morale  = Mathf.Min(GameManager.Instance.Morale + 5, 100);
                _ShowFeedback("Duş aldın! Yorgunluk -15, Moral +5");
                break;

            case "Exit":
                WorldManager.Instance.GoTo("WorldMap");
                return;
        }
    }

    private void _ShowFeedback(string msg)
    {
        _feedbackLabel.Text    = msg;
        _feedbackLabel.Visible = true;
        _feedbackTimer         = 2.5f;
    }

    private void _RefreshTime()
    {
        _timeLabel.Text = $"{GameTime.Instance.DayString}   {GameTime.Instance.TimeString}   {GameTime.Instance.PeriodName}";
    }
}
