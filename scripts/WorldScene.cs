using Godot;

public partial class WorldScene : Node2D
{
    private Player? _player;
    private Label?  _pitchHint;

    // Sync with BuildWorld.PitchGateWorld
    private static readonly Vector2 PitchGate = new Vector2(1375f, 524f);
    private const float INTERACT_DIST = 64f;

    public override void _Ready()
    {
        _player    = GetNodeOrNull<Player>("%Player");
        _pitchHint = GetNodeOrNull<Label>("%PitchHint");

        if (_player != null)
            _player.Interacted += _OnInteract;
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;
        bool near = _player.GlobalPosition.DistanceTo(PitchGate) < INTERACT_DIST;
        if (_pitchHint != null) _pitchHint.Visible = near;
    }

    private void _OnInteract()
    {
        if (_player == null) return;
        if (_player.GlobalPosition.DistanceTo(PitchGate) < INTERACT_DIST)
            WorldManager.Instance?.GoTo("NeighborhoodMatch");
    }
}
