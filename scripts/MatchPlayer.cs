using Godot;

public partial class MatchPlayer : CharacterBody2D
{
    private const float SPEED        = 225f;
    private const float SPRINT_SPEED = 345f;
    private const float KICK_RADIUS  = 36f;
    private const float KICK_FORCE   = 730f;
    private const float PUSH_FORCE   = 210f;

    private float   _kickCooldown;
    private Vector2 _facing = Vector2.Right;

    public override void _Ready()
    {
        CollisionLayer = 1u;
        CollisionMask  = 2u | 16u;
        _BuildVisual();
    }

    public override void _PhysicsProcess(double delta)
    {
        var  input  = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        bool sprint = Input.IsActionPressed("sprint");
        Velocity = input * (sprint ? SPRINT_SPEED : SPEED);
        if (input.LengthSquared() > 0.01f) _facing = input.Normalized();
        MoveAndSlide();

        _kickCooldown -= (float)delta;
        _PassivePush();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.Space)
            _PowerKick();
    }

    private void _PassivePush()
    {
        if (Football.Instance == null || _kickCooldown > 0f) return;
        var d = Football.Instance.GlobalPosition - GlobalPosition;
        if (d.Length() < KICK_RADIUS * 0.5f)
            Football.Instance.Kick(d.Normalized() * PUSH_FORCE);
    }

    private void _PowerKick()
    {
        if (Football.Instance == null) return;
        var d = Football.Instance.GlobalPosition - GlobalPosition;
        if (d.Length() > KICK_RADIUS) return;
        var dir = Velocity.LengthSquared() > 100f ? Velocity.Normalized() : _facing;
        Football.Instance.Kick(dir * KICK_FORCE);
        _kickCooldown = 0.28f;
    }

    private void _BuildVisual()
    {
        // Blue kit
        var body = new Polygon2D { Color = new Color(0.12f, 0.38f, 0.92f), ZIndex = 0 };
        body.Polygon = _Circle(10f, 16);
        AddChild(body);

        // White star / crest
        var crest = new Polygon2D { Color = Colors.White, ZIndex = 1 };
        crest.Polygon = _Circle(4.5f, 6);
        AddChild(crest);

        // Yellow triangle indicator (above player)
        var arrow = new Polygon2D { Color = new Color(1f, 0.88f, 0.1f), ZIndex = 2 };
        arrow.Polygon = new Vector2[] { new(0,-15f), new(-5f,-9f), new(5f,-9f) };
        AddChild(arrow);

        var col = new CollisionShape2D();
        col.Shape = new CapsuleShape2D { Radius = 8f, Height = 14f };
        col.Position = new Vector2(0f, 2f);
        AddChild(col);
    }

    private static Vector2[] _Circle(float r, int seg)
    {
        var pts = new Vector2[seg];
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.Tau;
            pts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        return pts;
    }
}
