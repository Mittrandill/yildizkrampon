using Godot;
using System.Collections.Generic;

public partial class FootballAI : CharacterBody2D
{
    public enum Role { GK, DEF_L, DEF_R, MID, FWD }

    [Export] public bool    IsBlueTeam  { get; set; } = false;
    [Export] public Vector2 BasePos     { get; set; } = Vector2.Zero;
    [Export] public Role    PlayerRole  { get; set; } = Role.MID;

    public bool AttacksRight { get; set; } = true;

    private const float SPEED       = 155f;
    private const float KICK_RADIUS = 38f;
    private const float KICK_FORCE  = 500f;
    private const float PASS_FORCE  = 420f;
    private const float GK_CATCH_DIST = 28f;
    // GK stays within this many px forward of goal line X
    private const float GK_MAX_FORWARD = 70f;

    private float _kickCooldown;
    private float _gkCatchTimer;
    private bool  _initialized;

    // Visual
    private Node2D    _visual   = null!;
    private Polygon2D _leftLeg  = null!;
    private Polygon2D _rightLeg = null!;
    private float     _legPhase;

    public override void _Ready()
    {
        CollisionLayer = 2u;
        CollisionMask  = 1u | 2u | 16u;
        _initialized   = true;
        _BuildVisual();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized) return;
        if (Football.Instance == null || MatchManager.Instance == null) return;
        if (MatchManager.Instance.IsSetPiece) { Velocity = Vector2.Zero; return; }

        float dt = (float)delta;
        _kickCooldown -= dt;

        switch (PlayerRole)
        {
            case Role.GK:    _BehaveGK(dt);  break;
            case Role.DEF_L:
            case Role.DEF_R: _BehaveDef(dt); break;
            case Role.MID:   _BehaveMid(dt); break;
            case Role.FWD:   _BehaveFwd(dt); break;
        }

