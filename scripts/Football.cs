using Godot;

public partial class Football : RigidBody2D
{
    public static Football? Instance { get; private set; }

    private const float RADIUS   = 10f;
    private const float GRAVITY  = 400f; // aerial fall rate px/s²

    // Last team to touch: 0=blue, 1=red, -1=none
    public int   LastTouchedTeam { get; set; } = -1;
    public bool  Frozen          { get; set; } = false;

    // Aerial: height in logical px above ground. 0 = rolling.
    public float BallHeight { get; private set; } = 0f;
    public bool  IsAerial   => BallHeight > 0.5f;

    // Vertical velocity for aerial simulation
    private float _vz    = 0f;

    // Lateral spin: applied as extra force each frame for curve effect
    public float Spin { get; set; } = 0f;

    // Visual nodes (set in _BuildVisual, updated in _Process)
    private Node2D?  _ballVis;
    private Polygon2D? _shadow;

    public override void _Ready()
    {
        Instance    = this;
        GravityScale = 0;
        LinearDamp   = 1.6f;
        AngularDamp  = 4f;
        PhysicsMaterialOverride = new PhysicsMaterial { Bounce = 0.55f, Friction = 0.18f };
        _BuildVisual();
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (Frozen)
        {
            LinearVelocity  = Vector2.Zero;
            AngularVelocity = 0f;
        }

        // Aerial physics
        if (IsAerial || _vz > 0f)
        {
            _vz -= GRAVITY * dt;
            BallHeight = Mathf.Max(0f, BallHeight + _vz * dt);
        }
        else
        {
            BallHeight = 0f;
            _vz        = 0f;
        }

        // Spin curve (perpendicular force)
        if (Mathf.Abs(Spin) > 0.5f && LinearVelocity.LengthSquared() > 100f)
        {
            var perp = new Vector2(-LinearVelocity.Y, LinearVelocity.X).Normalized();
            ApplyCentralForce(perp * Spin);
            Spin = Mathf.MoveToward(Spin, 0f, Mathf.Abs(Spin) * dt * 2f);
        }

        // Offset ball visual upward by BallHeight; shadow stays at (0,0)
        if (_ballVis != null) _ballVis.Position = new Vector2(0, -BallHeight);
    }

    /// <summary>Kick the ball along the pitch plane.</summary>
    public void Kick(Vector2 impulse, int team = -1, float spin = 0f)
    {
        if (team != -1) LastTouchedTeam = team;
        Spin = spin;
        ApplyCentralImpulse(impulse);
    }

    /// <summary>Kick with an aerial component (for corners / clearances).</summary>
    public void AerialKick(Vector2 impulse, float verticalSpeed, int team = -1)
    {
        if (team != -1) LastTouchedTeam = team;
        _vz = verticalSpeed;
        ApplyCentralImpulse(impulse);
    }

    public void ResetTo(Vector2 pos)
    {
        GlobalPosition  = pos;
        LinearVelocity  = Vector2.Zero;
        AngularVelocity = 0f;
        BallHeight      = 0f;
        _vz             = 0f;
        Spin            = 0f;
        Frozen          = false;
        LastTouchedTeam = -1;
    }

    private void _BuildVisual()
    {
        // Shadow (stays at ground level)
        _shadow = new Polygon2D
        {
            Color  = new Color(0f, 0f, 0f, 0.3f),
            ZIndex = -1
        };
        _shadow.Polygon = _Circle(RADIUS * 0.85f, 16);
        AddChild(_shadow);

        // Ball body (will be offset upward when aerial)
        _ballVis = new Node2D { ZIndex = 0 };
        AddChild(_ballVis);

        var white = new Polygon2D { Color = new Color(0.94f, 0.94f, 0.91f), ZIndex = 0 };
        white.Polygon = _Circle(RADIUS, 20);
        _ballVis.AddChild(white);

        // Black pentagons
        foreach (float a in new float[] { 0f, Mathf.Tau*0.2f, Mathf.Tau*0.4f,
                                          Mathf.Tau*0.6f, Mathf.Tau*0.8f })
        {
            var patch = new Polygon2D { Color = new Color(0.08f, 0.08f, 0.08f), ZIndex = 1 };
            patch.Polygon = _Circle(3f, 6, new Vector2(Mathf.Cos(a)*5.5f, Mathf.Sin(a)*5.5f));
            _ballVis.AddChild(patch);
        }

        // Specular
        var hl = new Polygon2D { Color = new Color(1f, 1f, 1f, 0.55f), ZIndex = 2 };
        hl.Polygon = _Circle(3f, 8, new Vector2(-3.5f, -3.5f));
        _ballVis.AddChild(hl);

        var col = new CollisionShape2D();
        col.Shape = new CircleShape2D { Radius = RADIUS };
        AddChild(col);
    }

    private static Vector2[] _Circle(float r, int seg, Vector2 offset = default)
    {
        var pts = new Vector2[seg];
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.Tau;
            pts[i] = offset + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        return pts;
    }
}
