using Godot;

public partial class WorldScene : Node2D
{
    private Player?   _player;
    private Camera2D? _camera;
    private Label?    _hint;

    // Ev kapısı dünya koordinatı (BuildWorld ile senkron)
    public static readonly Vector2 HouseDoorWorld = new Vector2(288f, 368f);
    private const float INTERACT_DIST = 48f;
    private bool _nearHouse = false;

    public override void _Ready()
    {
        _player = GetNodeOrNull<Player>("%Player");
        _camera = GetNodeOrNull<Camera2D>("%Camera");
        _hint   = GetNodeOrNull<Label>("%HouseHint");

        if (_player != null)
            _player.Interacted += _OnInteract;

        if (_camera != null && _player != null)
            _camera.Position = _player.GlobalPosition;
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;

        _nearHouse = _player.GlobalPosition.DistanceTo(HouseDoorWorld) < INTERACT_DIST;

        if (_hint != null)
            _hint.Visible = _nearHouse;
    }

    private void _OnInteract()
    {
        if (_nearHouse)
            WorldManager.Instance?.GoTo("HomeInterior");
    }
}
