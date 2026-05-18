using Godot;

/// res://scripts/WorldScene.cs
/// Mahalle dünyası — TileMap + oyuncu + kamera.
public partial class WorldScene : Node2D
{
    private Player?   _player;
    private Camera2D? _camera;

    private const int TILE_SIZE = 32;   // piksel/tile (in-game)
    private const int MAP_W     = 60;   // tile sayısı
    private const int MAP_H     = 40;

    public override void _Ready()
    {
        _player = GetNodeOrNull<Player>("%Player");
        _camera = GetNodeOrNull<Camera2D>("%Camera");

        if (_camera != null && _player != null)
            _camera.Position = _player.GlobalPosition;
    }

    public override void _Process(double delta)
    {
        // Kamera oyuncuyu takip eder (smooth)
        if (_camera != null && _player != null)
            _camera.Position = _camera.Position.Lerp(_player.GlobalPosition, 0.12f);
    }
}
