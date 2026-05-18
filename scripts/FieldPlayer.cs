using Godot;

/// res://scripts/FieldPlayer.cs
/// Saha oyuncusu — insan kontrolü + rol-tabanlı YZ (DEF/MID/FWD farklı davranır).
public partial class FieldPlayer : CharacterBody2D
{
    public enum Team { Red, Blue }
    public enum Role { DEF, MID, FWD }

    // ─── Metadata ───────────────────────────────────────────────
    public Team PlayerTeam  { get; set; } = Team.Red;
    public Role PlayerRole  { get; set; } = Role.MID;
    public int  SlotIndex   { get; set; } = 0;
    private float _slotX = 0.5f;
    private float _slotY = 0.5f;

    // ─── Saha sabitleri ─────────────────────────────────────────
    public const float PITCH_W  = 1920f;
    public const float PITCH_H  = 768f;
    public const float GOAL_TOP = 279f;
    public const float GOAL_BOT = 489f;

    // ─── Hız ────────────────────────────────────────────────────
    private const float WALK_SPEED   = 175f;
    private const float SPRINT_SPEED = 280f;
    private const float AI_BASE_SPEED = 158f;
    private const float CLAIM_DIST    = 40f;

    // ─── Durum ──────────────────────────────────────────────────
    public Vector2 FacingDir   { get; private set; } = Vector2.Right;
    public bool    HasBall      => Football.Instance?.BallController == this;
    public float   Stamina      { get; private set; } = 100f;

    // Maç istatistikleri
    public int GoalsScored     = 0;
    public int ShotsFired      = 0;
    public int PassesAttempted = 0;
    public int TacklesWon      = 0;

    // ─── İnsan kontrolü ─────────────────────────────────────────
    private float _shootCharge  = 0f;
    private bool  _isCharging   = false;
    private float _kickCooldown = 0f;

    // ─── Animasyon ──────────────────────────────────────────────
    private AnimatedSprite2D? _anim;

    // ─── YZ durumu ──────────────────────────────────────────────
    private enum AIState { Positioning, ChasingBall, Dribbling, Supporting, Marking, Pressing }
    private AIState _aiState    = AIState.Positioning;
    private float   _aiDecTimer = 0f;
    private Vector2 _aiTarget   = Vector2.Zero;

    // ─────────────────────────────────────────────────────────────
    public override void _Ready()
    {
        if (HasMeta("team"))
            PlayerTeam = ((string)GetMeta("team")) == "Red" ? Team.Red : Team.Blue;
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

        _aiTarget = GlobalPosition;
        _SetupAnimation();
    }

    public override void _Draw()
    {
        if (!(MatchManager.Instance?.IsHumanControlled(this) ?? false)) return;
        // Oyuncu üstünde yeşil ok
        DrawPolygon(
            new Vector2[] { new(0, -32), new(-10, -50), new(10, -50) },
            new Color[] { new(0.2f, 1f, 0.3f), new(0.2f, 1f, 0.3f), new(0.2f, 1f, 0.3f) }
        );
    }

    // ─── Animasyon ──────────────────────────────────────────────

    private void _SetupAnimation()
    {
        var old = GetNodeOrNull<Sprite2D>("Sprite2D");
        var sc  = old?.Scale    ?? new Vector2(0.30f, 0.30f);
        var pos = old?.Position ?? new Vector2(0, -6);
        old?.QueueFree();

        string prefix   = PlayerTeam == Team.Red ? "player" : "blue";
        string idlePath = PlayerTeam == Team.Red
            ? "res://assets/img/player_sprite.png" : "res://assets/img/npc_blue.png";
        string walk1P   = $"res://assets/img/anim/{prefix}_walk1.png";
        string walk2P   = $"res://assets/img/anim/{prefix}_walk2.png";
        string kickP    = $"res://assets/img/anim/{prefix}_kick.png";

        var idle  = GD.Load<Texture2D>(idlePath);
        var walk1 = ResourceLoader.Exists(walk1P) ? GD.Load<Texture2D>(walk1P) : idle;
        var walk2 = ResourceLoader.Exists(walk2P) ? GD.Load<Texture2D>(walk2P) : idle;
        var kick  = ResourceLoader.Exists(kickP)  ? GD.Load<Texture2D>(kickP)  : idle;

        var frames = new SpriteFrames();
        _AddAnim(frames, "idle",      4f,  true,  idle);
        _AddAnim(frames, "walk",      8f,  true,  idle, walk1);
        _AddAnim(frames, "walk_back", 7f,  true,  walk2);
        _AddAnim(frames, "run",       14f, true,  idle, walk1, walk2);
        _AddAnim(frames, "kick",      10f, false, kick, idle);

        _anim = new AnimatedSprite2D { Name = "AnimSprite", SpriteFrames = frames, Scale = sc, Position = pos };
        AddChild(_anim);
        _anim.Play("idle");
    }

