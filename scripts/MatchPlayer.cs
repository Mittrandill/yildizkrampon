using Godot;
using System.Collections.Generic;

public partial class MatchPlayer : CharacterBody2D
{
    private const float SPEED        = 170f;
    private const float SPRINT_SPEED = 260f;
    private const float KICK_RADIUS  = 36f;
    private const float KICK_FORCE   = 560f;
    private const float HEADER_FORCE = 350f;
    private const float PUSH_FORCE   = 170f;
    private const float SLIDE_SPEED  = 420f;
    private const float SLIDE_KICK   = 400f;
    private const float SLIDE_DIST   = 35f;

    // Stamina
    private float _stamina      = 100f;
    private const float STA_DRAIN_SPRINT = 25f;
    private const float STA_DRAIN_RUN    = 4f;
    private const float STA_RECOVER      = 9f;

    // Slide
    private float   _slideCooldown;
    private float   _slideTimer;
    private Vector2 _slideDir;
    private const float SLIDE_DURATION = 0.45f;
    private const float SLIDE_COOLDOWN = 1.2f;

    private float   _kickCooldown;
    private Vector2 _facing = Vector2.Down;

    // Visual
    private Node2D     _visual    = null!;
    private Polygon2D  _leftLeg   = null!;
    private Polygon2D  _rightLeg  = null!;
    private ColorRect? _staminaBar;
    private float      _legPhase;

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
        _kickCooldown  -= dt;
        _slideCooldown -= dt;

        if (InputFrozen)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        if (_slideTimer > 0f)
        {
            _slideTimer -= dt;
            Velocity = _slideDir * SLIDE_SPEED;
            _TrySlideTackle();
            MoveAndSlide();
            _UpdateVisual(dt);
            return;
        }

        var   input  = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        bool  sprint = Input.IsActionPressed("sprint") && _stamina > 1f;
        float spd;

        if (input.LengthSquared() > 0.01f)
        {
            _facing = input.Normalized();
            float factor = _stamina < 10f ? 0.6f : _stamina < 30f ? 0.8f : 1.0f;
            if (sprint)
            {
                spd      = SPRINT_SPEED * factor;
                _stamina = Mathf.Max(0f, _stamina - STA_DRAIN_SPRINT * dt);
            }
            else
            {
                spd      = SPEED * factor;
                _stamina = Mathf.Max(0f, _stamina - STA_DRAIN_RUN * dt);
            }
        }
        else
        {
            spd      = 0f;
            _stamina = Mathf.Min(100f, _stamina + STA_RECOVER * dt);
        }

        Velocity = input * spd;
        MoveAndSlide();
        _PassivePush();
        _UpdateVisual(dt);
        _UpdateStaminaBar();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (InputFrozen) return;

        if (e is InputEventKey k && k.Pressed && !k.Echo)
        {
            if (k.Keycode == Key.Space)   _TryKickOrHeader();
            else if (k.Keycode == Key.Ctrl) _TrySlide();
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

        if (Football.Instance.IsAerial && d.Length() < 30f)
        {
            Football.Instance.Kick(_facing * HEADER_FORCE, 0);
            _kickCooldown = 0.35f;
            return;
        }

        var   kickDir = Velocity.LengthSquared() > 100f ? Velocity.Normalized() : _facing;
        float cross   = Velocity.X * kickDir.Y - Velocity.Y * kickDir.X;
        float spin    = cross * 0.12f;
        Football.Instance.Kick(kickDir * KICK_FORCE, 0, spin);
        _kickCooldown = 0.28f;
    }

    // ── Slide tackle ────────────────────────────────────────────────────────────

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

    // ── Visual update ────────────────────────────────────────────────────────────

    private void _UpdateVisual(float dt)
    {
        float speed = Velocity.Length();
        if (speed > 20f)
        {
            float targetAngle = Velocity.Angle() + Mathf.Pi * 0.5f;
            _visual.Rotation = Mathf.LerpAngle(_visual.Rotation, targetAngle, 0.25f);
        }
        if (speed > 10f)
        {
            _legPhase += dt * speed * 0.045f;
            float amp = Mathf.Min(speed / SPEED, 1f) * 4f;
            _leftLeg.Position  = new Vector2(-5f, 8f + Mathf.Sin(_legPhase) * amp);
            _rightLeg.Position = new Vector2( 5f, 8f + Mathf.Sin(_legPhase + Mathf.Pi) * amp);
        }
    }

    // ── Stamina bar ─────────────────────────────────────────────────────────────

