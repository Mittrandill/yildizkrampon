using Godot;

public partial class Football : RigidBody2D
{
    public static Football? Instance { get; private set; }
    private const float RADIUS = 10f;

    public override void _Ready()
    {
        Instance    = this;
        GravityScale = 0;
        LinearDamp   = 1.6f;
        AngularDamp  = 4f;
        PhysicsMaterialOverride = new PhysicsMaterial { Bounce = 0.5f, Friction = 0.2f };
        _BuildVisual();
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public void Kick(Vector2 impulse) => ApplyCentralImpulse(impulse);

    public void ResetTo(Vector2 pos)
    {
        GlobalPosition  = pos;
        LinearVelocity  = Vector2.Zero;
        AngularVelocity = 0f;
    }

    private void _BuildVisual()
    {
        // White ball
        var ball = new Polygon2D { Color = new Color(0.94f, 0.94f, 0.91f), ZIndex = 0 };
        ball.Polygon = _Ring(RADIUS, 20);
        AddChild(ball);

        // Black pentagons (simplified)
        foreach (float a in new float[] { 0f, Mathf.Tau*0.2f, Mathf.Tau*0.4f,
                                          Mathf.Tau*0.6f, Mathf.Tau*0.8f })
        {
            var patch = new Polygon2D { Color = new Color(0.08f, 0.08f, 0.08f), ZIndex = 1 };
            patch.Polygon = _Ring(3f, 6, new Vector2(Mathf.Cos(a)*5.5f, Mathf.Sin(a)*5.5f));
            AddChild(patch);
        }

        // Specular highlight
        var hl = new Polygon2D { Color = new Color(1f, 1f, 1f, 0.55f), ZIndex = 2 };
        hl.Polygon = _Ring(3f, 8, new Vector2(-3.5f, -3.5f));
        AddChild(hl);

        var col = new CollisionShape2D();
        col.Shape = new CircleShape2D { Radius = RADIUS };
        AddChild(col);
    }

    private static Vector2[] _Ring(float r, int seg, Vector2 center = default)
    {
        var pts = new Vector2[seg];
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.Tau;
            pts[i] = center + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        return pts;
    }
}
