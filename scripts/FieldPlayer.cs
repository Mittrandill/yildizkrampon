using Godot;

/// res://scripts/FieldPlayer.cs
/// Saha oyuncusu — yön bazlı sprite rotasyon, insan + rol-tabanlı YZ.
public partial class FieldPlayer : CharacterBody2D
{
    public enum Team { Red, Blue }
    public enum Role { DEF, MID, FWD }

    // ─── Saha sabitleri ─────────────────────────────────────────
    public const float PITCH_W  = 1920f;
    public const float PITCH_H  = 768f;
    public const float GOAL_TOP = 279f;
    public const float GOAL_BOT = 489f;

    // ─── Metadata ───────────────────────────────────────────────
    public Team PlayerTeam  { get; set; } = Team.Red;
    public Role PlayerRole  { get; set; } = Role.MID;
    public int  SlotIndex   { get; set; } = 0;
    private float _slotX = 0.5f, _slotY = 0.5f;

    // ─── Hız sabitleri ──────────────────────────────────────────
    private const float WALK_SPEED   = 175f;
    private const float SPRINT_SPEED = 285f;
    private const float AI_SPEED     = 155f;
    private const float CLAIM_DIST   = 38f;

    // ─── Durum ──────────────────────────────────────────────────
    public Vector2 FacingDir   { get; private set; } = Vector2.Right;
    public bool    HasBall      => Football.Instance?.BallController == this;
    public float   Stamina      { get; private set; } = 100f;

    // İstatistikler
    public int GoalsScored     = 0;
    public int ShotsFired      = 0;
    public int PassesAttempted = 0;
    public int TacklesWon      = 0;

    // ─── İnsan kontrolü ─────────────────────────────────────────
    private float _shootCharge  = 0f;
    private bool  _isCharging   = false;
    private float _kickCooldown = 0f;

    // ─── Sprite ─────────────────────────────────────────────────
    private AnimatedSprite2D? _anim;

    // Sprite'ın doğal yönü: SOUTH (+Y, viewer'a doğru bakıyor).
    // Yüz yönünü SAĞA ayarlamak için: rotation = FacingDir.Angle() - PI/2
    private const float SPRITE_OFFSET = -Mathf.Pi / 2f;

    // ─── YZ ─────────────────────────────────────────────────────
    private enum AIState { Hold, Chase, Dribble, Support, Mark, Press }
    private AIState _aiState    = AIState.Hold;
    private float   _aiTimer    = 0f;
    private Vector2 _aiTarget   = Vector2.Zero;

    // ─────────────────────────────────────────────────────────────
    public override void _Ready()
    {
        if (HasMeta("team"))
            PlayerTeam = (string)GetMeta("team") == "Red" ? Team.Red : Team.Blue;
        if (HasMeta("role"))
        {
            string r = (string)GetMeta("role");
            PlayerRole = r == "DEF" ? Role.DEF : r == "FWD" ? Role.FWD : Role.MID;
        }
        if (HasMeta("slot_index")) SlotIndex = (int)GetMeta("slot_index");
        if (HasMeta("slot_x"))     _slotX    = (float)GetMeta("slot_x");
        if (HasMeta("slot_y"))     _slotY    = (float)GetMeta("slot_y");

        CollisionLayer = PlayerTeam == Team.Red ? 1u : 2u;
        CollisionMask  = 1 | 2 | 16;
        AddToGroup(PlayerTeam == Team.Red ? "team_red" : "team_blue");
        AddToGroup("field_players");

        // Kırmızı → sağa bak, Mavi → sola bak
        FacingDir = PlayerTeam == Team.Red ? Vector2.Right : Vector2.Left;
        _aiTarget = GlobalPosition;

        _SetupSprite();
    }

    // ─── Sprite kurulum ─────────────────────────────────────────

