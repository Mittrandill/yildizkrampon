using Godot;

public partial class FootballAI : CharacterBody2D
{
    [Export] public bool    IsBlueTeam { get; set; } = false;
    [Export] public Vector2 BasePos    { get; set; } = Vector2.Zero;

    private float _kickCooldown;
    private float _speed;
    private const float CHASE_RADIUS = 300f;
    private const float KICK_RADIUS  = 38f;
    private const float KICK_FORCE   = 570f;

    public override void _Ready()
    {
        CollisionLayer = 2u;
        CollisionMask  = 1u | 2u | 16u;
        _speed = 185f + (float)GD.RandRange(0.0, 25.0);
        _BuildVisual();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Football.Instance == null) { Velocity = Vector2.Zero; return; }
        _kickCooldown -= (float)delta;

        var   ball   = Football.Instance.GlobalPosition;
        var   toBall = ball - GlobalPosition;
        float dist   = toBall.Length();

        if (dist < KICK_RADIUS && _kickCooldown <= 0f)
        {
            _Kick(ball);
            _kickCooldown = 0.4f + (float)GD.RandRange(0.0, 0.2);
            Velocity = Vector2.Zero;
        }
        else
        {
            var target = dist < CHASE_RADIUS ? ball : BasePos;
            var delta2 = target - GlobalPosition;
            Velocity   = delta2.LengthSquared() > 9f ? delta2.Normalized() * _speed : Vector2.Zero;
            MoveAndSlide();
        }
    }

    private void _Kick(Vector2 ballPos)
    {
        float goalX  = IsBlueTeam ? 520f : -520f;
        float spread = (float)GD.RandRange(-35.0, 35.0);
        var   dir    = (new Vector2(goalX, spread) - ballPos).Normalized();
        Football.Instance?.Kick(dir * KICK_FORCE);
    }

    private void _BuildVisual()
    {
        Color kit = IsBlueTeam ? new Color(0.20f, 0.50f, 0.92f) : new Color(0.88f, 0.18f, 0.18f);

        var body = new Polygon2D { Color = kit, ZIndex = 0 };
        body.Polygon = _Circle(9f, 16);
        AddChild(body);

        var inner = new Polygon2D { Color = Colors.White, ZIndex = 1 };
        inner.Polygon = _Circle(4f, 10);
        AddChild(inner);

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