    private static void _AddAnim(SpriteFrames f, string n, float fps, bool loop, params Texture2D[] texs)
    {
        f.AddAnimation(n); f.SetAnimationSpeed(n, fps); f.SetAnimationLoop(n, loop);
        foreach (var t in texs) f.AddFrame(n, t);
    }

    private void _UpdateAnimation()
    {
        if (_anim == null) return;
        float spd      = Velocity.Length();
        bool  kicking  = _kickCooldown > 0.3f;
        bool  backMove = FacingDir.Y < -0.5f && Mathf.Abs(FacingDir.X) < 0.7f;
        bool  sprinting = spd > WALK_SPEED * 1.1f;

        string next = kicking      ? "kick"      :
                      spd < 20f    ? "idle"      :
                      backMove     ? "walk_back" :
                      sprinting    ? "run"       : "walk";

        if (_anim.Animation != next) _anim.Play(next);

        if (!backMove)
        {
            if (FacingDir.X < -0.1f)     _anim.FlipH = true;
            else if (FacingDir.X > 0.1f) _anim.FlipH = false;
        }
        else _anim.FlipH = false;
    }

    // ─── Fizik döngüsü ──────────────────────────────────────────

    public override void _PhysicsProcess(double delta)
    {
        if (_kickCooldown > 0f) _kickCooldown -= (float)delta;
        _aiDecTimer = Mathf.Max(0f, _aiDecTimer - (float)delta);
        QueueRedraw();

        // Serbest topu kap
        var ball = Football.Instance;
        if (ball != null && ball.CanBeClaimed && _kickCooldown <= 0f
            && GlobalPosition.DistanceTo(ball.GlobalPosition) < CLAIM_DIST)
            ball.GiveControl(this);

        if (MatchManager.Instance?.IsHumanControlled(this) ?? false)
            _HumanProcess((float)delta);
        else
            _AIProcess((float)delta);
    }

    // ─── İnsan kontrolü ─────────────────────────────────────────