    private void _SetupSprite()
    {
        var old = GetNodeOrNull<Sprite2D>("Sprite2D");
        var scale = old?.Scale ?? new Vector2(0.30f, 0.30f);
        old?.QueueFree();

        string prefix   = PlayerTeam == Team.Red ? "player" : "blue";
        string idlePath = PlayerTeam == Team.Red
            ? "res://assets/img/player_sprite.png" : "res://assets/img/npc_blue.png";
        string w1P = $"res://assets/img/anim/{prefix}_walk1.png";
        string w2P = $"res://assets/img/anim/{prefix}_walk2.png";
        string kP  = $"res://assets/img/anim/{prefix}_kick.png";

        var idle  = GD.Load<Texture2D>(idlePath);
        var walk1 = ResourceLoader.Exists(w1P) ? GD.Load<Texture2D>(w1P) : idle;
        var walk2 = ResourceLoader.Exists(w2P) ? GD.Load<Texture2D>(w2P) : idle;
        var kick  = ResourceLoader.Exists(kP)  ? GD.Load<Texture2D>(kP)  : idle;

        var f = new SpriteFrames();
        _Anim(f, "idle", 4f,  true,  idle);
        _Anim(f, "walk", 8f,  true,  idle, walk1);
        _Anim(f, "run",  14f, true,  idle, walk1, walk2);
        _Anim(f, "kick", 10f, false, kick, idle);

        _anim = new AnimatedSprite2D
        {
            Name         = "AnimSprite",
            SpriteFrames = f,
            Scale        = scale,
            Position     = new Vector2(0, -8),
            // Başlangıç rotasyonu: sağa bak
            Rotation     = FacingDir.Angle() + SPRITE_OFFSET
        };
        AddChild(_anim);
        _anim.Play("idle");
    }

    private static void _Anim(SpriteFrames f, string n, float fps, bool loop, params Texture2D[] tx)
    {
        f.AddAnimation(n); f.SetAnimationSpeed(n, fps); f.SetAnimationLoop(n, loop);
        foreach (var t in tx) f.AddFrame(n, t);
    }

    // Sprite rotasyonunu FacingDir'e göre güncelle
    private void _RefreshSprite()
    {
        if (_anim == null) return;
        float spd    = Velocity.Length();
        bool kicking = _kickCooldown > 0.28f;
        bool sprint  = spd > WALK_SPEED * 1.1f;

        string next = kicking      ? "kick" :
                      spd < 18f    ? "idle" :
                      sprint       ? "run"  : "walk";

        if (_anim.Animation != next) _anim.Play(next);

        // Tüm yönler için rotasyon — FlipH YOK
        _anim.Rotation = FacingDir.Angle() + SPRITE_OFFSET;
    }

    // ─── Fizik ──────────────────────────────────────────────────

    public override void _PhysicsProcess(double delta)
    {
        if (_kickCooldown > 0f) _kickCooldown -= (float)delta;
        _aiTimer = Mathf.Max(0f, _aiTimer - (float)delta);
        QueueRedraw();

        // Topu kap
        var ball = Football.Instance;
        if (ball != null && ball.CanBeClaimed && _kickCooldown <= 0f
            && GlobalPosition.DistanceTo(ball.GlobalPosition) < CLAIM_DIST)
            ball.GiveControl(this);

        bool isHuman = MatchManager.Instance?.IsHumanControlled(this) ?? false;
        if (isHuman) _Human((float)delta);
        else         _AI((float)delta);
    }

    public override void _Draw()
    {
        bool isHuman = MatchManager.Instance?.IsHumanControlled(this) ?? false;
        if (!isHuman) return;
        // Yeşil ok — insan oyuncusu göstergesi
        DrawPolygon(
            new[] { new Vector2(0, -32), new Vector2(-9, -48), new Vector2(9, -48) },
            new[] { new Color(0.1f, 1f, 0.2f), new Color(0.1f, 1f, 0.2f), new Color(0.1f, 1f, 0.2f) }
        );
    }

    // ─── İNSAN KONTROLÜ ─────────────────────────────────────────

