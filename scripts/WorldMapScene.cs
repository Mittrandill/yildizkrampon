using Godot;

/// res://scripts/WorldMapScene.cs
/// Ana dünya haritası — serbest yürüyüş, konumlara geçiş.
public partial class WorldMapScene : Node2D
{
    private CharacterBody2D _player    = null!;
    private Label           _promptLabel  = null!;
    private Label           _timeLabel    = null!;
    private Label           _playerLabel  = null!;

    private string _nearbyLocation = "";

    private const float Speed       = 180f;
    private const float SprintSpeed = 300f;

    public override void _Ready()
    {
        _player     = GetNode<CharacterBody2D>("Player");
        _promptLabel  = GetNode<Label>("%PromptLabel");
        _timeLabel    = GetNode<Label>("%TimeLabel");
        _playerLabel  = GetNode<Label>("%PlayerLabel");

        _playerLabel.Text = PlayerData.Instance.PlayerName;
        _promptLabel.Visible = false;

        GameTime.Instance.Resume();
        GameTime.Instance.HourChanged    += (h, m) => _RefreshTime();
        GameTime.Instance.PeriodChanged  += _ => _RefreshTime();
        _RefreshTime();

        // Connect all location zones
        foreach (var zone in GetNode("Zones").GetChildren())
        {
            if (zone is Area2D area)
            {
                string locKey = (string)area.GetMeta("location");
                area.BodyEntered += body =>
                {
                    if (body == _player) _OnNearLocation(locKey, area.Name);
                };
                area.BodyExited += body =>
                {
                    if (body == _player && _nearbyLocation == locKey) _OnLeaveLocation();
                };
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        float spd = Input.IsActionPressed("sprint") ? SprintSpeed : Speed;
        _player.Velocity = dir * spd;
        _player.MoveAndSlide();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact") && _nearbyLocation != "")
            WorldManager.Instance.GoTo(_nearbyLocation);

        // M = hızlı maç kısayolu
        if (@event is InputEventKey key && key.Pressed && !key.Echo
            && key.Keycode == Key.M)
            WorldManager.Instance.GoTo("Match");
    }

    private void _OnNearLocation(string locKey, string displayName)
    {
        _nearbyLocation = locKey;
        _promptLabel.Text = $"[E]  {displayName}  →  Gir";
        _promptLabel.Visible = true;
    }

    private void _OnLeaveLocation()
    {
        _nearbyLocation = "";
        _promptLabel.Visible = false;
    }

    private void _RefreshTime()
    {
        _timeLabel.Text = $"{GameTime.Instance.DayString}   {GameTime.Instance.TimeString}   {GameTime.Instance.PeriodName}";
    }
}