        MoveAndSlide();
        _UpdateVisual(dt);
    }

    // ── Tactical helpers ────────────────────────────────────────────────────────

    public Vector2 TacticalBase()
    {
        float sign = AttacksRight ? 1f : -1f;
        return PlayerRole switch
        {
            Role.GK    => new Vector2(-450f * sign, 0f),
            Role.DEF_L => new Vector2(-270f * sign, -90f),
            Role.DEF_R => new Vector2(-270f * sign,  90f),
            Role.MID   => new Vector2(-100f * sign, BasePos.Y > 0f ? 65f : -65f),
            Role.FWD   => new Vector2( 120f * sign, 0f),
            _          => BasePos,
        };
    }

    private Vector2 _ShiftedTactical()
    {
        var   ball  = Football.Instance!.GlobalPosition;
        float bx    = Mathf.Clamp(ball.X, -480f, 480f);
        float shift = bx * 0.18f;
        return TacticalBase() + new Vector2(shift, 0f);
    }

    // ── GK ──────────────────────────────────────────────────────────────────────

    private void _BehaveGK(float dt)
    {
        if (Football.Instance == null) return;

        // Hold ball after catch
        if (_gkCatchTimer > 0f)
        {
            _gkCatchTimer -= dt;
            Football.Instance.Frozen = true;
            Football.Instance.GlobalPosition = GlobalPosition + new Vector2(AttacksRight ? -16f : 16f, 0f);
            Velocity = Vector2.Zero;
            if (_gkCatchTimer <= 0f)
            {
                Football.Instance.Frozen = false;
                _GKKickUpfield();
            }
            return;
        }

        var   ball   = Football.Instance.GlobalPosition;
        var   toBall = ball - GlobalPosition;
        float dist   = toBall.Length();

        // Catch if close enough and ball is on ground
        if (dist < GK_CATCH_DIST && _kickCooldown <= 0f && !Football.Instance.IsAerial)
        {
            _gkCatchTimer = 1.0f;
            return;
        }

        // Goal line X for this GK – never leave this X beyond GK_MAX_FORWARD
        float lineX = AttacksRight ? -450f : 450f;

        // When ball is threatening the goal, slide laterally (stay on/near line)
        bool ballApproaching = _BallApproachingOurGoal();
        if (ballApproaching && dist < 180f)
        {
            // Clamp forward movement: GK can step at most GK_MAX_FORWARD px from goal
            float forwardClamp = AttacksRight
                ? Mathf.Clamp(ball.X, lineX, lineX + GK_MAX_FORWARD)
                : Mathf.Clamp(ball.X, lineX - GK_MAX_FORWARD, lineX);
            var target = new Vector2(forwardClamp, Mathf.Clamp(ball.Y, -55f, 55f));
            _MoveToward(target, SPEED * 1.6f);
            return;
        }

        // Default: hug goal line at ball Y
        float clampedY = Mathf.Clamp(ball.Y, -55f, 55f);
        _MoveToward(new Vector2(lineX, clampedY), SPEED * 0.9f);
    }

    private void _GKKickUpfield()
    {
        float targetX = AttacksRight ? 200f : -200f;
        float spread  = (float)GD.RandRange(-80.0, 80.0);
        var   dir     = (new Vector2(targetX, spread) - GlobalPosition).Normalized();
        Football.Instance?.Kick(dir * KICK_FORCE * 1.1f, IsBlueTeam ? 0 : 1);
        _kickCooldown = 0.5f;
    }

    private bool _BallApproachingOurGoal()
    {
        if (Football.Instance == null) return false;
        var vel = Football.Instance.LinearVelocity;
        return AttacksRight
            ? (vel.X < -60f && Football.Instance.GlobalPosition.X < 0f)
            : (vel.X >  60f && Football.Instance.GlobalPosition.X > 0f);
    }

    // ── DEF ─────────────────────────────────────────────────────────────────────

    private void _BehaveDef(float dt)
    {
        if (Football.Instance == null) return;
        var   ball   = Football.Instance.GlobalPosition;
        var   toBall = ball - GlobalPosition;
        float dist   = toBall.Length();

        if (dist < KICK_RADIUS && _kickCooldown <= 0f)
        {
            _ClearBall();
            return;
        }

        bool ballInOwnHalf = AttacksRight ? ball.X < 0f : ball.X > 0f;
        if (ballInOwnHalf && dist < 280f)
        {
            if (_IsPartnerDefPressingCloser())
                _MoveToward(_ShiftedTactical() + new Vector2(AttacksRight ? -80f : 80f, 0f), SPEED * 0.85f);
            else
                _MoveToward(ball, SPEED);
        }
        else
        {
            _MoveToward(_ShiftedTactical(), SPEED * 0.85f);
        }
    }

    private bool _IsPartnerDefPressingCloser()
    {
        if (Football.Instance == null) return false;
        string group  = IsBlueTeam ? "team_blue" : "team_red";
        float  myDist = (Football.Instance.GlobalPosition - GlobalPosition).Length();
        foreach (var node in GetTree().GetNodesInGroup(group))
        {
            if (node == this) continue;
            if (node is FootballAI other && other.PlayerRole is Role.DEF_L or Role.DEF_R)
            {
                float od = (Football.Instance.GlobalPosition - other.GlobalPosition).Length();
                if (od < myDist - 30f) return true;
            }
        }
        return false;
    }

    private void _ClearBall()
    {
        if (Football.Instance == null) return;
        float targetX = AttacksRight ? 300f : -300f;
        float spread  = (float)GD.RandRange(-60.0, 60.0);
        var   dir     = (new Vector2(targetX, spread) - Football.Instance.GlobalPosition).Normalized();
        Football.Instance.Kick(dir * KICK_FORCE, IsBlueTeam ? 0 : 1);
        _kickCooldown = 0.5f;
        Velocity = Vector2.Zero;
    }

    // ── MID ─────────────────────────────────────────────────────────────────────

    private void _BehaveMid(float dt)
    {
        if (Football.Instance == null) return;
        var   ball = Football.Instance.GlobalPosition;
        float dist = (ball - GlobalPosition).Length();

        if (dist < KICK_RADIUS && _kickCooldown <= 0f)
        {
            _SmartKick();
            return;
        }

        bool inMidfield = Mathf.Abs(ball.X) < 200f;
        if (dist < 260f || inMidfield)
            _MoveToward(ball, SPEED);
        else
            _MoveToward(_ShiftedTactical(), SPEED * 0.9f);
    }

    // ── FWD ─────────────────────────────────────────────────────────────────────

    private void _BehaveFwd(float dt)
    {
        if (Football.Instance == null) return;
        var   ball = Football.Instance.GlobalPosition;
        float dist = (ball - GlobalPosition).Length();

        if (dist < KICK_RADIUS && _kickCooldown <= 0f)
        {
            _SmartKick();
            return;
        }

        bool ballInAttackHalf = AttacksRight ? ball.X > 0f : ball.X < 0f;
        if (ballInAttackHalf)
        {
            float runX = AttacksRight
                ? Mathf.Min(ball.X + 120f, 420f)
                : Mathf.Max(ball.X - 120f, -420f);
            var runTarget = new Vector2(runX, Mathf.Lerp(GlobalPosition.Y, ball.Y * 0.4f, 0.15f));
            _MoveToward(dist < 300f ? ball : runTarget, SPEED * (dist < 300f ? 1f : 0.9f));
        }
        else
        {
            _MoveToward(ball, SPEED);
        }
    }

    // ── Smart kick ──────────────────────────────────────────────────────────────

    private void _SmartKick()
    {
        if (Football.Instance == null) return;
        int team = IsBlueTeam ? 0 : 1;

        var passTarget = _FindPassTarget();
        if (passTarget != null)
        {
            var dir = (passTarget.GlobalPosition - Football.Instance.GlobalPosition).Normalized();
            Football.Instance.Kick(dir * PASS_FORCE, team);
            _kickCooldown = 0.6f;
        }
        else
        {
            float goalX  = AttacksRight ? 506f : -506f;
            float spread = (float)GD.RandRange(-30.0, 30.0);
            var   dir    = (new Vector2(goalX, spread) - Football.Instance.GlobalPosition).Normalized();
            Football.Instance.Kick(dir * KICK_FORCE, team);
            _kickCooldown = 0.45f;
        }
        Velocity = Vector2.Zero;
    }

    private FootballAI? _FindPassTarget()
    {
        if (Football.Instance == null) return null;
        string group       = IsBlueTeam ? "team_blue" : "team_red";
        float  goalX       = AttacksRight ? 506f : -506f;
        float  myDistGoal  = Mathf.Abs(Football.Instance.GlobalPosition.X - goalX);
        FootballAI? best   = null;
        float  bestDist    = myDistGoal - 80f;

        foreach (var node in GetTree().GetNodesInGroup(group))
        {
            if (node == this) continue;
            if (node is not FootballAI ai) continue;
            float d = Mathf.Abs(ai.GlobalPosition.X - goalX);
            if (d < bestDist && _PassLaneClear(ai.GlobalPosition))
            {
                bestDist = d;
                best     = ai;
            }
        }
        return best;
    }

    private bool _PassLaneClear(Vector2 targetPos)
    {
        if (Football.Instance == null) return false;
        string oppGroup = IsBlueTeam ? "team_red" : "team_blue";
        var    from     = Football.Instance.GlobalPosition;
        var    dir      = targetPos - from;
        float  len      = dir.Length();
        if (len < 0.01f) return true;
        var dn = dir / len;

        foreach (var node in GetTree().GetNodesInGroup(oppGroup))
        {
            if (node is not CharacterBody2D opp) continue;
            var   toOpp = opp.GlobalPosition - from;
            float proj  = toOpp.Dot(dn);
            if (proj < 0f || proj > len) continue;
            if (Mathf.Abs(toOpp.X * dn.Y - toOpp.Y * dn.X) < 60f) return false;
        }
        return true;
    }

    // ── Movement ────────────────────────────────────────────────────────────────

    private void _MoveToward(Vector2 target, float speed)
    {
        var delta = target - GlobalPosition;
        Velocity = delta.LengthSquared() > 16f ? delta.Normalized() * speed : Vector2.Zero;
    }

    // ── Visual update ────────────────────────────────────────────────────────────

    private void _UpdateVisual(float dt)
    {
        float speed = Velocity.Length();
        if (speed > 20f)
        {
            float targetAngle = Velocity.Angle() + Mathf.Pi * 0.5f;
            _visual.Rotation = Mathf.LerpAngle(_visual.Rotation, targetAngle, 0.22f);
        }
        if (speed > 10f)
        {
            _legPhase += dt * speed * 0.045f;
            float amp = Mathf.Min(speed / SPEED, 1f) * 3.5f;
            _leftLeg.Position  = new Vector2(-5f, 8f + Mathf.Sin(_legPhase) * amp);
            _rightLeg.Position = new Vector2( 5f, 8f + Mathf.Sin(_legPhase + Mathf.Pi) * amp);
        }
    }

    // ── Visual build ─────────────────────────────────────────────────────────────

    private void _BuildVisual()
    {
        Color kit      = IsBlueTeam ? new Color(0.12f, 0.38f, 0.92f) : new Color(0.88f, 0.18f, 0.18f);
        Color skin     = new Color(0.87f, 0.70f, 0.55f);
        Color shorts   = new Color(0.9f,  0.9f,  0.9f);
        Color hair     = new Color(0.22f, 0.14f, 0.04f);

        // Ground shadow (doesn't rotate)
        var shadow = new Polygon2D { Color = new Color(0f, 0f, 0f, 0.18f), ZIndex = -1 };
        shadow.Polygon = _Ellipse(12f, 5f, 14);
        shadow.Position = new Vector2(0f, 4f);
        AddChild(shadow);

        // Rotating group
        _visual = new Node2D();
        AddChild(_visual);

        // Legs (skin colour circles)
        _leftLeg = new Polygon2D { Color = skin, ZIndex = 0 };
        _leftLeg.Polygon = _Circle(4.2f, 8);
        _leftLeg.Position = new Vector2(-5f, 8f);
        _visual.AddChild(_leftLeg);

        _rightLeg = new Polygon2D { Color = skin, ZIndex = 0 };
        _rightLeg.Polygon = _Circle(4.2f, 8);
        _rightLeg.Position = new Vector2(5f, 8f);
        _visual.AddChild(_rightLeg);

        // Shorts
        var shortsP = new Polygon2D { Color = shorts, ZIndex = 1 };
        shortsP.Polygon = _Ellipse(7f, 4.5f, 10);
        shortsP.Position = new Vector2(0f, 3f);
        _visual.AddChild(shortsP);

        // Shirt / body
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
        head.Polygon = _Circle(6f, 12);
        head.Position = new Vector2(0f, -11f);
        _visual.AddChild(head);

        // Hair (top half of head)
        var hairPts = new List<Vector2>();
        for (int i = 0; i <= 6; i++)
        {
            float a = i / 6f * Mathf.Pi + Mathf.Pi;
            hairPts.Add(new Vector2(0f, -11f) + new Vector2(Mathf.Cos(a) * 6f, Mathf.Sin(a) * 6f));
        }
        var hairP = new Polygon2D { Color = hair, ZIndex = 5 };
        hairP.Polygon = hairPts.ToArray();
        _visual.AddChild(hairP);

        // Role letter
        var lbl = new Label
        {
            Text     = PlayerRole.ToString().Split('_')[0][0].ToString(),
            ZIndex   = 6,
            Position = new Vector2(-4f, -7f),
        };
        lbl.AddThemeFontSizeOverride("font_size", 7);
        lbl.AddThemeColorOverride("font_color", Colors.White);
        AddChild(lbl);

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