    private void _Human(float delta)
    {
        var dir    = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        bool sprint = Input.IsActionPressed("sprint");

        if (dir.LengthSquared() > 0.01f) FacingDir = dir.Normalized();

        if (sprint && dir.LengthSquared() > 0.01f)
            Stamina = Mathf.Max(0f, Stamina - delta * 20f);
        else
            Stamina = Mathf.Min(100f, Stamina + delta * 14f);

        float spd  = (sprint && Stamina > 5f) ? SPRINT_SPEED : WALK_SPEED;
        float eMod = Mathf.Clamp(GameManager.Instance.Energy / 100f, 0.5f, 1f);

        Velocity = Velocity.Lerp(dir * spd * eMod, 0.28f);
        MoveAndSlide();
        MatchManager.Instance?.SetStaminaBar(Stamina);

        if (HasBall)
        {
            // F → pas
            if (Input.IsActionJustPressed("pass")) _HumanPass();

            // SPACE basılı → şarj, bırak → şut
            if (Input.IsActionPressed("action") && !_isCharging) _isCharging = true;
            if (_isCharging)
            {
                _shootCharge = Mathf.Min(100f, _shootCharge + delta * 85f);
                MatchManager.Instance?.SetPowerBar(_shootCharge);
            }
            if (Input.IsActionJustReleased("action") && _isCharging)
            {
                _Shoot(_shootCharge, isHuman: true);
                _shootCharge = 0f; _isCharging = false;
                MatchManager.Instance?.SetPowerBar(0f);
            }
        }
        else
        {
            _shootCharge = 0f; _isCharging = false;
            MatchManager.Instance?.SetPowerBar(0f);
            // E → faul
            if (Input.IsActionJustPressed("interact")) _Tackle();
        }

        _RefreshSprite();
    }

    private void _HumanPass()
    {
        var ball = Football.Instance;
        if (ball?.BallController != this) return;
        var t = _BestPassTarget();
        if (t == null) return;
        Vector2 d = t.GlobalPosition - GlobalPosition;
        ball.Kick(d.Normalized() * Mathf.Clamp(d.Length() * 2f, 300f, 780f));
        FacingDir = d.Normalized();
        _kickCooldown = 0.45f; PassesAttempted++;
        MatchManager.Instance?.OnAssistOpportunity(t);
    }

    private void _Shoot(float power, bool isHuman)
    {
        var ball = Football.Instance;
        if (ball?.BallController != this) return;

        Vector2 goalC = PlayerTeam == Team.Red
            ? new Vector2(PITCH_W + 5f, (GOAL_TOP + GOAL_BOT) / 2f)
            : new Vector2(-5f, (GOAL_TOP + GOAL_BOT) / 2f);

        Vector2 baseDir = (goalC - GlobalPosition).Normalized();
        Vector2 dir;
        if (isHuman)
            dir = (baseDir * 0.65f + FacingDir * 0.35f).Normalized();
        else
        {
            float n = Mathf.Lerp(0.12f, 0.025f, GameManager.Instance.ShotPower / 99f);
            dir = (baseDir + new Vector2((GD.Randf() * 2 - 1) * n, (GD.Randf() * 2 - 1) * n * 1.4f)).Normalized();
        }

        float stat  = Mathf.Clamp(GameManager.Instance.ShotPower / 50f, 0.7f, 1.5f);
        float speed = Mathf.Lerp(430f, 1050f, power / 100f) * stat;
        ball.Kick(dir * speed);
        FacingDir = dir; _kickCooldown = 0.65f; ShotsFired++;
    }

    private void _Tackle()
    {
        var ball = Football.Instance;
        if (ball?.BallController == null || ball.BallController.PlayerTeam == PlayerTeam) return;
        if (GlobalPosition.DistanceTo(ball.BallController.GlobalPosition) > 56f) return;
        float chance = 0.55f * Mathf.Clamp(GameManager.Instance.Technique / 50f, 0.4f, 1.6f);
        if (GD.Randf() < chance) { ball.ForceRelease(); TacklesWon++; }
        _kickCooldown = 0.4f;
    }

    // ─── YZ KONTROLÜ ────────────────────────────────────────────

    private void _AI(float delta)
    {
        var ball = Football.Instance;
        if (ball == null) { MoveAndSlide(); return; }

        Stamina = Mathf.Min(100f, Stamina + delta * 7f);

        if (_aiTimer <= 0f)
        {
            _aiState = _Decide(ball);
            _aiTimer = _DecInterval();
        }

        _aiTarget = _Target(ball);
        _MoveTo(_aiTarget, delta);
        if (HasBall) _WithBall(ball);
        _RefreshSprite();
    }