    private void _HumanProcess(float delta)
    {
        var dir    = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        bool sprint = Input.IsActionPressed("sprint");

        if (dir != Vector2.Zero) FacingDir = dir.Normalized();

        if (sprint && dir != Vector2.Zero)
            Stamina = Mathf.Max(0f, Stamina - delta * 20f);
        else
            Stamina = Mathf.Min(100f, Stamina + delta * 12f);

        float spd  = (sprint && Stamina > 5f) ? SPRINT_SPEED : WALK_SPEED;
        float eMod = Mathf.Clamp(GameManager.Instance.Energy / 100f, 0.55f, 1.0f);

        // Yumuşak ivme
        Velocity = Velocity.Lerp(dir * spd * eMod, 0.25f);
        MoveAndSlide();

        MatchManager.Instance?.SetStaminaBar(Stamina);

        if (HasBall)
        {
            if (Input.IsActionJustPressed("pass"))
                _HumanPass();

            if (Input.IsActionPressed("action") && !_isCharging)
                _isCharging = true;

            if (_isCharging)
            {
                _shootCharge = Mathf.Min(100f, _shootCharge + delta * 80f);
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

            if (Input.IsActionJustPressed("interact"))
                _TryTackle();
        }

        _UpdateAnimation();
    }

    private void _HumanPass()
    {
        var ball = Football.Instance;
        if (ball?.BallController != this) return;

        var target = _FindBestPassTarget();
        if (target == null) return;

        Vector2 dir   = target.GlobalPosition - GlobalPosition;
        float   speed = Mathf.Clamp(dir.Length() * 1.9f, 300f, 780f);
        ball.Kick(dir.Normalized() * speed);
        FacingDir     = dir.Normalized();
        _kickCooldown = 0.45f;
        PassesAttempted++;
        MatchManager.Instance?.OnAssistOpportunity(target);
    }

    private void _Shoot(float power, bool isHuman)
    {
        var ball = Football.Instance;
        if (ball?.BallController != this) return;

        Vector2 goalCenter = PlayerTeam == Team.Red
            ? new Vector2(PITCH_W + 10f, (GOAL_TOP + GOAL_BOT) / 2f)
            : new Vector2(-10f, (GOAL_TOP + GOAL_BOT) / 2f);

        Vector2 baseDir = (goalCenter - GlobalPosition).Normalized();
        Vector2 shootDir;

        if (isHuman)
        {
            // Yüz yönü + kale yönü karıştır
            shootDir = (baseDir * 0.6f + FacingDir * 0.4f).Normalized();
        }
        else
        {
            float noise = Mathf.Lerp(0.10f, 0.02f, GameManager.Instance.ShotPower / 99f);
            shootDir = (baseDir + new Vector2(
                (GD.Randf() * 2f - 1f) * noise,
                (GD.Randf() * 2f - 1f) * noise * 1.5f
            )).Normalized();
        }

        float statMod = Mathf.Clamp(GameManager.Instance.ShotPower / 50f, 0.7f, 1.5f);
        float speed   = Mathf.Lerp(420f, 1050f, power / 100f) * statMod;
        ball.Kick(shootDir * speed);
        FacingDir     = shootDir;
        _kickCooldown = 0.65f;
        ShotsFired++;
    }

    private void _TryTackle()
    {
        var ball = Football.Instance;
        if (ball?.BallController == null || ball.BallController.PlayerTeam == PlayerTeam) return;
        if (GlobalPosition.DistanceTo(ball.BallController.GlobalPosition) > 58f) return;

        float chance = 0.55f * Mathf.Clamp(GameManager.Instance.Technique / 50f, 0.4f, 1.6f);
        if (GD.Randf() < chance) { ball.ForceRelease(); TacklesWon++; }
        _kickCooldown = 0.45f;
    }

    // ─── YZ kontrolü ────────────────────────────────────────────

    private void _AIProcess(float delta)
    {
        var ball = Football.Instance;
        if (ball == null) { MoveAndSlide(); return; }

        Stamina = Mathf.Min(100f, Stamina + delta * 7f);

        if (_aiDecTimer <= 0f)
        {
            _aiState    = _DecideState(ball);
            _aiDecTimer = _AIDecisionInterval();
        }

        _aiTarget = _ComputeTarget(ball);
        _MoveToward(_aiTarget, delta);

        if (HasBall) _AIWithBall(ball);
        _UpdateAnimation();
    }

    private AIState _DecideState(Football ball)
    {
        if (HasBall) return AIState.Dribbling;

        bool myTeam   = ball.BallController?.PlayerTeam == PlayerTeam;
        bool ballFree = !ball.IsControlled;
        float dist    = GlobalPosition.DistanceTo(ball.GlobalPosition);

        // En fazla 2 oyuncu topa koşsun — formasyonu boz
        bool canChase = _IsAmongNearestN(ball, 2);

        switch (PlayerRole)
        {
            case Role.DEF:
            {
                bool inMyHalf = _BallInMyHalf(ball);
                if (canChase && inMyHalf && dist < 340f) return AIState.ChasingBall;
                if (!myTeam && inMyHalf)
                {
                    var opp = _NearestOpponent();
                    if (opp != null && GlobalPosition.DistanceTo(opp.GlobalPosition) < 260f)
                        return AIState.Marking;
                }
                return AIState.Positioning;
            }

            case Role.MID:
                if (canChase && dist < 400f) return AIState.ChasingBall;
                if (!myTeam && dist < 300f)  return AIState.Pressing;
                return myTeam ? AIState.Supporting : AIState.Positioning;

            default: // FWD
                if (canChase && dist < 480f) return AIState.ChasingBall;
                if (!myTeam && dist < 350f)  return AIState.Pressing;
                return myTeam ? AIState.Supporting : AIState.Positioning;
        }
    }

    private Vector2 _ComputeTarget(Football ball)
    {
        return _aiState switch
        {
            AIState.Dribbling                       => _DribbleTarget(),
            AIState.ChasingBall or AIState.Pressing => ball.GlobalPosition,
            AIState.Supporting                      => _SupportTarget(ball),
            AIState.Marking                         => _MarkTarget(ball),
            _                                       => _FormationTarget(ball.GlobalPosition),
        };
    }

    private Vector2 _DribbleTarget()
    {
        float gx = PlayerTeam == Team.Red ? PITCH_W - 90f : 90f;
        float gy = Mathf.Clamp(GlobalPosition.Y, GOAL_TOP + 10f, GOAL_BOT - 10f);
        return new Vector2(gx, Mathf.Lerp(GlobalPosition.Y, gy, 0.1f));
    }

    private Vector2 _SupportTarget(Football ball)
    {
        float ax   = PlayerTeam == Team.Red ? 1f : -1f;
        float offY = SlotIndex % 2 == 0 ? 120f : -120f;
        float offX = ax * (PlayerRole == Role.FWD ? 150f : 70f);
        return new Vector2(
            Mathf.Clamp(ball.GlobalPosition.X + offX, 60f, PITCH_W - 60f),
            Mathf.Clamp(ball.GlobalPosition.Y + offY, 60f, PITCH_H - 60f)
        );
    }

    private Vector2 _MarkTarget(Football ball)
    {
        var opp = _NearestOpponent();
        if (opp == null) return _FormationTarget(ball.GlobalPosition);
        Vector2 ownGoal = PlayerTeam == Team.Red
            ? new Vector2(0f, PITCH_H / 2f)
            : new Vector2(PITCH_W, PITCH_H / 2f);
        return opp.GlobalPosition + (ownGoal - opp.GlobalPosition).Normalized() * 38f;
    }

    private Vector2 _FormationTarget(Vector2 ballPos)
    {
        bool myTeam    = Football.Instance?.BallController?.PlayerTeam == PlayerTeam;
        float attackDir = PlayerTeam == Team.Red ? 1f : -1f;

        float baseX = PlayerTeam == Team.Red ? _slotX * PITCH_W : (1f - _slotX) * PITCH_W;
        float baseY = _slotY * PITCH_H;

        baseX += attackDir * (myTeam ? 0.07f : -0.05f) * PITCH_W;
        float blY = Mathf.Clamp(Mathf.Lerp(baseY, ballPos.Y, 0.22f), 60f, PITCH_H - 60f);

        return new Vector2(Mathf.Clamp(baseX, 50f, PITCH_W - 50f), blY);
    }

    private void _MoveToward(Vector2 target, float delta)
    {
        float spd  = AI_BASE_SPEED * _AISpeedFactor();
        Vector2 d  = target - GlobalPosition;
        float  dist = d.Length();

        if (dist < 5f) { Velocity = Vector2.Zero; MoveAndSlide(); return; }

        d /= dist;
        if (d != Vector2.Zero) FacingDir = d;
        d += _Separation() * 0.35f;

        Velocity = d.Normalized() * spd;
        MoveAndSlide();
    }

    private void _AIWithBall(Football ball)
    {
        if (_kickCooldown > 0f) return;

        Vector2 goalC = PlayerTeam == Team.Red
            ? new Vector2(PITCH_W, (GOAL_TOP + GOAL_BOT) / 2f)
            : new Vector2(0f, (GOAL_TOP + GOAL_BOT) / 2f);

        float distGoal = GlobalPosition.DistanceTo(goalC);

        if (distGoal < 400f && (goalC - GlobalPosition).Normalized().Dot(FacingDir) > 0.25f)
        {
            _Shoot(GD.Randf() * 30f + 60f, isHuman: false);
            return;
        }

        if (_SelfPressure() > 0.45f || (GD.Randf() < 0.22f && distGoal > 520f))
        {
            var t = _FindBestPassTarget();
            if (t != null) { _AIPass(t, ball); return; }
        }
    }

    private void _AIPass(FieldPlayer target, Football ball)
    {
        if (ball.BallController != this) return;
        Vector2 d = target.GlobalPosition - GlobalPosition;
        ball.Kick(d.Normalized() * Mathf.Clamp(d.Length() * 1.8f, 280f, 740f));
        FacingDir = d.Normalized(); _kickCooldown = 0.5f;
    }

    // ─── Yardımcı metodlar ───────────────────────────────────────

    private bool _BallInMyHalf(Football ball)
        => PlayerTeam == Team.Red
            ? ball.GlobalPosition.X < PITCH_W * 0.5f
            : ball.GlobalPosition.X > PITCH_W * 0.5f;

    private bool _IsAmongNearestN(Football ball, int n)
    {
        float myD  = GlobalPosition.DistanceTo(ball.GlobalPosition);
        string grp = PlayerTeam == Team.Red ? "team_red" : "team_blue";
        int cnt    = 0;
        foreach (Node node in GetTree().GetNodesInGroup(grp))
        {
            if (node is not FieldPlayer fp || fp == this) continue;
            if (fp.GlobalPosition.DistanceTo(ball.GlobalPosition) < myD - 20f) cnt++;
        }
        return cnt < n;
    }

    private FieldPlayer? _FindBestPassTarget()
    {
        string grp = PlayerTeam == Team.Red ? "team_red" : "team_blue";
        FieldPlayer? best = null;
        float bestSc = -9999f;
        foreach (Node node in GetTree().GetNodesInGroup(grp))
        {
            if (node is not FieldPlayer fp || fp == this) continue;
            Vector2 toFP = fp.GlobalPosition - GlobalPosition;
            float fwd    = PlayerTeam == Team.Red ? toFP.X : -toFP.X;
            float dist   = toFP.Length();
            if (fwd < -100f || dist < 60f || dist > 700f) continue;
            float prs  = _PressureOn(fp);
            float score = fwd * 0.5f - prs * 45f - Mathf.Abs(dist - 260f) * 0.1f;
            // Bonus: insan oyuncuya pas atma ihtimali
            if (MatchManager.Instance?.IsHumanControlled(fp) ?? false) score += 30f;
            if (score > bestSc) { bestSc = score; best = fp; }
        }
        return best;
    }

    private float _SelfPressure()
    {
        string opp = PlayerTeam == Team.Red ? "team_blue" : "team_red";
        float p = 0f;
        foreach (Node n in GetTree().GetNodesInGroup(opp))
        {
            if (n is not FieldPlayer fp) continue;
            float d = GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < 120f) p += 1f - d / 120f;
        }
        return Mathf.Clamp(p, 0f, 1f);
    }

