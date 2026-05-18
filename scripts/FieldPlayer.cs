using Godot;
using System.Collections.Generic;

/// res://scripts/FieldPlayer.cs
/// Saha oyuncusu — hem insan kontrolü hem YZ modu.
/// MatchManager hangi Red oyuncunun human-controlled olduğunu belirler.
public partial class FieldPlayer : CharacterBody2D
{
    public enum Team { Red, Blue }
    public enum Role { DEF, MID, FWD }

    // --- Metadata (BuildMatch tarafından SetMeta ile set edilir) ---
    public Team PlayerTeam  { get; set; } = Team.Red;
    public Role PlayerRole  { get; set; } = Role.MID;
    public int  SlotIndex   { get; set; } = 0;

    // Formation anchor (0-1 normalized, relative to team direction)
    private float _slotX = 0.5f;
    private float _slotY = 0.5f;

    // --- Runtime state ---
    public Vector2 FacingDir    { get; private set; } = Vector2.Right;
    public bool    HasBall       => Football.Instance?.BallController == this;
    public float   Stamina       { get; private set; } = 100f;

    // Match stats (for player rating)
    public int GoalsScored    = 0;
    public int ShotsFired     = 0;
    public int PassesAttempted= 0;
    public int TacklesWon     = 0;

    // Human control
    private float _shootCharge  = 0f;
    private bool  _isCharging   = false;
    private float _kickCooldown = 0f;

    // AI state
    private enum AIState { Positioning, ChasingBall, Dribbling, Supporting, Marking, Pressing }
    private AIState _aiState    = AIState.Positioning;
    private float   _aiDecTimer = 0f;
    private Vector2 _aiTarget   = Vector2.Zero;

    // Pitch constants (shared with MatchManager)
    public const float PITCH_W    = 1920f;
    public const float PITCH_H    = 768f;
    public const float GOAL_TOP   = 279f;
    public const float GOAL_BOT   = 489f;

    // Speed constants
    private const float WALK_SPEED   = 160f;
    private const float SPRINT_SPEED = 252f;
    private const float CONTROL_DIST = 32f;
    private const float TACKLE_DIST  = 34f;

    public override void _Ready()
    {
        // Read metadata set by scene builder
        if (HasMeta("team"))    PlayerTeam = ((string)GetMeta("team")) == "Red" ? Team.Red : Team.Blue;
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

        string group = PlayerTeam == Team.Red ? "team_red" : "team_blue";
        AddToGroup(group);
        AddToGroup("field_players");

        _aiTarget = _GetFormationPos(new Vector2(PITCH_W / 2f, PITCH_H / 2f));
    }

    public override void _Draw()
    {
        bool human = MatchManager.Instance?.IsHumanControlled(this) ?? false;
        if (!human) return;

        // Yeşil aşağı ok — oyuncunun başı üzerinde
        DrawPolygon(
            new Vector2[] { new Vector2(0, -30), new Vector2(-9, -46), new Vector2(9, -46) },
            new Color[] { new Color(0.15f, 1f, 0.3f), new Color(0.15f, 1f, 0.3f), new Color(0.15f, 1f, 0.3f) }
        );
        // İnce beyaz çerçeve çizgisi
        DrawLine(new Vector2(-9, -46), new Vector2(9, -46), new Color(1,1,1,0.8f), 1.5f);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_kickCooldown > 0f) _kickCooldown -= (float)delta;
        _aiDecTimer = Mathf.Max(0f, _aiDecTimer - (float)delta);

        QueueRedraw(); // gösterge ok her frame güncellenir

        var ball = Football.Instance;

        // Try to claim free ball
        if (ball != null && ball.CanBeClaimed && _kickCooldown <= 0f)
        {
            float dist = GlobalPosition.DistanceTo(ball.GlobalPosition);
            if (dist < CONTROL_DIST)
                ball.GiveControl(this);
        }

        bool humanControlled = MatchManager.Instance?.IsHumanControlled(this) ?? false;