    private AIState _Decide(Football ball)
    {
        if (HasBall) return AIState.Dribble;

        bool myBall  = ball.BallController?.PlayerTeam == PlayerTeam;
        bool free    = !ball.IsControlled;
        float dist   = GlobalPosition.DistanceTo(ball.GlobalPosition);
        bool canChase = _NearestN(ball, 2); // takım başına max 2 topu kovalasın

        switch (PlayerRole)
        {
            case Role.DEF:
                if (canChase && _InMyHalf(ball) && dist < 340f) return AIState.Chase;
                if (!myBall && _InMyHalf(ball))
                {
                    var opp = _NearestOpp();
                    if (opp != null && GlobalPosition.DistanceTo(opp.GlobalPosition) < 260f)
                        return AIState.Mark;
                }
                return AIState.Hold;

            case Role.MID:
                if (canChase && dist < 420f) return AIState.Chase;
                if (!myBall && dist < 320f)  return AIState.Press;
                return myBall ? AIState.Support : AIState.Hold;

            default: // FWD
                if (canChase && dist < 500f) return AIState.Chase;
                if (!myBall && dist < 380f)  return AIState.Press;
                return myBall ? AIState.Support : AIState.Hold;
        }
    }

    private Vector2 _Target(Football ball) => _aiState switch
    {
        AIState.Dribble               => _DribTarget(),
        AIState.Chase or AIState.Press => ball.GlobalPosition,
        AIState.Support               => _SupTarget(ball),
        AIState.Mark                  => _MarkTarget(ball),
        _                             => _FormTarget(ball.GlobalPosition),
    };

    private Vector2 _DribTarget()
    {
        float gx = PlayerTeam == Team.Red ? PITCH_W - 80f : 80f;
        return new Vector2(gx, Mathf.Lerp(GlobalPosition.Y, PITCH_H / 2f, 0.08f));
    }

    private Vector2 _SupTarget(Football ball)
    {
        float ax = PlayerTeam == Team.Red ? 1f : -1f;
        float ox = ax * (PlayerRole == Role.FWD ? 160f : 80f);
        float oy = SlotIndex % 2 == 0 ? 130f : -130f;
        return new Vector2(
            Mathf.Clamp(ball.GlobalPosition.X + ox, 60f, PITCH_W - 60f),
            Mathf.Clamp(ball.GlobalPosition.Y + oy, 60f, PITCH_H - 60f));
    }

    private Vector2 _MarkTarget(Football ball)
    {
        var opp = _NearestOpp();
        if (opp == null) return _FormTarget(ball.GlobalPosition);
        Vector2 goal = PlayerTeam == Team.Red
            ? new Vector2(0f, PITCH_H / 2f) : new Vector2(PITCH_W, PITCH_H / 2f);
        return opp.GlobalPosition + (goal - opp.GlobalPosition).Normalized() * 38f;
    }

    private Vector2 _FormTarget(Vector2 ballPos)
    {
        bool myBall = Football.Instance?.BallController?.PlayerTeam == PlayerTeam;
        float ax    = PlayerTeam == Team.Red ? 1f : -1f;
        float bx    = PlayerTeam == Team.Red ? _slotX * PITCH_W : (1f - _slotX) * PITCH_W;
        float by    = _slotY * PITCH_H;
        bx += ax * (myBall ? 0.07f : -0.05f) * PITCH_W;
        float ly = Mathf.Clamp(Mathf.Lerp(by, ballPos.Y, 0.22f), 60f, PITCH_H - 60f);
        return new Vector2(Mathf.Clamp(bx, 50f, PITCH_W - 50f), ly);
    }

    private void _MoveTo(Vector2 target, float delta)
    {
        Vector2 d = target - GlobalPosition;
        float dist = d.Length();
        if (dist < 5f) { Velocity = Vector2.Zero; MoveAndSlide(); return; }
        d /= dist;
        if (d.LengthSquared() > 0.01f) FacingDir = d;
        d += _Sep() * 0.35f;
        Velocity = d.Normalized() * AI_SPEED * _AISpdFactor();
        MoveAndSlide();
    }

    private void _WithBall(Football ball)
    {
        if (_kickCooldown > 0f) return;
        Vector2 goalC = PlayerTeam == Team.Red
            ? new Vector2(PITCH_W, (GOAL_TOP + GOAL_BOT) / 2f)
            : new Vector2(0f, (GOAL_TOP + GOAL_BOT) / 2f);
        float dist = GlobalPosition.DistanceTo(goalC);
        if (dist < 400f && (goalC - GlobalPosition).Normalized().Dot(FacingDir) > 0.2f)
        {
            _Shoot(GD.Randf() * 30f + 60f, false);
            return;
        }
        if (_SelfPress() > 0.45f || (GD.Randf() < 0.22f && dist > 540f))
        {
            var t = _BestPassTarget();
            if (t != null) { _AIPass(t, ball); }
        }
    }

