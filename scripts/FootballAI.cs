using Godot;
using System.Collections.Generic;

public partial class FootballAI : CharacterBody2D
{
    public enum Role { GK, DEF_L, DEF_R, MID, FWD }

    [Export] public bool   IsBlueTeam      { get; set; } = false;
    [Export] public Vector2 BasePos        { get; set; } = Vector2.Zero;
    [Export] public Role    PlayerRole     { get; set; } = Role.MID;

    // Set by MatchManager each frame based on which side we attack
    public bool AttacksRight { get; set; } = true;

    private const float SPEED         = 190f;
    private const float KICK_RADIUS   = 38f;
    private const float KICK_FORCE    = 600f;
    private const float PASS_FORCE    = 460f;
    private const float GK_CATCH_DIST = 28f;

    private float _kickCooldown;
    private float _gkCatchTimer;   // >0 = holding ball
    private bool  _initialized;

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
            case Role.GK:  _BehaveGK(dt);  break;
            case Role.DEF_L:
            case Role.DEF_R: _BehaveDef(dt); break;
            case Role.MID:   _BehaveMid(dt); break;
            case Role.FWD:   _BehaveFwd(dt); break;
        }

        MoveAndSlide();
    }

    // ── Tactical helpers ────────────────────────────────────────────────────────

    // Returns the tactical base position for our role given current attack direction
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

    // Shifted tactical position that moves as a block toward/away from the ball
    private Vector2 _ShiftedTactical()
    {
        var ball  = Football.Instance!.GlobalPosition;
        float bx  = Mathf.Clamp(ball.X, -480f, 480f);
        float shift = bx * 0.18f;
        var tb = TacticalBase();
        return tb + new Vector2(shift, 0f);
    }

    // ── GK ─────────────────────────────────────────────────────────────────────

    private void _BehaveGK(float dt)
    {
        if (Football.Instance == null) return;

        // Hold ball after catch
        if (_gkCatchTimer > 0f)
        {
            _gkCatchTimer -= dt;
            Football.Instance.Frozen   = true;
            Football.Instance.GlobalPosition = GlobalPosition + new Vector2(AttacksRight ? -16f : 16f, 0f);
            Velocity = Vector2.Zero;
            if (_gkCatchTimer <= 0f)
            {
                Football.Instance.Frozen = false;
                _GKKickUpfield();
            }
            return;
        }

        var ball    = Football.Instance.GlobalPosition;
        var toBall  = ball - GlobalPosition;
        float dist  = toBall.Length();

        // Catch if close enough and ball is on ground
        bool ballGrounded = !Football.Instance.IsAerial;
        if (dist < GK_CATCH_DIST && _kickCooldown <= 0f && ballGrounded)
        {
            _gkCatchTimer = 1.0f;
            return;
        }

        // Dive toward incoming shots (quick burst)
        bool ballComingTowardGoal = _BallApproachingOurGoal();
        if (dist < 200f && ballComingTowardGoal)
        {
            Velocity = toBall.Normalized() * SPEED * 1.5f;
            return;
        }

        // Hug goal line at ball Y
        float lineX    = AttacksRight ? -450f : 450f;
        float clampedY = Mathf.Clamp(ball.Y, -55f, 55f);
        var   target   = new Vector2(lineX, clampedY);
        _MoveToward(target, SPEED * 0.9f);
    }

    private void _GKKickUpfield()
    {
        float targetX = AttacksRight ? 200f : -200f;
        float spread  = (float)GD.RandRange(-80.0, 80.0);
        var   dir     = (new Vector2(targetX, spread) - GlobalPosition).Normalized();
        int   team    = IsBlueTeam ? 0 : 1;
        Football.Instance?.Kick(dir * KICK_FORCE * 1.1f, team);
        _kickCooldown = 0.5f;
    }

    private bool _BallApproachingOurGoal()
    {
        if (Football.Instance == null) return false;
        var vel  = Football.Instance.LinearVelocity;
        float myGoalX = AttacksRight ? -480f : 480f;
        float ballX   = Football.Instance.GlobalPosition.X;
        // Ball moving toward our goal side
        return AttacksRight ? (vel.X < -60f && ballX < 0f) : (vel.X > 60f && ballX > 0f);
    }

    // ── DEF ────────────────────────────────────────────────────────────────────

    private void _BehaveDef(float dt)
    {
        if (Football.Instance == null) return;
        var ball    = Football.Instance.GlobalPosition;
        var toBall  = ball - GlobalPosition;
        float dist  = toBall.Length();

        // Kick if on ball
        if (dist < KICK_RADIUS && _kickCooldown <= 0f)
        {
            _ClearBall();
            return;
        }

        float myGoalX = AttacksRight ? -480f : 480f;
        bool  ballInOwnHalf = AttacksRight ? ball.X < 0f : ball.X > 0f;

        if (ballInOwnHalf && dist < 280f)
        {
            // Check if another DEF is pressing — if so, hold deeper
            bool partnerPressing = _IsPartnerDefPressingCloser();
            if (partnerPressing)
            {
                // Cover behind: retreat 80px toward goal
                var coverPos = _ShiftedTactical() + new Vector2(AttacksRight ? -80f : 80f, 0f);
                _MoveToward(coverPos, SPEED * 0.85f);
            }
            else
            {
                _MoveToward(ball, SPEED);
            }
        }
        else
        {
            _MoveToward(_ShiftedTactical(), SPEED * 0.85f);
        }
    }

    private bool _IsPartnerDefPressingCloser()
    {
        if (Football.Instance == null) return false;
        string group = IsBlueTeam ? "team_blue" : "team_red";
        float myDist = (Football.Instance.GlobalPosition - GlobalPosition).Length();
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
        int   team    = IsBlueTeam ? 0 : 1;
        Football.Instance.Kick(dir * KICK_FORCE, team);
        _kickCooldown = 0.5f;
        Velocity = Vector2.Zero;
    }

    // ── MID ────────────────────────────────────────────────────────────────────

    private void _BehaveMid(float dt)
    {
        if (Football.Instance == null) return;
        var ball   = Football.Instance.GlobalPosition;
        float dist = (ball - GlobalPosition).Length();

        if (dist < KICK_RADIUS && _kickCooldown <= 0f)
        {
            _SmartKick();
            return;
        }

        // Press in midfield or near ball
        bool inMidfield = Mathf.Abs(ball.X) < 200f;
        if (dist < 260f || inMidfield)
            _MoveToward(ball, SPEED);
        else
            _MoveToward(_ShiftedTactical(), SPEED * 0.9f);
    }

    // ── FWD ────────────────────────────────────────────────────────────────────

    private void _BehaveFwd(float dt)
    {
        if (Football.Instance == null) return;
        var ball   = Football.Instance.GlobalPosition;
        float dist = (ball - GlobalPosition).Length();

        if (dist < KICK_RADIUS && _kickCooldown <= 0f)
        {
            _SmartKick();
            return;
        }

        bool ballInAttackHalf = AttacksRight ? ball.X > 0f : ball.X < 0f;
        if (ballInAttackHalf)
        {
            // Make a run into space behind defense
            float runX = AttacksRight ? Mathf.Min(ball.X + 120f, 420f) : Mathf.Max(ball.X - 120f, -420f);
            var runTarget = new Vector2(runX, Mathf.Lerp(GlobalPosition.Y, ball.Y * 0.4f, 0.15f));
            if (dist < 300f)
                _MoveToward(ball, SPEED);
            else
                _MoveToward(runTarget, SPEED * 0.9f);
        }
        else
        {
            _MoveToward(ball, SPEED);
        }
    }

    // ── Smart kick: pass or shoot ───────────────────────────────────────────────

    private void _SmartKick()
    {
        if (Football.Instance == null) return;

        // Try passing if a teammate is in better position
        var passTarget = _FindPassTarget();
        if (passTarget != null)
        {
            var dir = (passTarget.GlobalPosition - Football.Instance.GlobalPosition).Normalized();
            int team = IsBlueTeam ? 0 : 1;
            Football.Instance.Kick(dir * PASS_FORCE, team);
            _kickCooldown = 0.6f;
        }
        else
        {
            // Shoot toward goal
            float goalX = AttacksRight ? 506f : -506f;
            float spread = (float)GD.RandRange(-30.0, 30.0);
            var dir = (new Vector2(goalX, spread) - Football.Instance.GlobalPosition).Normalized();
            int team = IsBlueTeam ? 0 : 1;
            Football.Instance.Kick(dir * KICK_FORCE, team);
            _kickCooldown = 0.45f;
        }
        Velocity = Vector2.Zero;
    }

    private FootballAI? _FindPassTarget()
    {
        if (Football.Instance == null) return null;
        string group    = IsBlueTeam ? "team_blue" : "team_red";
        float  goalX    = AttacksRight ? 506f : -506f;
        float  myDistToGoal = Mathf.Abs(Football.Instance.GlobalPosition.X - goalX);
        FootballAI? best = null;
        float  bestDist = myDistToGoal - 80f; // teammate must be significantly closer to goal

        foreach (var node in GetTree().GetNodesInGroup(group))
        {
            if (node == this) continue;
            if (node is not FootballAI ai) continue;
            float teammateDist = Mathf.Abs(ai.GlobalPosition.X - goalX);
            if (teammateDist < bestDist)
            {
                // Check not blocked (simplified: no opponent within 60px of line)
                if (_PassLaneClear(ai.GlobalPosition))
                {
                    bestDist = teammateDist;
                    best     = ai;
                }
            }
        }
        return best;
    }

    private bool _PassLaneClear(Vector2 targetPos)
    {
        if (Football.Instance == null) return false;
        string oppGroup = IsBlueTeam ? "team_red" : "team_blue";
        var    from     = Football.Instance.GlobalPosition;
        var    dir      = (targetPos - from);
        float  len      = dir.Length();
        if (len < 0.01f) return true;
        var dn = dir / len;

        foreach (var node in GetTree().GetNodesInGroup(oppGroup))
        {
            if (node is not CharacterBody2D opp) continue;
            var toOpp = opp.GlobalPosition - from;
            float proj = toOpp.Dot(dn);
            if (proj < 0f || proj > len) continue;
            float cross = Mathf.Abs(toOpp.X * dn.Y - toOpp.Y * dn.X);
            if (cross < 60f) return false;
        }
        return true;
    }

    // ── Movement helper ─────────────────────────────────────────────────────────

    private void _MoveToward(Vector2 target, float speed)
    {
        var delta = target - GlobalPosition;
        Velocity = delta.LengthSquared() > 16f ? delta.Normalized() * speed : Vector2.Zero;
    }

    // ── Visual ─────────────────────────────────────────────────────────────────

    private void _BuildVisual()
    {
        Color kit = IsBlueTeam ? new Color(0.20f, 0.50f, 0.92f) : new Color(0.88f, 0.18f, 0.18f);

        var body = new Polygon2D { Color = kit, ZIndex = 0 };
        body.Polygon = _Circle(9f, 16);
        AddChild(body);

        var inner = new Polygon2D { Color = Colors.White, ZIndex = 1 };
        inner.Polygon = _Circle(4f, 10);
        AddChild(inner);

        // Role indicator letter
        var lbl = new Label
        {
            Text     = PlayerRole.ToString().Split('_')[0][0].ToString(),
            ZIndex   = 2,
            Position = new Vector2(-5f, -18f),
        };
        lbl.AddThemeFontSizeOverride("font_size", 9);
        lbl.AddThemeColorOverride("font_color", Colors.White);
        AddChild(lbl);

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