        if (humanControlled)
            _HumanProcess((float)delta);
        else
            _AIProcess((float)delta);
    }

    // ─────────────────────────────────────────────────────────────
    //  HUMAN CONTROL
    // ─────────────────────────────────────────────────────────────

    private void _HumanProcess(float delta)
    {
        var dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        bool sprint = Input.IsActionPressed("sprint");

        if (dir != Vector2.Zero) FacingDir = dir;

        // Stamina
        if (sprint && dir != Vector2.Zero)
            Stamina = Mathf.Max(0f, Stamina - delta * 22f);
        else
            Stamina = Mathf.Min(100f, Stamina + delta * 9f);

        float spd = (sprint && Stamina > 5f) ? SPRINT_SPEED : WALK_SPEED;
        float energyMod = Mathf.Clamp(GameManager.Instance.Energy / 100f, 0.5f, 1.0f);
        float sprintMod = Mathf.Clamp(GameManager.Instance.Sprint / 50f, 0.7f, 1.4f);
        Velocity = dir * spd * energyMod * sprintMod;
        MoveAndSlide();

        MatchManager.Instance?.SetStaminaBar(Stamina);

        if (HasBall)
        {
            // PAS: F
            if (Input.IsActionJustPressed("pass"))
                _HumanPass();

            // ŞUT: Space (şarj et ve bırak)
            if (Input.IsActionPressed("action") && !_isCharging)
                _isCharging = true;

            if (_isCharging)
            {
                _shootCharge = Mathf.Min(100f, _shootCharge + delta * 75f);
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

            // FAUL: E
            if (Input.IsActionJustPressed("interact"))
                _TryTackle();
        }
    }

    private void _HumanPass()
    {
        var ball = Football.Instance;
        if (ball?.BallController != this) return;

        FieldPlayer? target = _FindBestPassTarget();
        if (target == null) return;

        Vector2 toTarget  = target.GlobalPosition - GlobalPosition;
        float   dist      = toTarget.Length();
        float   passSpeed = Mathf.Clamp(dist * 1.8f, 320f, 720f);
        ball.Kick(toTarget.Normalized() * passSpeed);
        FacingDir = toTarget.Normalized();
        _kickCooldown     = 0.55f;
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
            // Blend joystick direction + goal direction
            shootDir = (baseDir * 0.65f + FacingDir * 0.35f).Normalized();
        }
        else
        {
            // AI shoot with slight inaccuracy based on skill
            float noise = Mathf.Lerp(0.12f, 0.02f, GameManager.Instance.ShotPower / 99f);
            shootDir = (baseDir + new Vector2(GD.Randf() * noise - noise / 2f,
                                              GD.Randf() * noise - noise / 2f)).Normalized();
        }

        float shotPowerStat = Mathf.Clamp(GameManager.Instance.ShotPower / 50f, 0.6f, 1.5f);
        float speed = Mathf.Lerp(380f, 960f, power / 100f) * shotPowerStat;
        ball.Kick(shootDir * speed);
        FacingDir     = shootDir;
        _kickCooldown = 0.65f;
        ShotsFired++;
    }

    private void _TryTackle()
    {
        var ball = Football.Instance;
        if (ball?.BallController == null) return;
        if (ball.BallController.PlayerTeam == PlayerTeam) return;

        float dist = GlobalPosition.DistanceTo(ball.BallController.GlobalPosition);
        if (dist > TACKLE_DIST * 1.6f) return;

        float techMod  = Mathf.Clamp(GameManager.Instance.Technique / 50f, 0.4f, 1.6f);
        float chance   = 0.60f * techMod;

        if (GD.Randf() < chance)
        {
            ball.ForceRelease();
            TacklesWon++;
        }
        _kickCooldown = 0.5f;
    }

    // ─────────────────────────────────────────────────────────────
    //  AI CONTROL
    // ─────────────────────────────────────────────────────────────

    private void _AIProcess(float delta)
    {
        var ball = Football.Instance;
        if (ball == null) { MoveAndSlide(); return; }

        // Update stamina (AI recovers always)
        Stamina = Mathf.Min(100f, Stamina + delta * 6f);

        if (_aiDecTimer <= 0f)
        {
            _UpdateAIState(ball);
            _aiDecTimer = _AIDecisionInterval();
        }

        _aiTarget = _ComputeAITarget(ball);
        _MoveTowardAI(_aiTarget, delta);

        if (HasBall) _AIBallDecision(ball);
    }

    private void _UpdateAIState(Football ball)
    {
        bool myTeamHasBall  = ball.BallController?.PlayerTeam == PlayerTeam;
        bool oppHasBall     = ball.IsControlled && !myTeamHasBall;
        bool ballFree       = !ball.IsControlled;

        if (HasBall)       { _aiState = AIState.Dribbling; return; }

        bool iChase        = _IsNearestToBall(ball);

        if (ballFree)
        {
            _aiState = iChase ? AIState.ChasingBall : AIState.Positioning;
            return;
        }
        if (oppHasBall)
        {
            _aiState = iChase ? AIState.Pressing : AIState.Marking;
            return;
        }
        // My team has ball
        _aiState = iChase ? AIState.Positioning : AIState.Supporting;
    }

    private Vector2 _ComputeAITarget(Football ball)
    {
        switch (_aiState)
        {
            case AIState.Dribbling:
            {
                // Drive toward opponent goal
                float gx = PlayerTeam == Team.Red ? PITCH_W * 0.85f : PITCH_W * 0.15f;
                return new Vector2(gx, Mathf.Lerp(GlobalPosition.Y, PITCH_H / 2f, 0.1f));
            }
            case AIState.ChasingBall:
            case AIState.Pressing:
                return ball.GlobalPosition;

            case AIState.Supporting:
                return _GetSupportPosition(ball);

            case AIState.Marking:
                return _GetMarkingPosition(ball);

            default:
                return _GetFormationPos(ball.GlobalPosition);
        }
    }

    private void _MoveTowardAI(Vector2 target, float delta)
    {
        float diff = _AISpeedFactor();
        float spd  = WALK_SPEED * diff;
        Vector2 dir = (target - GlobalPosition);
        float dist = dir.Length();
        if (dist < 4f) { Velocity = Vector2.Zero; MoveAndSlide(); return; }

        dir /= dist;
        if (dir != Vector2.Zero) FacingDir = dir;

        // Separation from teammates
        dir += _ComputeSeparation() * 0.4f;

        Velocity = dir.Normalized() * spd;
        MoveAndSlide();
    }

    private void _AIBallDecision(Football ball)
    {
        if (_kickCooldown > 0f) return;

        Vector2 goalCenter = PlayerTeam == Team.Red
            ? new Vector2(PITCH_W, (GOAL_TOP + GOAL_BOT) / 2f)
            : new Vector2(0f, (GOAL_TOP + GOAL_BOT) / 2f);

        float distGoal = GlobalPosition.DistanceTo(goalCenter);
        Vector2 toGoal = (goalCenter - GlobalPosition).Normalized();
        float angle    = FacingDir.Dot(toGoal);

        // ŞUT: menzile geldiyse
        float shootRange = Mathf.Lerp(250f, 450f, _AISpeedFactor());
        if (distGoal < shootRange && angle > 0.35f)
        {
            float power = GD.Randf() * 35f + 55f; // 55-90% güç
            _Shoot(power, isHuman: false);
            return;
        }

        // PAS: baskı altındaysa veya daha iyi konumda takım arkadaşı varsa
        float pressure = _GetSelfPressure(ball);
        if (pressure > 0.5f || (GD.Randf() < 0.3f && distGoal > 500f))
        {
            FieldPlayer? t = _FindBestPassTarget();
            if (t != null) { _AIPass(t, ball); return; }
        }

        // Hedef kaleye doğru dribbling — _MoveTowardAI halleder
    }

    private void _AIPass(FieldPlayer target, Football ball)
    {
        if (ball.BallController != this) return;
        Vector2 toTarget  = target.GlobalPosition - GlobalPosition;
        float   dist      = toTarget.Length();
        float   speed     = Mathf.Clamp(dist * 1.7f, 280f, 680f);
        ball.Kick(toTarget.Normalized() * speed);
        FacingDir     = toTarget.Normalized();
        _kickCooldown = 0.55f;
        MatchManager.Instance?.OnAssistOpportunity(target);
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────

    private Vector2 _GetFormationPos(Vector2 ballPos)
    {
        bool myTeamHasBall = Football.Instance?.BallController?.PlayerTeam == PlayerTeam;
        float attackDir    = PlayerTeam == Team.Red ? 1f : -1f;

        // Slot in world coords
        float baseX, baseY;
        if (PlayerTeam == Team.Red)
        {
            baseX = _slotX * PITCH_W;
        }
        else
        {
            baseX = (1f - _slotX) * PITCH_W;
        }
        baseY = _slotY * PITCH_H;

        // Push forward when attacking, drop deep when defending
        float bias = myTeamHasBall ? 0.08f : -0.06f;
        baseX += attackDir * bias * PITCH_W;

        // Pull Y toward ball (within limits)
        float blendedY = Mathf.Lerp(baseY, ballPos.Y, 0.28f);
        blendedY = Mathf.Clamp(blendedY, 60f, PITCH_H - 60f);

        return new Vector2(Mathf.Clamp(baseX, 40f, PITCH_W - 40f), blendedY);
    }

    private Vector2 _GetSupportPosition(Football ball)
    {
        // Be in triangle: left/right of ball carrier, forward
        float attackDir = PlayerTeam == Team.Red ? 1f : -1f;
        float lateral   = (SlotIndex % 2 == 0 ? 1f : -1f) * 90f;
        float forward   = 80f * attackDir;
        return ball.GlobalPosition + new Vector2(forward, lateral);
    }

    private Vector2 _GetMarkingPosition(Football ball)
    {
        // Find nearest opponent to mark
        FieldPlayer? opp = _NearestOpponentToMe();
        if (opp == null) return _GetFormationPos(ball.GlobalPosition);
        // Stand between opponent and our goal
        Vector2 ownGoal = PlayerTeam == Team.Red
            ? new Vector2(0f, PITCH_H / 2f)
            : new Vector2(PITCH_W, PITCH_H / 2f);
        return Vector2.Zero.Lerp(opp.GlobalPosition, 0.7f) +
               (ownGoal - opp.GlobalPosition).Normalized() * 30f;
    }

    private FieldPlayer? _FindBestPassTarget()
    {
        string group = PlayerTeam == Team.Red ? "team_red" : "team_blue";
        FieldPlayer? best = null;
        float bestScore  = -9999f;

        foreach (Node node in GetTree().GetNodesInGroup(group))
        {
            if (node is not FieldPlayer fp || fp == this) continue;

            Vector2 toTarget = fp.GlobalPosition - GlobalPosition;
            float forwardness = PlayerTeam == Team.Red ? toTarget.X : -toTarget.X;
            if (forwardness < -80f) continue; // geri pas kabul et ama küçük bonusla

            float distance = toTarget.Length();
            if (distance < 60f || distance > 650f) continue;

            float pressure = _GetPressureOn(fp);
            float score    = forwardness * 0.6f
                           - pressure * 40f
                           - Mathf.Abs(distance - 280f) * 0.12f;

            if (score > bestScore) { bestScore = score; best = fp; }
        }
        return best;
    }

    private float _GetSelfPressure(Football ball)
    {
        string oppGroup = PlayerTeam == Team.Red ? "team_blue" : "team_red";
        float totalPressure = 0f;
        foreach (Node node in GetTree().GetNodesInGroup(oppGroup))
        {
            if (node is not FieldPlayer fp) continue;
            float d = GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < 120f) totalPressure += 1f - d / 120f;
        }
        return Mathf.Clamp(totalPressure, 0f, 1f);
    }

    private float _GetPressureOn(FieldPlayer target)
    {
        string oppGroup = PlayerTeam == Team.Red ? "team_blue" : "team_red";
        float p = 0f;
        foreach (Node node in GetTree().GetNodesInGroup(oppGroup))
        {
            if (node is not FieldPlayer fp) continue;
            float d = target.GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < 100f) p += 1f - d / 100f;
        }
        return Mathf.Clamp(p, 0f, 1f);
    }

    private bool _IsNearestToBall(Football ball)
    {
        float myDist = GlobalPosition.DistanceTo(ball.GlobalPosition);
        string group = PlayerTeam == Team.Red ? "team_red" : "team_blue";
        foreach (Node node in GetTree().GetNodesInGroup(group))
        {
            if (node is not FieldPlayer fp || fp == this) continue;
            if (fp.GlobalPosition.DistanceTo(ball.GlobalPosition) < myDist - 15f) return false;
        }
        return true;
    }

    private FieldPlayer? _NearestOpponentToMe()
    {
        string group = PlayerTeam == Team.Red ? "team_blue" : "team_red";
        FieldPlayer? nearest = null;
        float nearestD = float.MaxValue;
        foreach (Node n in GetTree().GetNodesInGroup(group))
        {
            if (n is not FieldPlayer fp) continue;
            float d = GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < nearestD) { nearestD = d; nearest = fp; }
        }
        return nearest;
    }

    private Vector2 _ComputeSeparation()
    {
        Vector2 force = Vector2.Zero;
        foreach (Node n in GetTree().GetNodesInGroup("field_players"))
        {
            if (n is not FieldPlayer fp || fp == this) continue;
            float d = GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (d < 28f && d > 0.01f)
                force += (GlobalPosition - fp.GlobalPosition).Normalized() * (1f - d / 28f) * 60f;
        }
        return force;
    }

    private float _AISpeedFactor()
    {
        // Kolay başlangıç (Overall 38) → AI %70 hız; Overall 99 → %100 hız
        return Mathf.Lerp(0.62f, 1.0f, Mathf.Clamp(GameManager.Instance.Overall / 100f, 0f, 1f));
    }

    private float _AIDecisionInterval()
    {
        // Daha yavaş kararlar başlangıçta (0.7s → 0.25s)
        return Mathf.Lerp(0.70f, 0.25f, Mathf.Clamp(GameManager.Instance.Overall / 100f, 0f, 1f));
    }
}
