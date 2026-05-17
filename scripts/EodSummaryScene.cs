using Godot;

/// res://scripts/EodSummaryScene.cs
/// End-of-day summary: displays all stats and events from the day.
public partial class EodSummaryScene : Node2D
{
    public override void _Ready()
    {
        SequenceManager.Instance.FadeIn();
        GameManager.Instance.DayStep = 2;

        _PopulateUI();
    }

    private void _PopulateUI()
    {
        var gm = GameManager.Instance;

        _SetLabel("%EnergyValue", $"{gm.Energy}");
        _SetLabel("%MoraleValue", $"{gm.Morale}");
        _SetLabel("%FatigueValue", $"{gm.Fatigue}");
        _SetLabel("%ShotValue", $"{gm.ShotPower}");
        _SetLabel("%SprintValue", $"{gm.Sprint}");
        _SetLabel("%TechValue", $"{gm.Technique}");
        _SetLabel("%OverallValue", $"{gm.Overall}");

        var eventsLabel = GetNodeOrNull<Label>("%EventsLabel");
        if (eventsLabel != null)
            eventsLabel.Text = string.Join("\n• ", gm.DayEvents);

        var continueLabel = GetNodeOrNull<Label>("%ContinueLabel");
        if (continueLabel != null)
            continueLabel.Text = "Enter'a bas — devam et";
    }

    private void _SetLabel(string uniqueName, string text)
    {
        var lbl = GetNodeOrNull<Label>(uniqueName);
        if (lbl != null) lbl.Text = text;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("action"))
            SequenceManager.Instance.GoToNextScene();
    }
}