    private void _AIPass(FieldPlayer t, Football ball)
    {
        if (ball.BallController != this) return;
        Vector2 d = t.GlobalPosition - GlobalPosition;
        ball.Kick(d.Normalized() * Mathf.Clamp(d.Length() * 1.9f, 280f, 740f));
        FacingDir = d.Normalized(); _kickCooldown = 0.5f;
    }

    // ─── Yardımcılar ────────────────────────────────────────────

    private bool _InMyHalf(Football ball) =>
        PlayerTeam == Team.Red
            ? ball.GlobalPosition.X < PITCH_W * 0.5f
            : ball.GlobalPosition.X > PITCH_W * 0.5f;

    private bool _NearestN(Football ball, int n)
    {
        float my = GlobalPosition.DistanceTo(ball.GlobalPosition);
        string g = PlayerTeam == Team.Red ? "team_red" : "team_blue";
        int cnt = 0;
        foreach (Node node in GetTree().GetNodesInGroup(g))
            if (node is FieldPlayer fp && fp != this
                && fp.GlobalPosition.DistanceTo(ball.GlobalPosition) < my - 20f) cnt++;
        return cnt < n;
    }

    private FieldPlayer? _BestPassTarget()
    {
        string g = PlayerTeam == Team.Red ? "team_red" : "team_blue";
        FieldPlayer? best = null; float bs = -9999f;
        foreach (Node n in GetTree().GetNodesInGroup(g))
        {
            if (n is not FieldPlayer fp || fp == this) continue;
            Vector2 v = fp.GlobalPosition - GlobalPosition;
            float fwd = PlayerTeam == Team.Red ? v.X : -v.X;
            float d   = v.Length();
            if (fwd < -100f || d < 60f || d > 700f) continue;
            float s = fwd * 0.5f - _PressOn(fp) * 45f - Mathf.Abs(d - 260f) * 0.1f;
            if (MatchManager.Instance?.IsHumanControlled(fp) ?? false) s += 30f;
            if (s > bs) { bs = s; best = fp; }
        }
        return best;
    }

    private float _SelfPress()
    {
        string g = PlayerTeam == Team.Red ? "team_blue" : "team_red";
        float p = 0f;
        foreach (Node n in GetTree().GetNodesInGroup(g))
        {
            if (n is not FieldPlayer fp) continue;
            float d = GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < 120f) p += 1f - d / 120f;
        }
        return Mathf.Clamp(p, 0f, 1f);
    }

    private float _PressOn(FieldPlayer t)
    {
        string g = PlayerTeam == Team.Red ? "team_blue" : "team_red";
        float p = 0f;
        foreach (Node n in GetTree().GetNodesInGroup(g))
        {
            if (n is not FieldPlayer fp) continue;
            float d = t.GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < 100f) p += 1f - d / 100f;
        }
        return Mathf.Clamp(p, 0f, 1f);
    }

    private FieldPlayer? _NearestOpp()
    {
        string g = PlayerTeam == Team.Red ? "team_blue" : "team_red";
        FieldPlayer? best = null; float bd = float.MaxValue;
        foreach (Node n in GetTree().GetNodesInGroup(g))
        {
            if (n is not FieldPlayer fp) continue;
            float d = GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < bd) { bd = d; best = fp; }
        }
        return best;
    }

    private Vector2 _Sep()
    {
        Vector2 f = Vector2.Zero;
        foreach (Node n in GetTree().GetNodesInGroup("field_players"))
        {
            if (n is not FieldPlayer fp || fp == this) continue;
            float d = GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < 32f && d > 0.01f)
                f += (GlobalPosition - fp.GlobalPosition).Normalized() * (1f - d / 32f) * 70f;
        }
        return f;
    }

    private float _AISpdFactor()
        => Mathf.Lerp(0.65f, 1.0f, Mathf.Clamp(GameManager.Instance.Overall / 100f, 0f, 1f));

    private float _DecInterval()
        => Mathf.Lerp(0.60f, 0.18f, Mathf.Clamp(GameManager.Instance.Overall / 100f, 0f, 1f));
}
