using Godot;

public partial class MatchScene : Node2D
{
    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.Escape)
            WorldManager.Instance?.GoTo("World");
    }
}
