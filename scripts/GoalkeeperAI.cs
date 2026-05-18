using Godot;

/// res://scripts/GoalkeeperAI.cs
/// Kaleci YZ — açı bisektörü konumlanma, dalış, dağıtım.
public partial class GoalkeeperAI : CharacterBody2D
{
    public enum Team { Red, Blue }
    [Export] public Team GKTeam = Team.Red;

    private float _goalX;
    private float _goalCY = (FieldPlayer.GOAL_TOP + FieldPlayer.GOAL_BOT) / 2f;
    private float _minX, _maxX;

    private bool    _isDiving  = false;
    private Vector2 _diveTo    = Vector2.Zero;
    private float   _diveTimer = 0f;
    private float   _cooldown  = 0f;

    private const float GK_SPEED  = 148f;
    private const float DIVE_SPEED = 420f;
    private const float GK_DEPTH  = 55f;

    private AnimatedSprite2D? _anim;
    private Vector2 _facingDir = Vector2.Right;
    private const float SPRITE_OFFSET = -Mathf.Pi / 2f;

    public override void _Ready()
    {
        if (HasMeta("team")) GKTeam = (string)GetMeta("team") == "Red" ? Team.Red : Team.Blue;

        if (GKTeam == Team.Red)
        {
            _goalX = GK_DEPTH; _minX = 15f; _maxX = GK_DEPTH + 40f;
            _facingDir = Vector2.Right;
        }
        else
        {
            _goalX = FieldPlayer.PITCH_W - GK_DEPTH;
            _minX  = FieldPlayer.PITCH_W - GK_DEPTH - 40f;
            _maxX  = FieldPlayer.PITCH_W - 15f;
            _facingDir = Vector2.Left;
        }

        CollisionLayer = GKTeam == Team.Red ? 1u : 2u;
        CollisionMask  = 1 | 2 | 16;
        AddToGroup(GKTeam == Team.Red ? "team_red" : "team_blue");
        AddToGroup("goalkeepers");
        _SetupSprite();
    }

    private void _SetupSprite()
    {
        var old = GetNodeOrNull<Sprite2D>("Sprite2D");
        var sc  = old?.Scale ?? new Vector2(0.30f, 0.30f);
        old?.QueueFree();

        string idlePath = GKTeam == Team.Red
            ? "res://assets/img/player_sprite.png" : "res://assets/img/npc_blue.png";
        var idle = GD.Load<Texture2D>(idlePath);
        string prefix = GKTeam == Team.Red ? "player" : "blue";
        string w1P = $"res://assets/img/anim/{prefix}_walk1.png";
        var walk1 = ResourceLoader.Exists(w1P) ? GD.Load<Texture2D>(w1P) : idle;

        var frames = new SpriteFrames();
        _A(frames, "idle", 4f,  true,  idle);
        _A(frames, "walk", 7f,  true,  idle, walk1);
        _A(frames, "dive", 6f,  false, walk1);

        _anim = new AnimatedSprite2D
        {
            Name         = "AnimSprite",
            SpriteFrames = frames,
            Scale        = sc,
            Position     = new Vector2(0, -8),
            Modulate     = GKTeam == Team.Red ? new Color(1f, 0.7f, 0.2f) : new Color(0.7f, 0.7f, 1f),
            Rotation     = _facingDir.Angle() + SPRITE_OFFSET
        };
        AddChild(_anim);
        _anim.Play("idle");
    }

    private static void _A(SpriteFrames f, string n, float fps, bool loop, params Texture2D[] tx)
    {
        f.AddAnimation(n); f.SetAnimationSpeed(n, fps); f.SetAnimationLoop(n, loop);
        foreach (var t in tx) f.AddFrame(n, t);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_cooldown > 0f) _cooldown -= (float)delta;

        var ball = Football.Instance;
        if (ball == null) { MoveAndSlide(); return; }

        if (_isDiving) { _Dive(delta, ball); return; }

        // Yakındaki topu kap
        if (_cooldown <= 0f && !ball.IsControlled
            && GlobalPosition.DistanceTo(ball.GlobalPosition) < 38f)
        {
            _Distribute(ball); return;
        }

        // Dalış tetikle
        if (!ball.IsControlled && _ShouldDive(ball))
        {
            _StartDive(ball); return;
        }

        // Normal konumlanma
        Vector2 target = _GetPos(ball);
        Vector2 dir    = target - GlobalPosition;
        float   dist   = dir.Length();

