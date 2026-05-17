using Godot;
using System.Collections.Generic;

/// res://scripts/SimpleInteriorScene.cs
/// Genel iç mekan scripti — BakkalInterior, SporTesisi, Fitness, CayBahcesi için.
/// Zon meta verilerinden etkileri okur.
public partial class SimpleInteriorScene : Node2D
{
    private CharacterBody2D _player    = null!;
    private Label           _promptLabel  = null!;
    private Label           _timeLabel    = null!;
    private Label           _feedbackLabel= null!;

    private string _nearbyAction = "";
    private float  _feedbackTimer = 0f;
    private bool   _busy = false;

    private const float Speed = 160f;

    public override void _Ready()
    {
        _player       = GetNode<CharacterBody2D>("Player");
        _promptLabel  = GetNode<Label>("%PromptLabel");
        _timeLabel    = GetNode<Label>("%TimeLabel");
        _feedbackLabel= GetNode<Label>("%FeedbackLabel");

        _promptLabel.Visible   = false;
        _feedbackLabel.Visible = false;

        GameTime.Instance.Resume();
        GameTime.Instance.HourChanged += (h, m) => _RefreshTime();
        _RefreshTime();

        foreach (var zone in GetNode("InteractZones").GetChildren())
        {
            if (zone is Area2D area)
            {
                string action = (string)area.GetMeta("action");
                string prompt = (string)area.GetMeta("prompt");
                area.BodyEntered += body => { if (body == _player) _OnNear(action, prompt); };
                area.BodyExited  += body => { if (body == _player && _nearbyAction == action) _OnLeave(); };
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_busy)
        {
            var dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
            _player.Velocity = dir * Speed;
            _player.MoveAndSlide();
        }

        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= (float)delta;
            if (_feedbackTimer <= 0f) { _feedbackLabel.Visible = false; _busy = false; }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact") && _nearbyAction != "" && !_busy)
            _DoAction(_nearbyAction);
    }

    private void _OnNear(string action, string prompt)
    {
        _nearbyAction = action;
        _promptLabel.Text    = prompt;
        _promptLabel.Visible = true;
    }

    private void _OnLeave()
    {
        _nearbyAction        = "";
        _promptLabel.Visible = false;
    }

    private void _DoAction(string action)
    {
        if (action == "Exit") { WorldManager.Instance.GoTo("WorldMap"); return; }

        // Find the zone and read its effect metadata
        foreach (var zone in GetNode("InteractZones").GetChildren())
        {
            if (zone is Area2D area && (string)area.GetMeta("action") == action)
            {
                string feedback = area.HasMeta("feedback") ? (string)area.GetMeta("feedback") : "";
                float  timeCost = area.HasMeta("time_min") ? (float)area.GetMeta("time_min") : 0f;
                string effects  = area.HasMeta("effects")  ? (string)area.GetMeta("effects")  : "";

                if (timeCost > 0) GameTime.Instance.AdvanceTime(timeCost);
                _ApplyEffects(effects);
                _ShowFeedback(feedback);
                break;
            }
        }
    }

    // effects format: "Energy:+20,Fatigue:-15,ShotPower:+2" etc.
    private void _ApplyEffects(string effects)
    {
        if (string.IsNullOrEmpty(effects)) return;
        var gm = GameManager.Instance;
        foreach (var part in effects.Split(','))
        {
            var kv = part.Trim().Split(':');
            if (kv.Length != 2) continue;
            if (!int.TryParse(kv[1], out int delta)) continue;
            switch (kv[0])
            {
                case "Energy":    gm.Energy    = Mathf.Clamp(gm.Energy    + delta, 0, 100); break;
                case "Morale":    gm.Morale    = Mathf.Clamp(gm.Morale    + delta, 0, 100); break;
                case "Fatigue":   gm.Fatigue   = Mathf.Clamp(gm.Fatigue   + delta, 0, 100); break;
                case "ShotPower": gm.ShotPower = Mathf.Clamp(gm.ShotPower + delta, 0, 99);  break;
                case "Sprint":    gm.Sprint    = Mathf.Clamp(gm.Sprint    + delta, 0, 99);  break;
                case "Technique": gm.Technique = Mathf.Clamp(gm.Technique + delta, 0, 99);  break;
                case "Stamina":   gm.Stamina   = Mathf.Clamp(gm.Stamina   + delta, 0, 99);  break;
            }
        }
    }

    private void _ShowFeedback(string msg)
    {
        _feedbackLabel.Text    = msg;
        _feedbackLabel.Visible = true;
        _feedbackTimer         = 2.5f;
        _busy                  = true;
    }

    private void _RefreshTime()
    {
        _timeLabel.Text = $"{GameTime.Instance.DayString}   {GameTime.Instance.TimeString}   {GameTime.Instance.PeriodName}";
    }
}
