using Godot;

public partial class WorldScene : Node2D
{
    private Player?   _player;
    private Camera2D? _camera;

    public override void _Ready()
    {
        _player = GetNodeOrNull<Player>("%Player");
        _camera = GetNodeOrNull<Camera2D>("%Camera");

        if (_camera != null && _player != null)
            _camera.Position = _player.GlobalPosition;
    }
}
