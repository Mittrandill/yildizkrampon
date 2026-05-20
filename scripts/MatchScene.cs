using Godot;

public partial class MatchScene : Node2D
{
    private Camera2D? _cam;

    // At zoom 2.0x on 1280×720 viewport → 640×360 world pixels visible.
    // Pitch is 1000×560, goals add ~52px each side → total ~1064×560.
    // Camera travel: X ±212, Y ±100 to always keep pitch in frame.
    private const float ZOOM_LEVEL = 2.0f;
    private const float CAM_LIMIT_X = 212f;
    private const float CAM_LIMIT_Y = 100f;
    private const float CAM_SPEED   = 5f;

    public override void _Ready()
    {
        _cam = GetNodeOrNull<Camera2D>("Camera");
        if (_cam != null)
            _cam.Zoom = new Vector2(ZOOM_LEVEL, ZOOM_LEVEL);
    }

    public override void _Process(double delta)
    {
        if (_cam == null || Football.Instance == null) return;

        // Follow ball, blend slightly toward human player so it's predictable
        var target = Football.Instance.GlobalPosition;
        var human  = GetNodeOrNull<MatchPlayer>("%MatchPlayer");
        if (human != null)
            target = target.Lerp(human.GlobalPosition, 0.3f);

        _cam.GlobalPosition = _cam.GlobalPosition.Lerp(target, (float)delta * CAM_SPEED);
        _cam.GlobalPosition = new Vector2(
            Mathf.Clamp(_cam.GlobalPosition.X, -CAM_LIMIT_X, CAM_LIMIT_X),
            Mathf.Clamp(_cam.GlobalPosition.Y, -CAM_LIMIT_Y, CAM_LIMIT_Y)
        );
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.Escape)
            WorldManager.Instance?.GoTo("World");
    }
}
