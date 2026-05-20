using Godot;

public partial class MatchBall : Node2D
{
    public const float Radius = 6f;

    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public float VerticalVelocity { get; set; }
    public float Height { get; set; }
    public float Friction { get; set; } = 390f;
    public float Gravity { get; set; } = 760f;
    public Rect2 Bounds { get; set; } = new(new Vector2(96, 80), new Vector2(848, 456));
    public MatchActor? Holder { get; private set; }
    public Vector2 HoldOffset { get; set; } = Vector2.Zero;
    public float FreeAfterKick { get; set; }
    public int LastTouchTeam { get; private set; } = -1;
    public MatchActor? LastTouchActor { get; private set; }

    private Sprite2D? _shadow;
    private Sprite2D? _visual;
    private Line2D? _trail;

    public override void _Ready()
    {
        _shadow = new Sprite2D
        {
            Texture = MakeOvalTexture(18, 7, new Color(0.02f, 0.03f, 0.02f, 0.34f)),
            Position = new Vector2(0f, Radius - 1f),
            ZIndex = -1
        };
        AddChild(_shadow);

        _visual = new Sprite2D
        {
            Texture = MakeBallTexture(),
            Position = Vector2.Zero
        };
        AddChild(_visual);

        _trail = new Line2D
        {
            Width = 2f,
            DefaultColor = new Color(1f, 1f, 1f, 0.18f),
            ZIndex = -1
        };
        AddChild(_trail);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        FreeAfterKick = Mathf.Max(FreeAfterKick - dt, 0f);

        if (Holder != null)
        {
            Vector2 heldPosition = Holder.Position + HoldOffset;
            Position = Position.DistanceTo(heldPosition) > 54f
                ? heldPosition
                : Position.Lerp(heldPosition, 0.20f);
            Velocity = (heldPosition - Position) * 6f;
            Height = 1.5f;
            VerticalVelocity = 0f;
            UpdateVisualHeight();
            UpdateTrail();
            return;
        }

        Position += Velocity * dt;
        UpdateHeight(dt);
        float speed = Velocity.Length();
        if (speed > 0f)
        {
            float newSpeed = Mathf.Max(speed - Friction * dt, 0f);
            Velocity = speed > 0.01f ? Velocity.Normalized() * newSpeed : Vector2.Zero;
        }
        UpdateVisualHeight();
        UpdateTrail();
    }

    public void Kick(Vector2 direction, float power, MatchActor? actor = null, int team = -1, float liftRatio = 0.18f)
    {
        if (direction.LengthSquared() <= 0.01f)
            return;

        Holder = null;
        LastTouchActor = actor;
        LastTouchTeam = team;
        FreeAfterKick = 0.12f;
        Velocity = direction.Normalized() * power * 1.04f;
        Height = 4f;
        VerticalVelocity = Mathf.Clamp(power * liftRatio, 16f, 132f);
    }

    public void Roll(Vector2 direction, float power, MatchActor actor, int team)
    {
        if (direction.LengthSquared() <= 0.01f)
            return;

        Holder = null;
        LastTouchActor = actor;
        LastTouchTeam = team;
        FreeAfterKick = 0.14f;
        Velocity = direction.Normalized() * power;
        Height = 0f;
        VerticalVelocity = 0f;
    }

    public bool Claim(MatchActor actor, Vector2 attackDirection, bool force = false)
    {
        if (!force && (Holder != null || FreeAfterKick > 0f || IsAirborne()))
            return false;

        Holder = actor;
        LastTouchActor = actor;
        LastTouchTeam = actor.Team;
        Vector2 direction = attackDirection.LengthSquared() > 0.01f ? attackDirection.Normalized() : Vector2.Right;
        HoldOffset = direction * 23f;
        Velocity = Vector2.Zero;
        Height = 1.5f;
        VerticalVelocity = 0f;
        return true;
    }

    public void DribbleNudge(Vector2 direction, float amount)
    {
        if (Holder == null || direction.LengthSquared() <= 0.01f)
            return;

        Vector2 nudge = direction.Normalized() * amount;
        Position += nudge;
        Velocity = nudge * 12f;
        Height = 1f;
        VerticalVelocity = 0f;
    }

    public void Deflect(Vector2 direction, float power, MatchActor actor, float liftRatio = 0.08f)
    {
        Kick(direction, power, actor, actor.Team, liftRatio);
        FreeAfterKick = 0.04f;
    }

    public void Release()
    {
        Holder = null;
    }

    public bool IsAirborne() => Height > 14f;

    public void ResetToCenter()
    {
        Holder = null;
        LastTouchActor = null;
        LastTouchTeam = -1;
        FreeAfterKick = 0f;
        Position = Bounds.GetCenter();
        Velocity = Vector2.Zero;
        Height = 0f;
        VerticalVelocity = 0f;
        UpdateVisualHeight();
    }

    private void UpdateTrail()
    {
        if (_trail == null)
            return;

        _trail.ClearPoints();
        if (Velocity.Length() < 150f || Holder != null)
            return;

        Vector2 back = -Velocity.Normalized() * Mathf.Clamp(Velocity.Length() * 0.08f, 16f, 48f);
        _trail.AddPoint(new Vector2(0f, -Height * 0.35f));
        _trail.AddPoint(back);
    }

    private void UpdateHeight(float dt)
    {
        if (Height <= 0f && VerticalVelocity <= 0f)
        {
            Height = 0f;
            return;
        }

        Height += VerticalVelocity * dt;
        VerticalVelocity -= Gravity * dt;
        if (Height < 0f)
        {
            Height = 0f;
            if (Velocity.Length() > 155f && Mathf.Abs(VerticalVelocity) > 80f)
            {
                VerticalVelocity = -VerticalVelocity * 0.32f;
                Velocity *= 0.92f;
            }
            else
            {
                VerticalVelocity = 0f;
            }
        }
    }

    private void UpdateVisualHeight()
    {
        if (_visual == null || _shadow == null)
            return;

        _visual.Position = new Vector2(0f, -Height);
        float shadowScale = Mathf.Clamp(1f - Height / 90f, 0.45f, 1f);
        _shadow.Scale = new Vector2(shadowScale, shadowScale);
        _shadow.Position = new Vector2(0f, Radius - 1f);
        _shadow.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(1f - Height / 145f, 0.32f, 1f));
    }

    private static Texture2D MakeBallTexture()
    {
        int size = 18;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x, y).DistanceTo(center);
                if (d > 8.1f)
                    continue;
                Color color = d > 7.2f ? new Color(0.18f, 0.18f, 0.16f, 1f) : new Color(0.96f, 0.93f, 0.82f, 1f);
                if ((Mathf.Abs(x - center.X) < 1.2f && y > 4 && y < 14) || (Mathf.Abs(y - center.Y) < 1.1f && x > 4 && x < 14))
                    color = new Color(0.22f, 0.22f, 0.20f, 1f);
                img.SetPixel(x, y, color);
            }
        }
        return ImageTexture.CreateFromImage(img);
    }

    private static Texture2D MakeOvalTexture(int width, int height, Color color)
    {
        var img = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        float cx = (width - 1) * 0.5f;
        float cy = (height - 1) * 0.5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x - cx) / cx;
                float ny = (y - cy) / cy;
                float d = nx * nx + ny * ny;
                if (d <= 1f)
                    img.SetPixel(x, y, new Color(color.R, color.G, color.B, color.A * (1f - d * 0.45f)));
            }
        }
        return ImageTexture.CreateFromImage(img);
    }
}