    private void _UpdateStaminaBar()
    {
        if (_staminaBar == null) return;
        _staminaBar.Size = new Vector2(40f * (_stamina / 100f), 5f);
        var c = _stamina > 50f ? new Color(0.2f, 0.9f, 0.2f)
              : _stamina > 25f ? new Color(0.9f, 0.8f, 0.1f)
              :                  new Color(0.9f, 0.2f, 0.2f);
        _staminaBar.Color = c;
    }

    // ── Visual build ─────────────────────────────────────────────────────────────

    private void _BuildVisual()
    {
        Color skin   = new Color(0.87f, 0.70f, 0.55f);
        Color kit    = new Color(0.12f, 0.38f, 0.92f);
        Color shorts = new Color(0.9f,  0.9f,  0.9f);
        Color hair   = new Color(0.22f, 0.14f, 0.04f);

        // Selection ring (yellow, non-rotating, below player)
        var ring = new Polygon2D { Color = new Color(1f, 0.92f, 0.1f, 0.30f), ZIndex = -2 };
        ring.Polygon = _Circle(16f, 20);
        AddChild(ring);

        // Ground shadow (non-rotating)
        var shadow = new Polygon2D { Color = new Color(0f, 0f, 0f, 0.2f), ZIndex = -1 };
        shadow.Polygon = _Ellipse(12f, 5f, 14);
        shadow.Position = new Vector2(0f, 4f);
        AddChild(shadow);

        // Rotating visual group
        _visual = new Node2D();
        AddChild(_visual);

        // Legs
        _leftLeg = new Polygon2D { Color = skin, ZIndex = 0 };
        _leftLeg.Polygon = _Circle(4.5f, 8);
        _leftLeg.Position = new Vector2(-5f, 8f);
        _visual.AddChild(_leftLeg);

        _rightLeg = new Polygon2D { Color = skin, ZIndex = 0 };
        _rightLeg.Polygon = _Circle(4.5f, 8);
        _rightLeg.Position = new Vector2(5f, 8f);
        _visual.AddChild(_rightLeg);

        // Shorts
        var shortsP = new Polygon2D { Color = shorts, ZIndex = 1 };
        shortsP.Polygon = _Ellipse(7.5f, 4.5f, 10);
        shortsP.Position = new Vector2(0f, 3f);
        _visual.AddChild(shortsP);

        // Shirt (blue)
        var body = new Polygon2D { Color = kit, ZIndex = 2 };
        body.Polygon = _Ellipse(9f, 7f, 14);
        body.Position = new Vector2(0f, -2f);
        _visual.AddChild(body);

        // Shirt crest
        var crest = new Polygon2D { Color = Colors.White, ZIndex = 3 };
        crest.Polygon = _Circle(2.5f, 6);
        crest.Position = new Vector2(0f, -2f);
        _visual.AddChild(crest);

        // Head
        var head = new Polygon2D { Color = skin, ZIndex = 4 };
        head.Polygon = _Circle(6.5f, 12);
        head.Position = new Vector2(0f, -11f);
        _visual.AddChild(head);

        // Hair (upper half of head)
        var hairList = new List<Vector2>();
        for (int i = 0; i <= 7; i++)
        {
            float a = i / 7f * Mathf.Pi + Mathf.Pi;
            hairList.Add(new Vector2(0f, -11f) + new Vector2(Mathf.Cos(a) * 6.5f, Mathf.Sin(a) * 6.5f));
        }
        var hairP = new Polygon2D { Color = hair, ZIndex = 5 };
        hairP.Polygon = hairList.ToArray();
        _visual.AddChild(hairP);

        // Stamina bar background (non-rotating, above player)
        var bgBar = new ColorRect
        {
            Color    = new Color(0.15f, 0.15f, 0.15f, 0.75f),
            Size     = new Vector2(40f, 5f),
            Position = new Vector2(-20f, -34f),
            ZIndex   = 10,
        };
        AddChild(bgBar);

        _staminaBar = new ColorRect
        {
            Color    = new Color(0.2f, 0.9f, 0.2f),
            Size     = new Vector2(40f, 5f),
            Position = new Vector2(-20f, -34f),
            ZIndex   = 11,
        };
        AddChild(_staminaBar);

        // Collision
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

    private static Vector2[] _Ellipse(float rx, float ry, int seg)
    {
        var pts = new Vector2[seg];
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.Tau;
            pts[i] = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
        }
        return pts;
    }
}