    private float _PressureOn(FieldPlayer target)
    {
        string opp = PlayerTeam == Team.Red ? "team_blue" : "team_red";
        float p = 0f;
        foreach (Node n in GetTree().GetNodesInGroup(opp))
        {
            if (n is not FieldPlayer fp) continue;
            float d = target.GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < 100f) p += 1f - d / 100f;
        }
        return Mathf.Clamp(p, 0f, 1f);
    }

    private FieldPlayer? _NearestOpponent()
    {
        string grp = PlayerTeam == Team.Red ? "team_blue" : "team_red";
        FieldPlayer? nearest = null; float nd = float.MaxValue;
        foreach (Node n in GetTree().GetNodesInGroup(grp))
        {
            if (n is not FieldPlayer fp) continue;
            float d = GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < nd) { nd = d; nearest = fp; }
        }
        return nearest;
    }

    private Vector2 _Separation()
    {
        Vector2 f = Vector2.Zero;
        foreach (Node n in GetTree().GetNodesInGroup("field_players"))
        {
            if (n is not FieldPlayer fp || fp == this) continue;
            float d = GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < 32f && d > 0.01f)
                f += (GlobalPosition - fp.GlobalPosition).Normalized() * (1f - d / 32f) * 75f;
        }
        return f;
    }

    private float _AISpeedFactor()
        => Mathf.Lerp(0.65f, 1.0f, Mathf.Clamp(GameManager.Instance.Overall / 100f, 0f, 1f));

    private float _AIDecisionInterval()
        => Mathf.Lerp(0.60f, 0.20f, Mathf.Clamp(GameManager.Instance.Overall / 100f, 0f, 1f));
}
