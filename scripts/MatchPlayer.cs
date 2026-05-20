using Godot;

public partial class MatchPlayer : CharacterBody2D
{
    private const float SPEED         = 225f;
    private const float SPRINT_SPEED  = 345f;
    private const float KICK_RADIUS   = 36f;
    private const float KICK_FORCE    = 730f;
    private const float HEADER_FORCE  = 380f;
    private const float PUSH_FORCE    = 210f;
    private const float SLIDE_SPEED   = 500f;
    private const float SLIDE_KICK    = 480f;
    private const float SLIDE_DIST    = 35f;

    // Stamina
    private float _stamina      = 100f;
    private const float STA_DRAIN_SPRINT = 25f;
    private const float STA_DRAIN_RUN    = 5f;
    private const float STA_RECOVER      = 8f;

    // Slide
    private float   _slideCooldown;
    private float   _slideTimer;
    private Vector2 _slideDir;
    private const float SLIDE_DURATION = 0.5f;
    private const float SLIDE_COOLDOWN = 1.2f;

    private float   _kickCooldown;
    private Vector2 _facing = Vector2.Right;

    // HUD bar
    private ColorRect? _staminaBar;

    // MatchManager gate: frozen by set pieces / kickoff
    public bool InputFrozen { get; set; } = false;

    public override void _Ready()
    {
        CollisionLayer = 1u;
        CollisionMask  = 2u | 16u;
        _BuildVisual();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _kickCooldown   -= dt;
        _slideCooldown  -= dt;

        if (InputFrozen)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        // Sliding takes over movement
        if (_slideTimer > 0f)
        {
            _slideTimer -= dt;
            Velocity = _slideDir * SLIDE_SPEED;
            _TrySlideTackle();
            MoveAndSlide();
            return;
        }

        var input  = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        bool sprint = Input.IsActionPressed("sprint") && _stamina > 1f;

        float spd;
        if (input.LengthSquared() > 0.01f)
        {
            _facing = input.Normalized();
            if (sprint)
            {
                float factor = _stamina < 10f ? 0.6f : _stamina < 30f ? 0.8f : 1.0f;
                spd = SPRINT_SPEED * factor;
                _stamina = Mathf.Max(0f, _stamina - STA_DRAIN_SPRINT * dt);
            }
            else
            {
                float factor = _stamina < 10f ? 0.6f : _stamina < 30f ? 0.8f : 1.0f;
                spd = SPEED * factor;
                _stamina = Mathf.Max(0f, _stamina - STA_DRAIN_RUN * dt);
            }
        }
        else
        {
            spd = 0f;
            _stamina = Mathf.Min(100f, _stamina + STA_RECOVER * dt);
        }

        Velocity = input * spd;
        MoveAndSlide();

        _PassivePush();
        _UpdateStaminaBar();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (InputFrozen) return;

        if (e is InputEventKey k && k.Pressed && !k.Echo)
        {
            if (k.Keycode == Key.Space)
                _TryKickOrHeader();
            else if (k.Keycode == Key.Ctrl)
                _TrySlide();
        }
    }

    // ── Passive push (dribble) ──────────────────────────────────────────────────

    private void _PassivePush()
    {
        if (Football.Instance == null || _kickCooldown > 0f) return;
        var d = Football.Instance.GlobalPosition - GlobalPosition;
        if (d.Length() < KICK_RADIUS * 0.5f)
        {
            Football.Instance.Kick(d.Normalized() * PUSH_FORCE, 0);
            _kickCooldown = 0.12f;
        }
    }

    // ── Kick or Header ──────────────────────────────────────────────────────────

    private void _TryKickOrHeader()
    {
        if (Football.Instance == null) return;
        var d = Football.Instance.GlobalPosition - GlobalPosition;
        if (d.Length() > KICK_RADIUS) return;

        // Header if ball is aerial and close enough
        if (Football.Instance.IsAerial && d.Length() < 30f)
        {
            Football.Instance.Kick(_facing * HEADER_FORCE, 0);
            _kickCooldown = 0.35f;
            return;
        }

        // Power kick with curve (if moving sideways relative to kick direction)
        var kickDir = Velocity.LengthSquared() > 100f ? Velocity.Normalized() : _facing;
        float cross = Velocity.X * kickDir.Y - Velocity.Y * kickDir.X;
        float spin  = cross * 0.15f;
        Football.Instance.Kick(kickDir * KICK_FORCE, 0, spin);
        _kickCooldown = 0.28f;
    }

    // ── Slide tackle ───────────────────────────────────────────────────────────

    private void _TrySlide()
    {
        if (_slideCooldown > 0f) return;
        _slideDir      = _facing;
        _slideTimer    = SLIDE_DURATION;
        _slideCooldown = SLIDE_COOLDOWN;
    }

    private void _TrySlideTackle()
    {
        if (Football.Instance == null || _kickCooldown > 0f) return;
        var d = Football.Instance.GlobalPosition - GlobalPosition;
        if (d.Length() < SLIDE_DIST)
        {
            Football.Instance.Kick(_slideDir * SLIDE_KICK, 0);
            _kickCooldown = 0.5f;
        }
    }

    // ── Stamina bar ────────────────────────────────────────────────────────────

    private void _UpdateStaminaBar()
    {
        if (_staminaBar == null) return;
        _staminaBar.Size = new Vector2(60f * (_stamina / 100f), 5f);
        var c = _stamina > 50f ? new Color(0.3f, 0.9f, 0.3f)
              : _stamina > 25f ? new Color(0.9f, 0.8f, 0.1f)
              :                  new Color(0.9f, 0.2f, 0.2f);
        _staminaBar.Color = c;
    }

    private void _BuildVisual()
    {
        var body = new Polygon2D { Color = new Color(0.12f, 0.38f, 0.92f), ZIndex = 0 };
        body.Polygon = _Circle(10f, 16);
        AddChild(body);

        var crest = new Polygon2D { Color = Colors.White, ZIndex = 1 };
        crest.Polygon = _Circle(4.5f, 6);
        AddChild(crest);

        // Yellow triangle indicator
        var arrow = new Polygon2D { Color = new Color(1f, 0.88f, 0.1f), ZIndex = 2 };
        arrow.Polygon = new Vector2[] { new(0, -16f), new(-5f, -10f), new(5f, -10f) };
        AddChild(arrow);

        // Stamina bar background
        var bgBar = new ColorRect
        {
            Color    = new Color(0.2f, 0.2f, 0.2f, 0.7f),
            Size     = new Vector2(60f, 5f),
            Position = new Vector2(-30f, -26f),
            ZIndex   = 3,
        };
        AddChild(bgBar);

        _staminaBar = new ColorRect
        {
            Color    = new Color(0.3f, 0.9f, 0.3f),
            Size     = new Vector2(60f, 5f),
            Position = new Vector2(-30f, -26f),
            ZIndex   = 4,
        };
        AddChild(_staminaBar);

        var col = new CollisionShape2D();
        col.Shape    = new CapsuleShape2D { Radius = 8f, Height = 14f };
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