        if (dist > 4f)
        {
            Velocity = dir.Normalized() * GK_SPEED;
            _facingDir = dir.Normalized();
        }
        else Velocity = Vector2.Zero;

        MoveAndSlide();
        _UpdateSprite(Velocity.Length() > 10f ? "walk" : "idle");
    }

    private Vector2 _GetPos(Football ball)
    {
        float ty = Mathf.Clamp(
            _goalCY + (ball.GlobalPosition.Y - _goalCY) * 0.55f,
            FieldPlayer.GOAL_TOP + 20f, FieldPlayer.GOAL_BOT - 20f);
        return new Vector2(Mathf.Clamp(_goalX, _minX, _maxX), ty);
    }

    private bool _ShouldDive(Football ball)
    {
        if (ball.LinearVelocity.Length() < 200f) return false;
        float t = _TimeX(ball.GlobalPosition, ball.LinearVelocity, _goalX);
        if (t < 0f || t > 1.3f) return false;
        float ay = ball.GlobalPosition.Y + ball.LinearVelocity.Y * t;
        return ay >= FieldPlayer.GOAL_TOP - 45f && ay <= FieldPlayer.GOAL_BOT + 45f;
    }

    private void _StartDive(Football ball)
    {
        _isDiving   = true;
        _diveTimer  = 0.75f;
        float t     = _TimeX(ball.GlobalPosition, ball.LinearVelocity, _goalX);
        float arrY  = t > 0 ? ball.GlobalPosition.Y + ball.LinearVelocity.Y * t : _goalCY;
        _diveTo     = new Vector2(_goalX, Mathf.Clamp(arrY, FieldPlayer.GOAL_TOP - 22f, FieldPlayer.GOAL_BOT + 22f));
        _UpdateSprite("dive");
    }

    private void _Dive(double delta, Football ball)
    {
        _diveTimer -= (float)delta;
        Vector2 d = _diveTo - GlobalPosition;
        Velocity = d.Length() > 6f ? d.Normalized() * DIVE_SPEED : Vector2.Zero;
        MoveAndSlide();

        if (!ball.IsControlled && GlobalPosition.DistanceTo(ball.GlobalPosition) < 42f)
        {
            _Distribute(ball); _isDiving = false; _diveTimer = 0f; return;
        }
        if (_diveTimer <= 0f) _isDiving = false;
    }

    private void _Distribute(Football ball)
    {
        FieldPlayer? t  = _FindOpen();
        Vector2 throwDir;
        float speed;
        if (t != null)
        {
            throwDir = (t.GlobalPosition - GlobalPosition).Normalized();
            speed    = Mathf.Clamp(GlobalPosition.DistanceTo(t.GlobalPosition) * 1.5f, 300f, 620f);
        }
        else
        {
            throwDir = GKTeam == Team.Red ? Vector2.Right : Vector2.Left;
            speed    = 380f;
        }
        ball.Kick(throwDir * speed);
        _cooldown = 2.0f; _isDiving = false;
    }

    private FieldPlayer? _FindOpen()
    {
        string g = GKTeam == Team.Red ? "team_red" : "team_blue";
        FieldPlayer? best = null; float bs = -9999f;
        foreach (Node n in GetTree().GetNodesInGroup(g))
        {
            if (n is not FieldPlayer fp) continue;
            float d = GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < 80f || d > 700f) continue;
            float fwd = GKTeam == Team.Red ? fp.GlobalPosition.X : FieldPlayer.PITCH_W - fp.GlobalPosition.X;
            string opp = GKTeam == Team.Red ? "team_blue" : "team_red";
            float press = 0f;
            foreach (Node on in GetTree().GetNodesInGroup(opp))
            {
                if (on is not FieldPlayer of2) continue;
                float od = fp.GlobalPosition.DistanceTo(of2.GlobalPosition);
                if (od < 100f) press += 1f - od / 100f;
            }
            float s = fwd * 0.5f - press * 50f;
            if (s > bs) { bs = s; best = fp; }
        }
        return best;
    }

    private void _UpdateSprite(string anim)
    {
        if (_anim == null) return;
        if (_anim.Animation != anim) _anim.Play(anim);
        _anim.Rotation = _facingDir.Angle() + SPRITE_OFFSET;
    }

    private float _TimeX(Vector2 pos, Vector2 vel, float tx)
    {
        float dx = tx - pos.X;
        if (Mathf.Abs(vel.X) < 0.01f) return -1f;
        float t = dx / vel.X;
        return t > 0 ? t : -1f;
    }
}
