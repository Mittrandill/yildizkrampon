using Godot;
using System.Collections.Generic;

public partial class NeighborhoodMatchController : Node2D
{
    public Rect2 FieldBounds { get; } = new(new Vector2(80, 70), new Vector2(960, 520));
    public Vector2 GoalY { get; } = new(260, 420);
    public float LeftGoalX { get; } = 80f;
    public float RightGoalX { get; } = 1040f;
    public List<MatchActor> Actors { get; } = new();
    public string LastRestartReason { get; private set; } = "";
    public int CurrentHalf { get; private set; } = 1;
    public bool IsHalfTimePause => _pauseAction == MatchPauseAction.StartSecondHalf;

    private enum MatchPauseAction
    {
        None,
        FinishGoalSequence,
        StartSecondHalf
    }

    private const float HalfDuration = 45f;
    private const float MatchDuration = HalfDuration * 2f;
    private const string HomeTeamName = "Kirmizi";
    private const string AwayTeamName = "Mavi";
    private static readonly Color HomeKitColor = new(0.86f, 0.18f, 0.12f);
    private static readonly Color AwayKitColor = new(0.05f, 0.30f, 0.78f);
    private MatchBall? _ball;
    private Label? _scoreLabel;
    private Label? _helpLabel;
    private Label? _refereeLabel;
    private Label? _leftInfoLabel;
    private Label? _rightInfoLabel;
    private ColorRect? _leftStaminaFill;
    private ColorRect? _rightStaminaFill;
    private ColorRect? _leftShotFill;
    private ColorRect? _rightShotFill;
    private int _homeScore;
    private int _awayScore;
    private float _elapsed;
    private float _goalPause;
    private MatchPauseAction _pauseAction = MatchPauseAction.None;
    private int _pendingGoalTeam = -1;
    private int _kickoffTeam;
    private bool _finished;

    public override void _Ready()
    {
        GD.Randomize();
        GetNodeOrNull<Camera2D>("Camera")?.MakeCurrent();
        BuildPitch();
        SpawnBall();
        SpawnTeams();
        BuildUi();
        ResetForKickoff("Baslama");
    }

    public override void _Process(double delta)
    {
        if (_finished)
            return;

        float dt = (float)delta;
        if (_goalPause > 0f)
        {
            _goalPause = Mathf.Max(_goalPause - dt, 0f);
            if (_goalPause <= 0f && _pendingGoalTeam >= 0)
                FinishGoalSequence();
            else if (_goalPause <= 0f && _pauseAction == MatchPauseAction.StartSecondHalf)
                BeginSecondHalf();
            UpdateScoreLabel();
            UpdateInfoPanels();
            return;
        }

        _elapsed += dt;
        if (CurrentHalf == 1 && _elapsed >= HalfDuration)
        {
            StartHalftime();
            UpdateScoreLabel();
            UpdateInfoPanels();
            return;
        }

        TryGoalkeeperSaves();
        CheckBallExit();
        RescueStuckBall();
        UpdateScoreLabel();
        UpdateInfoPanels();

        if (_elapsed >= MatchDuration)
            FinishMatch();
    }

    public bool IsMatchPaused() => _finished || _goalPause > 0f;

    public Vector2 ClampActorPosition(Vector2 actorPosition, int team, bool goalkeeper)
    {
        var result = actorPosition;
        if (goalkeeper)
        {
            float goalX = DefenseGoalXForTeam(team);
            if (goalX == LeftGoalX)
                result.X = Mathf.Clamp(result.X, FieldBounds.Position.X + 8f, FieldBounds.Position.X + 112f);
            else
                result.X = Mathf.Clamp(result.X, FieldBounds.End.X - 112f, FieldBounds.End.X - 8f);
        }
        result.X = Mathf.Clamp(result.X, FieldBounds.Position.X + 10f, FieldBounds.End.X - 10f);
        result.Y = Mathf.Clamp(result.Y, FieldBounds.Position.Y + 10f, FieldBounds.End.Y - 10f);
        return result;
    }

    public void ResolveActorOverlap(MatchActor actor)
    {
        float minDistance = _ball?.Holder == actor ? 16f : 18f;
        Vector2 push = Vector2.Zero;
        foreach (var other in Actors)
        {
            if (other == actor)
                continue;
            Vector2 offset = actor.Position - other.Position;
            float distance = offset.Length();
            if (distance < minDistance)
            {
                Vector2 direction = distance > 0.01f ? offset / distance : Vector2.Right.Rotated((Actors.IndexOf(actor) + 1) * 0.73f);
                push += direction * (minDistance - distance) * 0.12f;
            }
        }
        if (push.LengthSquared() > 0.01f)
            actor.Position = ClampActorPosition(actor.Position + push.LimitLength(0.95f), actor.Team, actor.IsGoalkeeper);
    }

    public bool ShouldActorPress(MatchActor actor)
    {
        if (actor.IsGoalkeeper || _ball == null)
            return false;
        if (_ball.Holder != null)
        {
            if (_ball.Holder.Team == actor.Team)
                return false;
            if (FindPrimaryPresser(actor.Team) == actor)
                return true;
            return actor.Role == "forward" && actor.Position.DistanceTo(_ball.Holder.Position) < 92f && ForwardRoomForTeam(_ball.Holder.Team, _ball.Holder.Position) < 330f;
        }
        if (NearestTeamActorToBall(actor.Team) == actor)
            return true;
        return actor.Role == "forward" && _ball.Position.DistanceTo(actor.Position) < 118f;
    }

    public Vector2 GetAiTarget(MatchActor actor)
    {
        if (_ball == null)
            return actor.HomePosition;
        if (actor.IsGoalkeeper)
            return GetGoalkeeperTarget(actor);
        if (_ball.Holder == null)
            return NearestTeamActorToBall(actor.Team) == actor ? _ball.Position : LooseBallShapeTarget(actor);
        if (_ball.Holder != null && _ball.Holder.Team == actor.Team)
            return InPossessionTarget(actor);
        if (_ball.Holder != null && _ball.Holder.Team != actor.Team)
            return OutOfPossessionTarget(actor);
        return actor.HomePosition;
    }

    public Vector2 GetGoalkeeperTarget(MatchActor keeper)
    {
        if (_ball == null)
            return keeper.HomePosition;
        float goalX = DefenseGoalXForTeam(keeper.Team);
        if (_ball.Holder == null && !_ball.IsAirborne())
        {
            float ballSpeed = _ball.Velocity.Length();
            float keeperDistance = keeper.Position.DistanceTo(_ball.Position);
            bool inKeeperZone = Mathf.Abs(_ball.Position.X - goalX) < 132f
                && _ball.Position.Y >= GoalY.X - 74f
                && _ball.Position.Y <= GoalY.Y + 74f;
            if (inKeeperZone && keeperDistance < 178f && ballSpeed < 255f)
                return ClampActorPosition(_ball.Position, keeper.Team, true);
        }
        Vector2 threat = _ball.Holder != null ? _ball.Holder.Position : _ball.Position;
        float distanceToGoal = Mathf.Abs(threat.X - goalX);
        float threatWeight = 1f - Mathf.Clamp(distanceToGoal / 520f, 0f, 1f);
        if (_ball.Holder != null && _ball.Holder.Team == keeper.Team)
            threatWeight = 0f;
        float targetY = Mathf.Lerp(GoalCenterY(), threat.Y, 0.28f + threatWeight * 0.38f);
        targetY = Mathf.Clamp(targetY, GoalY.X + 18f, GoalY.Y - 18f);
        float targetX = goalX + GetAttackDirection(keeper.Team).X * 42f;
        return new Vector2(targetX, targetY);
    }

    public bool CanActorShoot(MatchActor actor)
    {
        float distance = actor.Position.DistanceTo(new Vector2(GoalXForTeam(actor.Team), GoalCenterY()));
        Vector2 attack = GetAttackDirection(actor.Team);
        bool advanced = attack.X > 0f
            ? actor.Position.X > FieldBounds.GetCenter().X + 72f
            : actor.Position.X < FieldBounds.GetCenter().X - 72f;
        bool central = Mathf.Abs(actor.Position.Y - GoalCenterY()) < 172f;
        return advanced && central && distance < 470f;
    }

    public float EvaluatePass(MatchActor actor, MatchActor? target)
    {
        if (target == null)
            return 0f;
        float distance = actor.Position.DistanceTo(target.Position);
        if (distance < 42f || distance > 405f)
            return 0f;
        float forwardGain = ForwardGainForTeam(actor.Team, actor.Position, target.Position);
        float targetPressure = GetPressure(target);
        float score = 0.28f;
        score += Mathf.Clamp(forwardGain / 220f, -0.2f, 0.42f);
        score += target.Role == "forward" ? 0.2f : 0.08f;
        score += IsPassLaneClear(actor.Position, target.Position, actor.Team) ? 0.16f : -0.22f;
        score += ActorWantsPass(target) ? 0.16f : 0f;
        score -= targetPressure * 0.22f;
        if (ForwardRoomForTeam(target.Team, target.Position) < 250f && target.Role == "forward")
            score += 0.14f;
        if (target.Controlled && GetPressure(target) < 0.48f)
            score += 0.22f;
        return Mathf.Clamp(score, 0f, 1f);
    }

    public string TeamName(int team) => team == 0 ? HomeTeamName : AwayTeamName;

    public Color TeamKitColor(int team) => team == 0 ? HomeKitColor : AwayKitColor;

    public float VisualScaleForFieldY(float y)
    {
        float t = Mathf.Clamp((y - FieldBounds.Position.Y) / FieldBounds.Size.Y, 0f, 1f);
        return Mathf.Lerp(0.90f, 1.10f, t);
    }

    public float EvaluateCarry(MatchActor actor)
    {
        Vector2 ahead = BallCarrierTarget(actor);
        ahead.X = Mathf.Clamp(ahead.X, FieldBounds.Position.X + 42f, FieldBounds.End.X - 42f);
        ahead.Y = Mathf.Clamp(ahead.Y, FieldBounds.Position.Y + 42f, FieldBounds.End.Y - 42f);
        float score = 0.18f;
        score += IsLaneClearForCarry(actor.Position, ahead, actor.Team) ? 0.42f : -0.18f;
        score += Mathf.Clamp(ForwardRoomForTeam(actor.Team, actor.Position) / 520f, 0f, 0.24f);
        score -= GetPressure(actor) * 0.34f;
        if (Mathf.Abs(actor.Position.Y - GoalCenterY()) < 150f)
            score += 0.08f;
        return Mathf.Clamp(score, 0f, 1f);
    }

    public float EvaluateShot(MatchActor actor)
    {
        Vector2 goal = new(GoalXForTeam(actor.Team), GoalCenterY());
        float distance = actor.Position.DistanceTo(goal);
        float angleBonus = 1f - Mathf.Clamp(Mathf.Abs(actor.Position.Y - GoalCenterY()) / 170f, 0f, 1f);
        float distanceScore = 1f - Mathf.Clamp(distance / 520f, 0f, 1f);
        float laneBonus = IsPassLaneClear(actor.Position, goal, actor.Team) ? 0.22f : -0.12f;
        float keeperPenalty = GoalkeeperCoverage(actor.Team) * 0.18f;
        return Mathf.Clamp(distanceScore * 0.62f + angleBonus * 0.38f + laneBonus - GetPressure(actor) * 0.18f - keeperPenalty, 0f, 1f);
    }

    public MatchActor? FindBestPassTarget(MatchActor actor)
    {
        MatchActor? best = null;
        float bestScore = -1f;
        foreach (var candidate in Actors)
        {
            if (candidate == actor || candidate.Team != actor.Team || candidate.IsGoalkeeper)
                continue;
            float score = EvaluatePass(actor, candidate);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        return best;
    }

    public MatchActor? GetPassRequestTarget(int team)
    {
        if (_ball?.Holder == null || _ball.Holder.Team != team)
            return null;
        foreach (var actor in Actors)
        {
            if (actor.Team == team && actor.HasActivePassRequest() && GetPressure(actor) < 0.9f && IsPassLaneClear(_ball.Holder.Position, actor.Position, team))
                return actor;
        }
        return null;
    }

    public bool ActorWantsPass(MatchActor actor)
    {
        if (_ball?.Holder == null || _ball.Holder.Team != actor.Team || actor == _ball.Holder || actor.IsGoalkeeper || actor.Role == "defender")
            return false;
        if (GetPressure(actor) > 0.72f)
            return false;
        float gain = ForwardGainForTeam(actor.Team, _ball.Holder.Position, actor.Position);
        return gain > 40f && ForwardRoomForTeam(actor.Team, actor.Position) > 105f && IsPassLaneClear(_ball.Holder.Position, actor.Position, actor.Team);
    }

    public bool TryFindThroughPass(MatchActor actor, out Vector2 targetPoint, out MatchActor? runner)
    {
        targetPoint = Vector2.Zero;
        runner = null;
        if (_ball?.Holder != actor || actor.IsGoalkeeper)
            return false;

        Vector2 attack = GetAttackDirection(actor.Team);
        float bestScore = 0.25f;
        foreach (var candidate in Actors)
        {
            if (candidate == actor || candidate.Team != actor.Team || candidate.IsGoalkeeper || candidate.Role == "defender")
                continue;
            float gain = ForwardGainForTeam(actor.Team, actor.Position, candidate.Position);
            if (gain < 22f)
                continue;
            Vector2 space = ClampToField(candidate.Position + attack * (candidate.Role == "forward" ? 122f : 88f), 38f);
            if (!IsPassLaneClear(actor.Position, space, actor.Team))
                continue;
            float pressure = GetPressure(candidate);
            float score = 0.42f + Mathf.Clamp(gain / 230f, 0f, 0.32f) + Mathf.Clamp(ForwardRoomForTeam(actor.Team, candidate.Position) / 420f, 0f, 0.22f) - pressure * 0.24f;
            if (candidate.Controlled)
                score += 0.14f;
            if (score > bestScore)
            {
                bestScore = score;
                targetPoint = space;
                runner = candidate;
            }
        }
        return runner != null;
    }

    public bool IsLaneClearForCarry(Vector2 from, Vector2 to, int team)
    {
        foreach (var candidate in Actors)
        {
            if (candidate.Team == team)
                continue;
            Vector2 closest = Geometry2D.GetClosestPointToSegment(candidate.Position, from, to);
            if (closest.DistanceTo(candidate.Position) < 44f)
                return false;
        }
        return true;
    }

    public bool IsPassLaneClear(Vector2 from, Vector2 to, int team)
    {
        foreach (var candidate in Actors)
        {
            if (candidate.Team == team)
                continue;
            Vector2 closest = Geometry2D.GetClosestPointToSegment(candidate.Position, from, to);
            if (closest.DistanceTo(candidate.Position) < 34f)
                return false;
        }
        return true;
    }

    public float GetPressure(MatchActor actor)
    {
        float pressure = 0f;
        foreach (var candidate in Actors)
        {
            if (candidate.Team == actor.Team)
                continue;
            float distance = candidate.Position.DistanceTo(actor.Position);
            if (distance < 120f)
                pressure += 1f - distance / 120f;
        }
        return Mathf.Clamp(pressure, 0f, 1f);
    }

    public Vector2 GetShotDirection(MatchActor actor)
    {
        float upperCorner = GoalY.X + 14f;
        float lowerCorner = GoalY.Y - 14f;
        float cornerY = actor.Position.Y <= GoalCenterY() ? lowerCorner : upperCorner;
        MatchActor? keeper = TeamGoalkeeper(1 - actor.Team);
        if (keeper != null)
        {
            float upperGap = Mathf.Abs(keeper.Position.Y - upperCorner);
            float lowerGap = Mathf.Abs(keeper.Position.Y - lowerCorner);
            cornerY = upperGap > lowerGap ? upperCorner : lowerCorner;
        }
        Vector2 target = new(GoalXForTeam(actor.Team), cornerY);
        return (target - actor.Position).Normalized();
    }

    public Vector2 GetAttackDirection(int team)
    {
        bool homeAttacksRight = CurrentHalf == 1;
        if (team == 0)
            return homeAttacksRight ? Vector2.Right : Vector2.Left;
        return homeAttacksRight ? Vector2.Left : Vector2.Right;
    }

    public float GoalXForTeam(int team) => GetAttackDirection(team).X > 0f ? RightGoalX : LeftGoalX;

    public float DefenseGoalXForTeam(int team) => GetAttackDirection(team).X > 0f ? LeftGoalX : RightGoalX;

    public float GoalCenterY() => (GoalY.X + GoalY.Y) * 0.5f;

    public float ForwardGainForTeam(int team, Vector2 from, Vector2 to) => (to.X - from.X) * GetAttackDirection(team).X;

    public float ForwardRoomForTeam(int team, Vector2 from) => GetAttackDirection(team).X > 0f ? FieldBounds.End.X - from.X : from.X - FieldBounds.Position.X;

    public void ShowRefereeMessage(string message)
    {
        LastRestartReason = message;
        if (_refereeLabel != null)
            _refereeLabel.Text = message;
    }

    private void TryGoalkeeperSaves()
    {
        if (_ball == null || _ball.Holder != null || _ball.LastTouchTeam < 0)
            return;

        foreach (var actor in Actors)
        {
            if (!actor.IsGoalkeeper || actor.Team == _ball.LastTouchTeam)
                continue;
            if (actor.TryGoalkeeperSave())
                return;
        }
    }

    private float GoalkeeperCoverage(int attackingTeam)
    {
        MatchActor? keeper = TeamGoalkeeper(1 - attackingTeam);
        if (keeper == null)
            return 0f;
        float centerOffset = Mathf.Abs(keeper.Position.Y - GoalCenterY());
        return 1f - Mathf.Clamp(centerOffset / 96f, 0f, 1f);
    }

    private Vector2 InPossessionTarget(MatchActor actor)
    {
        Vector2 attack = GetAttackDirection(actor.Team);
        if (_ball?.Holder == actor)
            return BallCarrierTarget(actor);
        if (actor.Role == "forward")
            return RunBehindTarget(actor);
        if (actor.Role == "support")
            return AdvancedSupportPosition(actor, 160f, 74f);
        if (actor.Role == "defender")
            return DefenderOutletPosition(actor);
        return actor.HomePosition;
    }

    private Vector2 BallCarrierTarget(MatchActor actor)
    {
        Vector2 goal = new(GoalXForTeam(actor.Team), GoalCenterY());
        Vector2 toGoal = (goal - actor.Position).Normalized();
        Vector2 attack = GetAttackDirection(actor.Team);
        Vector2 direction = attack.Lerp(toGoal, 0.72f).Normalized();

        float centerPull = Mathf.Clamp((actor.Position.Y - GoalCenterY()) / 210f, -1f, 1f);
        direction = (direction + new Vector2(0f, -centerPull * 0.58f)).Normalized();
        if (actor.Position.Y < FieldBounds.Position.Y + 96f)
            direction = (direction + Vector2.Down * 0.85f).Normalized();
        else if (actor.Position.Y > FieldBounds.End.Y - 96f)
            direction = (direction + Vector2.Up * 0.85f).Normalized();

        float forward = actor.Role == "forward" ? 128f : 104f;
        Vector2 target = actor.Position + direction * forward;
        target.X = Mathf.Clamp(target.X, FieldBounds.Position.X + 48f, FieldBounds.End.X - 48f);
        target.Y = Mathf.Clamp(target.Y, FieldBounds.Position.Y + 58f, FieldBounds.End.Y - 58f);
        return target;
    }

    private Vector2 LooseBallShapeTarget(MatchActor actor)
    {
        Vector2 attack = GetAttackDirection(actor.Team);
        Vector2 ballPosition = _ball?.Position ?? actor.HomePosition;
        Vector2 target = actor.Role switch
        {
            "forward" => ballPosition + attack * 86f + new Vector2(0f, actor.HomePosition.Y < GoalCenterY() ? -58f : 58f),
            "support" => ballPosition - attack * 48f + new Vector2(0f, actor.HomePosition.Y < GoalCenterY() ? -42f : 42f),
            "defender" => ballPosition - attack * 132f + new Vector2(0f, actor.HomePosition.Y < GoalCenterY() ? -62f : 62f),
            _ => actor.HomePosition
        };
        return ClampToField(target, 40f);
    }

    private Vector2 OutOfPossessionTarget(MatchActor actor)
    {
        if (ShouldActorPress(actor))
            return _ball?.Holder != null ? PressingTarget(actor) : _ball!.Position;
        MatchActor? mark = FindMarkTarget(actor);
        if (mark != null && _ball?.Holder != null)
            return MarkingTarget(actor, mark);
        if (actor.Role == "defender" && _ball != null)
        {
            float shieldX = DefenseGoalXForTeam(actor.Team) == LeftGoalX ? FieldBounds.Position.X + 230f : FieldBounds.End.X - 230f;
            return new Vector2(shieldX, Mathf.Clamp(_ball.Position.Y, 180f, 440f));
        }
        return actor.HomePosition;
    }

    private Vector2 AdvancedSupportPosition(MatchActor actor, float forward, float laneOffset)
    {
        Vector2 basePosition = _ball?.Holder?.Position ?? actor.HomePosition;
        Vector2 target = basePosition + GetAttackDirection(actor.Team) * forward + new Vector2(0f, laneOffset);
        return ClampToField(target, 36f);
    }

    private Vector2 DefenderOutletPosition(MatchActor actor)
    {
        Vector2 attack = GetAttackDirection(actor.Team);
        Vector2 basePosition = _ball?.Holder?.Position ?? actor.HomePosition;
        float lane = actor.HomePosition.Y < GoalCenterY() ? -86f : 86f;
        Vector2 target = basePosition - attack * 118f + new Vector2(0f, lane);
        float minForward = DefenseGoalXForTeam(actor.Team) + attack.X * 155f;
        if (attack.X > 0f)
            target.X = Mathf.Max(target.X, minForward);
        else
            target.X = Mathf.Min(target.X, minForward);
        return ClampToField(target, 40f);
    }

    private Vector2 RunBehindTarget(MatchActor actor)
    {
        Vector2 basePosition = _ball?.Holder?.Position ?? actor.HomePosition;
        float lane = actor.HomePosition.Y < GoalCenterY() ? -92f : 92f;
        Vector2 target = basePosition + GetAttackDirection(actor.Team) * 285f + new Vector2(0f, lane);
        return ClampToField(target, 36f);
    }

    private Vector2 PressingTarget(MatchActor actor)
    {
        if (_ball?.Holder == null)
            return actor.HomePosition;
        Vector2 ownGoal = new(DefenseGoalXForTeam(actor.Team), GoalCenterY());
        Vector2 goalSide = (ownGoal - _ball.Holder.Position).Normalized();
        Vector2 side = new(-goalSide.Y, goalSide.X);
        float laneSide = actor.HomePosition.Y < GoalCenterY() ? -1f : 1f;
        float containDistance = actor.Position.DistanceTo(_ball.Holder.Position) > 42f ? 30f : 8f;
        Vector2 target = _ball.Holder.Position + goalSide * containDistance + side * laneSide * 16f;
        return ClampToField(target, 34f);
    }

    private Vector2 MarkingTarget(MatchActor actor, MatchActor target)
    {
        Vector2 ownGoal = new(DefenseGoalXForTeam(actor.Team), GoalCenterY());
        Vector2 goalSide = (ownGoal - target.Position).Normalized();
        return ClampToField(target.Position + goalSide * 34f, 34f);
    }

    private MatchActor? FindMarkTarget(MatchActor actor)
    {
        string desired = actor.Role switch
        {
            "defender" => "forward",
            "forward" => "defender",
            _ => "support"
        };
        foreach (var candidate in Actors)
            if (candidate.Team != actor.Team && candidate.Role == desired && !candidate.IsGoalkeeper)
                return candidate;
        return null;
    }

    private MatchActor? TeamGoalkeeper(int team)
    {
        foreach (var actor in Actors)
            if (actor.Team == team && actor.IsGoalkeeper)
                return actor;
        return null;
    }

    private MatchActor? NearestTeamActorToBall(int team)
    {
        if (_ball == null)
            return null;
        MatchActor? best = null;
        float bestDistance = float.MaxValue;
        foreach (var candidate in Actors)
        {
            if (candidate.Team != team || candidate.IsGoalkeeper)
                continue;
            float distance = candidate.Position.DistanceSquaredTo(_ball.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }
        return best;
    }

    private MatchActor? FindPrimaryPresser(int team)
    {
        if (_ball?.Holder == null || _ball.Holder.Team == team)
            return null;
        MatchActor? best = null;
        float bestScore = float.MinValue;
        foreach (var candidate in Actors)
        {
            if (candidate.Team != team || candidate.IsGoalkeeper)
                continue;
            float distance = candidate.Position.DistanceTo(_ball.Holder.Position);
            float roleBonus = candidate.Role == "support" ? 34f : candidate.Role == "forward" ? 24f : 10f;
            float score = 220f - distance + roleBonus;
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        return best;
    }

    public MatchActor? FindGoalkeeperOutlet(MatchActor keeper)
    {
        MatchActor? best = null;
        float bestScore = -1f;
        Vector2 attack = GetAttackDirection(keeper.Team);
        foreach (var candidate in Actors)
        {
            if (candidate.Team != keeper.Team || candidate == keeper || candidate.IsGoalkeeper)
                continue;
            float distance = keeper.Position.DistanceTo(candidate.Position);
            if (distance < 42f || distance > 455f)
                continue;
            float forwardGain = ForwardGainForTeam(keeper.Team, keeper.Position, candidate.Position);
            float laneScore = IsPassLaneClear(keeper.Position, candidate.Position, keeper.Team) ? 0.28f : -0.34f;
            float pressurePenalty = GetPressure(candidate) * 0.34f;
            float distanceScore = 1f - Mathf.Abs(distance - 210f) / 280f;
            float roleBonus = candidate.Role == "defender" ? 0.18f : candidate.Role == "support" ? 0.24f : 0.08f;
            if (forwardGain < -65f)
                roleBonus -= 0.18f;
            float widthBonus = Mathf.Clamp(Mathf.Abs(candidate.Position.Y - GoalCenterY()) / 190f, 0f, 0.14f);
            float score = 0.32f + Mathf.Clamp(distanceScore, -0.2f, 0.34f) + Mathf.Clamp(forwardGain / 360f, -0.12f, 0.22f) + laneScore + roleBonus + widthBonus - pressurePenalty;
            if (candidate.Position.X * attack.X > keeper.Position.X * attack.X)
                score += 0.08f;
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        return bestScore > 0.34f ? best : null;
    }

    private Vector2 ClampToField(Vector2 value, float margin)
    {
        return new Vector2(
            Mathf.Clamp(value.X, FieldBounds.Position.X + margin, FieldBounds.End.X - margin),
            Mathf.Clamp(value.Y, FieldBounds.Position.Y + margin, FieldBounds.End.Y - margin)
        );
    }

    private void SpawnBall()
    {
        _ball = new MatchBall { Name = "Ball", Bounds = FieldBounds.Grow(-8f), ZIndex = 60 };
        AddChild(_ball);
        _ball.ResetToCenter();
    }

    private void SpawnTeams()
    {
        AddActor(0, new Vector2(122, 340), HomeKitColor.Darkened(0.12f), true, false, "goalkeeper", "Kirmizi Kaleci");
        AddActor(0, new Vector2(316, 340), HomeKitColor, false, true, "support", GameManager.Instance.PlayerName);
        AddActor(0, new Vector2(420, 456), HomeKitColor, false, false, "defender", "Eren");
        AddActor(0, new Vector2(504, 224), HomeKitColor, false, false, "forward", "Mert");
        AddActor(1, new Vector2(998, 340), AwayKitColor.Darkened(0.12f), true, false, "goalkeeper", "Mavi Kaleci");
        AddActor(1, new Vector2(804, 340), AwayKitColor, false, false, "support", "Baran");
        AddActor(1, new Vector2(700, 456), AwayKitColor, false, false, "defender", "Can");
        AddActor(1, new Vector2(616, 224), AwayKitColor, false, false, "forward", "Ali");
    }

    private void AddActor(int team, Vector2 home, Color color, bool goalkeeper, bool human, string role, string displayName)
    {
        if (_ball == null)
            return;
        var actor = new MatchActor { Name = displayName.Replace(" ", "") };
        actor.Setup(this, _ball, team, home, color, goalkeeper, human, role, displayName);
        AddChild(actor);
        Actors.Add(actor);
    }

    private void ResetForKickoff(string reason)
    {
        ShowRefereeMessage(reason);
        _pauseAction = MatchPauseAction.None;
        _ball?.ResetToCenter();
        foreach (var actor in Actors)
            actor.Position = actor.HomePosition;
        MatchActor? kickoff = Actors.Find(a => a.Team == _kickoffTeam && a.Role == "support");
        if (_ball != null && kickoff != null)
        {
            Vector2 direction = GetAttackDirection(kickoff.Team);
            kickoff.Position = ClampActorPosition(_ball.Position - direction * 26f, kickoff.Team, kickoff.IsGoalkeeper);
            _ball.Claim(kickoff, direction);
            _ball.Position = kickoff.Position + _ball.HoldOffset;
        }
    }

    private void CheckBallExit()
    {
        if (_ball == null || _ball.Holder != null || _goalPause > 0f)
            return;

        bool inGoalY = _ball.Position.Y >= GoalY.X && _ball.Position.Y <= GoalY.Y;
        if (_ball.Position.X <= LeftGoalX && inGoalY)
        {
            int scoringTeam = TeamAttackingSide(0);
            if (_ball.Height <= 30f && _ball.LastTouchTeam == scoringTeam)
                ScoreGoal(scoringTeam);
            else
                CallGoalLineRestart(0);
        }
        else if (_ball.Position.X >= RightGoalX && inGoalY)
        {
            int scoringTeam = TeamAttackingSide(1);
            if (_ball.Height <= 30f && _ball.LastTouchTeam == scoringTeam)
                ScoreGoal(scoringTeam);
            else
                CallGoalLineRestart(1);
        }
        else if (!FieldBounds.HasPoint(_ball.Position))
        {
            if (_ball.Position.Y < FieldBounds.Position.Y)
                CallThrowIn(new Vector2(Mathf.Clamp(_ball.Position.X, FieldBounds.Position.X + 24f, FieldBounds.End.X - 24f), FieldBounds.Position.Y + 28f));
            else if (_ball.Position.Y > FieldBounds.End.Y)
                CallThrowIn(new Vector2(Mathf.Clamp(_ball.Position.X, FieldBounds.Position.X + 24f, FieldBounds.End.X - 24f), FieldBounds.End.Y - 28f));
            else if (_ball.Position.X < FieldBounds.Position.X)
                CallGoalLineRestart(0);
            else if (_ball.Position.X > FieldBounds.End.X)
                CallGoalLineRestart(1);
        }
    }

    private void CallThrowIn(Vector2 restartPosition)
    {
        int restartTeam = _ball?.LastTouchTeam == 0 ? 1 : 0;
        PlaceRestart("Tac", restartTeam, restartPosition, GetAttackDirection(restartTeam), 0.7f);
    }

    public void CallGoalLineRestart(int side)
    {
        int defendingTeam = TeamDefendingSide(side);
        int attackingTeam = 1 - defendingTeam;
        if (_ball?.LastTouchTeam == defendingTeam)
        {
            float cornerX = side == 0 ? FieldBounds.Position.X + 24f : FieldBounds.End.X - 24f;
            float cornerY = _ball.Position.Y < FieldBounds.GetCenter().Y ? FieldBounds.Position.Y + 24f : FieldBounds.End.Y - 24f;
            PlaceRestart("Korner", attackingTeam, new Vector2(cornerX, cornerY), GetAttackDirection(attackingTeam), 0.85f);
        }
        else
        {
            float goalKickX = side == 0 ? FieldBounds.Position.X + 92f : FieldBounds.End.X - 92f;
            PlaceGoalKick(defendingTeam, new Vector2(goalKickX, GoalCenterY()));
        }
    }

    private int TeamDefendingSide(int side)
    {
        float sideGoalX = side == 0 ? LeftGoalX : RightGoalX;
        for (int team = 0; team <= 1; team++)
            if (Mathf.IsEqualApprox(DefenseGoalXForTeam(team), sideGoalX))
                return team;
        return side == 0 ? 0 : 1;
    }

    private int TeamAttackingSide(int side)
    {
        float sideGoalX = side == 0 ? LeftGoalX : RightGoalX;
        for (int team = 0; team <= 1; team++)
            if (Mathf.IsEqualApprox(GoalXForTeam(team), sideGoalX))
                return team;
        return side == 0 ? 1 : 0;
    }

    private void PlaceRestart(string reason, int restartTeam, Vector2 restartPosition, Vector2 direction, float pause)
    {
        if (_ball == null)
            return;
        ShowRefereeMessage($"{reason} - {TeamName(restartTeam)}");
        _ball.Release();
        _ball.Position = ClampToField(restartPosition, 24f);
        _ball.Velocity = Vector2.Zero;
        _ball.Height = 0f;
        _ball.VerticalVelocity = 0f;
        _ball.FreeAfterKick = 0f;

        MatchActor? taker = FirstRestartTaker(restartTeam);
        if (taker != null)
        {
            taker.Position = ClampActorPosition(_ball.Position - direction * 30f, taker.Team, taker.IsGoalkeeper);
            _ball.Claim(taker, direction, true);
            _ball.Position = taker.Position + _ball.HoldOffset;
        }
        SpreadActorsAfterRestart(_ball.Position, restartTeam, direction, taker);
        if (reason == "Tac")
            ShapeThrowInTeams(_ball.Position, restartTeam, direction, taker);
        _goalPause = pause;
        _pauseAction = MatchPauseAction.None;
        _pendingGoalTeam = -1;
    }

    private void PlaceGoalKick(int restartTeam, Vector2 restartPosition)
    {
        if (_ball == null)
            return;
        Vector2 direction = GetAttackDirection(restartTeam);
        ShowRefereeMessage($"Aut - {TeamName(restartTeam)}");
        _ball.Release();
        _ball.Position = ClampToField(restartPosition, 50f);
        _ball.Velocity = Vector2.Zero;
        _ball.Height = 0f;
        _ball.VerticalVelocity = 0f;
        _ball.FreeAfterKick = 0f;

        MatchActor? keeper = TeamGoalkeeper(restartTeam);
        if (keeper != null)
        {
            keeper.Position = ClampActorPosition(_ball.Position - direction * 18f, keeper.Team, keeper.IsGoalkeeper);
            _ball.Claim(keeper, direction, true);
            _ball.Position = keeper.Position + _ball.HoldOffset;
        }
        SpreadActorsAfterRestart(_ball.Position, restartTeam, direction, keeper);
        ShapeGoalKickReceivingTeam(_ball.Position, restartTeam, direction);
        _goalPause = 0.85f;
        _pauseAction = MatchPauseAction.None;
        _pendingGoalTeam = -1;
    }

    private MatchActor? FirstRestartTaker(int team)
    {
        foreach (var actor in Actors)
            if (actor.Team == team && !actor.IsGoalkeeper && actor.Role == "support")
                return actor;
        foreach (var actor in Actors)
            if (actor.Team == team && !actor.IsGoalkeeper)
                return actor;
        return TeamGoalkeeper(team);
    }

    private void SpreadActorsAfterRestart(Vector2 anchor, int restartTeam, Vector2 direction, MatchActor? taker)
    {
        int index = 0;
        foreach (var actor in Actors)
        {
            if (actor == taker)
                continue;
            Vector2 target = actor.HomePosition;
            if (actor.Team == restartTeam && !actor.IsGoalkeeper)
            {
                float forward = actor.Role == "forward" ? 220f : actor.Role == "support" ? 132f : 86f;
                float lane = actor.Role == "forward" ? 18f : index % 2 == 0 ? 64f : -56f;
                target = anchor + direction * forward + new Vector2(0f, lane);
            }
            else if (actor.Team != restartTeam && !actor.IsGoalkeeper)
            {
                float back = actor.Role == "forward" ? 58f : actor.Role == "support" ? 122f : 96f;
                float lane = index % 2 == 0 ? 54f : -50f;
                target = anchor - direction * back + new Vector2(0f, lane);
            }
            actor.Position = ClampActorPosition(target, actor.Team, actor.IsGoalkeeper);
            index++;
        }
    }

    private void ShapeGoalKickReceivingTeam(Vector2 anchor, int restartTeam, Vector2 direction)
    {
        int receivingTeam = 1 - restartTeam;
        int index = 0;
        foreach (var actor in Actors)
        {
            if (actor.Team != receivingTeam || actor.IsGoalkeeper)
                continue;

            Vector2 target = actor.Role switch
            {
                "forward" => anchor + direction * 250f + new Vector2(0f, index % 2 == 0 ? -54f : 54f),
                "support" => anchor + direction * 330f + new Vector2(0f, index % 2 == 0 ? 70f : -70f),
                "defender" => anchor + direction * 415f + new Vector2(0f, index % 2 == 0 ? -82f : 82f),
                _ => anchor + direction * 300f
            };
            target.X = Mathf.Clamp(target.X, FieldBounds.Position.X + 270f, FieldBounds.End.X - 270f);
            target.Y = Mathf.Clamp(target.Y, FieldBounds.Position.Y + 78f, FieldBounds.End.Y - 78f);
            actor.Position = ClampActorPosition(target, actor.Team, actor.IsGoalkeeper);
            index++;
        }
    }

    private void ShapeThrowInTeams(Vector2 anchor, int restartTeam, Vector2 direction, MatchActor? taker)
    {
        Vector2 infield = anchor.Y < FieldBounds.GetCenter().Y ? Vector2.Down : Vector2.Up;
        int attackIndex = 0;
        int defendIndex = 0;

        if (taker != null)
        {
            Vector2 takerTarget = anchor - infield * 16f - direction * 8f;
            taker.Position = ClampActorPosition(takerTarget, taker.Team, taker.IsGoalkeeper);
            _ball?.Claim(taker, (direction * 0.72f + infield * 0.28f).Normalized(), true);
            if (_ball != null)
                _ball.Position = taker.Position + _ball.HoldOffset;
        }

        foreach (var actor in Actors)
        {
            if (actor == taker || actor.IsGoalkeeper)
                continue;

            Vector2 target;
            if (actor.Team == restartTeam)
            {
                target = actor.Role switch
                {
                    "forward" => anchor + direction * 180f + infield * 66f,
                    "support" => anchor + direction * 94f + infield * 38f,
                    "defender" => anchor - direction * 74f + infield * 90f,
                    _ => anchor + direction * 110f + infield * 54f
                };
                target += new Vector2(0f, (attackIndex % 2 == 0 ? 1f : -1f) * 18f);
                target = ClampThrowInLane(target, infield, 42f);
                attackIndex++;
            }
            else
            {
                target = actor.Role switch
                {
                    "forward" => anchor - direction * 34f + infield * 42f,
                    "support" => anchor + direction * 84f + infield * 56f,
                    "defender" => anchor + direction * 164f + infield * 84f,
                    _ => anchor + direction * 70f + infield * 58f
                };
                target += new Vector2(0f, (defendIndex % 2 == 0 ? -1f : 1f) * 16f);
                target = ClampThrowInLane(target, infield, 38f);
                defendIndex++;
            }

            actor.Position = ClampActorPosition(target, actor.Team, actor.IsGoalkeeper);
        }
    }

    private Vector2 ClampThrowInLane(Vector2 target, Vector2 infield, float sidelineMargin)
    {
        target.X = Mathf.Clamp(target.X, FieldBounds.Position.X + 42f, FieldBounds.End.X - 42f);
        if (infield == Vector2.Down)
            target.Y = Mathf.Clamp(target.Y, FieldBounds.Position.Y + sidelineMargin, FieldBounds.End.Y - 82f);
        else
            target.Y = Mathf.Clamp(target.Y, FieldBounds.Position.Y + 82f, FieldBounds.End.Y - sidelineMargin);
        return target;
    }

    private void RescueStuckBall()
    {
        if (_ball == null || _ball.Holder != null || _ball.Velocity.Length() > 35f || _ball.Height > 8f)
            return;
        if (!FieldBounds.Grow(-18f).HasPoint(_ball.Position))
            _ball.Position = ClampToField(_ball.Position, 22f);
    }

    private MatchActor? NearestActorToBall()
    {
        if (_ball == null)
            return null;
        MatchActor? best = null;
        float bestDistance = float.MaxValue;
        foreach (var actor in Actors)
        {
            if (actor.IsGoalkeeper)
                continue;
            float distance = actor.Position.DistanceSquaredTo(_ball.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = actor;
            }
        }
        return best;
    }

    private void ScoreGoal(int team)
    {
        _pendingGoalTeam = team;
        _pauseAction = MatchPauseAction.FinishGoalSequence;
        _goalPause = 2.0f;
        if (_ball != null)
        {
            _ball.Release();
            _ball.Velocity = Vector2.Zero;
            _ball.FreeAfterKick = 2.0f;
        }
        foreach (var actor in Actors)
            if (actor.Team == team)
                actor.StartGoalCelebration();
        ShowRefereeMessage($"GOOOL! {TeamName(team)} seviniyor");
    }

    private void FinishGoalSequence()
    {
        int scoringTeam = _pendingGoalTeam;
        if (scoringTeam == 0)
            _homeScore++;
        else if (scoringTeam == 1)
            _awayScore++;
        if (scoringTeam >= 0)
            _kickoffTeam = 1 - scoringTeam;
        _pendingGoalTeam = -1;
        _pauseAction = MatchPauseAction.None;
        ResetForKickoff("Gol");
    }

    private void StartHalftime()
    {
        _goalPause = 2.0f;
        _pauseAction = MatchPauseAction.StartSecondHalf;
        _pendingGoalTeam = -1;
        if (_ball != null)
        {
            _ball.Release();
            _ball.Velocity = Vector2.Zero;
            _ball.FreeAfterKick = 2.0f;
        }
        ShowRefereeMessage("Devre arasi");
    }

    private void BeginSecondHalf()
    {
        CurrentHalf = 2;
        _kickoffTeam = 1;
        MirrorHomePositionsForSecondHalf();
        ResetForKickoff("Ikinci yari");
    }

    private void MirrorHomePositionsForSecondHalf()
    {
        foreach (var actor in Actors)
            actor.SetHomePosition(new Vector2(LeftGoalX + RightGoalX - actor.HomePosition.X, actor.HomePosition.Y));
    }

    private void FinishMatch()
    {
        _finished = true;
        string result = GameManager.Instance.CompletePlayableMatch(_homeScore, _awayScore, PlayerMatchRating());
        DialogueManager.Instance.Show("Mahalle Maci", result, () => WorldManager.Instance.GoTo("World"));
    }

    private float PlayerMatchRating()
    {
        float rating = 6f + _homeScore * 0.45f - _awayScore * 0.25f;
        MatchActor? player = Actors.Find(a => a.Controlled);
        if (player != null)
            rating += Mathf.Clamp(player.Stamina / 100f, 0f, 1f) * 0.4f;
        return Mathf.Clamp(rating, 1f, 10f);
    }

    private void BuildUi()
    {
        var canvas = new CanvasLayer { Name = "HUD", Layer = 10 };
        AddChild(canvas);
        _scoreLabel = HudLabel(canvas, new Vector2(730, 84), new Vector2(360, 34), 16, HorizontalAlignment.Right);
        _refereeLabel = HudLabel(canvas, new Vector2(730, 118), new Vector2(360, 32), 14, HorizontalAlignment.Right);
        _helpLabel = HudLabel(canvas, new Vector2(285, 648), new Vector2(500, 42), 13, HorizontalAlignment.Center);
        _helpLabel.Text = "WASD: hareket | Shift: sprint | Space: sut | Q: pas | Shift+Q: havadan pas | R: pas iste | F: top kap";
        var left = BuildInfoPanel(canvas, new Vector2(96, 548));
        _leftInfoLabel = left.label;
        _leftStaminaFill = left.stamina;
        _leftShotFill = left.shot;
        var right = BuildInfoPanel(canvas, new Vector2(872, 548));
        _rightInfoLabel = right.label;
        _rightStaminaFill = right.stamina;
        _rightShotFill = right.shot;
        UpdateScoreLabel();
    }

    private static Label HudLabel(Node parent, Vector2 position, Vector2 size, int fontSize, HorizontalAlignment align)
    {
        var label = new Label
        {
            Position = position,
            Size = size,
            HorizontalAlignment = align,
            ZIndex = 20
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", Colors.White);
        parent.AddChild(label);
        return label;
    }

    private static (Label label, ColorRect stamina, ColorRect shot) BuildInfoPanel(Node parent, Vector2 position)
    {
        var panel = new ColorRect { Position = position, Size = new Vector2(152, 60), Color = new Color(0.04f, 0.06f, 0.05f, 0.72f) };
        parent.AddChild(panel);
        var label = HudLabel(parent, position + new Vector2(8, 4), new Vector2(136, 28), 12, HorizontalAlignment.Left);
        var staminaBack = new ColorRect { Position = position + new Vector2(12, 38), Size = new Vector2(128, 6), Color = new Color(0.02f, 0.02f, 0.02f, 0.85f) };
        parent.AddChild(staminaBack);
        var stamina = new ColorRect { Position = staminaBack.Position, Size = new Vector2(0, 6), Color = new Color(0.22f, 0.9f, 0.28f) };
        parent.AddChild(stamina);
        var shotBack = new ColorRect { Position = position + new Vector2(12, 48), Size = new Vector2(128, 5), Color = new Color(0.02f, 0.02f, 0.02f, 0.85f) };
        parent.AddChild(shotBack);
        var shot = new ColorRect { Position = shotBack.Position, Size = new Vector2(0, 5), Color = new Color(0.95f, 0.82f, 0.2f) };
        parent.AddChild(shot);
        return (label, stamina, shot);
    }

    private void UpdateScoreLabel()
    {
        if (_scoreLabel != null)
        {
            string half = CurrentHalf == 1 ? "1. Yari" : "2. Yari";
            _scoreLabel.Text = $"Mahalle 4v4 | {half} | {_homeScore} - {_awayScore} | {Mathf.Max(Mathf.CeilToInt(MatchDuration - _elapsed), 0)}s";
        }
    }

    private void UpdateInfoPanels()
    {
        UpdateTeamPanel(0, _leftInfoLabel, _leftStaminaFill, _leftShotFill);
        UpdateTeamPanel(1, _rightInfoLabel, _rightStaminaFill, _rightShotFill);
    }

    private void UpdateTeamPanel(int team, Label? label, ColorRect? staminaFill, ColorRect? shotFill)
    {
        if (label == null || staminaFill == null || shotFill == null)
            return;
        MatchActor? holder = _ball?.Holder != null && _ball.Holder.Team == team ? _ball.Holder : null;
        string teamName = TeamName(team);
        if (holder == null)
        {
            label.Text = $"{teamName}\nTop bosta";
            staminaFill.Size = new Vector2(0, 6);
            shotFill.Size = new Vector2(0, 5);
            return;
        }
        label.Text = $"{teamName} | {holder.Role}\n{holder.ActorName}";
        staminaFill.Size = new Vector2(128f * Mathf.Clamp(holder.Stamina / 100f, 0f, 1f), 6);
        shotFill.Size = new Vector2(128f * Mathf.Clamp(holder.ShotCharge, 0f, 1f), 5);
    }

    private void BuildPitch()
    {
        AddFieldBackdrop();
    }

    private void AddFieldBackdrop()
    {
        Texture2D? texture = GD.Load<Texture2D>("res://assets/places/match_field_backdrop.png");
        if (texture == null)
            return;

        var sprite = new Sprite2D
        {
            Name = "MatchFieldBackdrop",
            Texture = texture,
            Centered = false,
            Position = Vector2.Zero,
            ZIndex = -25
        };
        AddChild(sprite);
    }

    private void AddPitchTexture()
    {
        Texture2D? texture = GD.Load<Texture2D>("res://assets/places/match_pitch_texture.png");
        if (texture == null)
        {
            Rect(FieldBounds.Position, FieldBounds.Size, new Color(0.18f, 0.53f, 0.24f), "MatchPitchTextureFallback", -23);
            return;
        }

        var sprite = new Sprite2D
        {
            Name = "MatchPitchTexture",
            Texture = texture,
            Centered = false,
            Position = FieldBounds.Position,
            ZIndex = -23
        };
        AddChild(sprite);
    }

    private void AddPitchWear()
    {
        Rect(FieldBounds.Position + new Vector2(0, -13f), new Vector2(FieldBounds.Size.X, 12f), new Color(0.47f, 0.36f, 0.20f, 0.28f), "TopTouchlineDirt", -21);
        Rect(new Vector2(FieldBounds.Position.X, FieldBounds.End.Y + 1f), new Vector2(FieldBounds.Size.X, 14f), new Color(0.43f, 0.32f, 0.18f, 0.32f), "BottomTouchlineDirt", -21);
        Oval("CenterCircleWear", FieldBounds.GetCenter(), new Vector2(76f, 31f), new Color(0.33f, 0.42f, 0.18f, 0.24f), -21);
        Oval("LeftKeeperWear", new Vector2(LeftGoalX + 42f, GoalCenterY()), new Vector2(72f, 46f), new Color(0.44f, 0.34f, 0.19f, 0.32f), -21);
        Oval("RightKeeperWear", new Vector2(RightGoalX - 42f, GoalCenterY()), new Vector2(72f, 46f), new Color(0.44f, 0.34f, 0.19f, 0.32f), -21);
        Oval("LeftPenaltySpotWear", new Vector2(LeftGoalX + 88f, GoalCenterY()), new Vector2(34f, 15f), new Color(0.55f, 0.43f, 0.24f, 0.24f), -20);
        Oval("RightPenaltySpotWear", new Vector2(RightGoalX - 88f, GoalCenterY()), new Vector2(34f, 15f), new Color(0.55f, 0.43f, 0.24f, 0.24f), -20);

        for (int i = 0; i < 22; i++)
        {
            float x = FieldBounds.Position.X + 36f + (i * 83f) % (FieldBounds.Size.X - 72f);
            float y = FieldBounds.Position.Y + 42f + (i * 47f) % (FieldBounds.Size.Y - 84f);
            Vector2 size = new(10f + (i % 4) * 4f, 3f + (i % 3) * 2f);
            Color color = i % 2 == 0 ? new Color(0.13f, 0.40f, 0.18f, 0.25f) : new Color(0.28f, 0.62f, 0.25f, 0.22f);
            Rect(new Vector2(x, y), size, color, $"GrassFleck{i}", -20);
        }
    }

    private void AddGoalNet(bool left)
    {
        float goalX = left ? LeftGoalX : RightGoalX;
        float netX = left ? goalX - 42f : goalX + 8f;
        var rect = new Rect2(new Vector2(netX, GoalY.X - 18f), new Vector2(34f, GoalY.Y - GoalY.X + 36f));
        string prefix = left ? "Left" : "Right";
        Rect(rect.Position + new Vector2(left ? -4f : 4f, 8f), rect.Size, new Color(0.02f, 0.05f, 0.03f, 0.26f), $"{prefix}GoalNetShadow", -22);
        LineRect(rect, 2.5f, new Color(0.86f, 0.90f, 0.82f, 0.72f), $"{prefix}GoalNet");

        for (int i = 1; i <= 3; i++)
        {
            float x = rect.Position.X + i * rect.Size.X / 4f;
            Line(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), 1.5f, new Color(0.86f, 0.90f, 0.82f, 0.26f), $"{prefix}GoalNetVertical{i}", -18);
        }
        for (int i = 1; i <= 4; i++)
        {
            float y = rect.Position.Y + i * rect.Size.Y / 5f;
            Line(new Vector2(rect.Position.X, y), new Vector2(rect.End.X, y), 1.5f, new Color(0.86f, 0.90f, 0.82f, 0.25f), $"{prefix}GoalNetHorizontal{i}", -18);
        }
    }

    private void AddCornerFlags()
    {
        AddCornerFlag("CornerFlagTopLeft", FieldBounds.Position + new Vector2(-10f, -10f), false);
        AddCornerFlag("CornerFlagTopRight", new Vector2(FieldBounds.End.X + 10f, FieldBounds.Position.Y - 10f), true);
        AddCornerFlag("CornerFlagBottomLeft", new Vector2(FieldBounds.Position.X - 10f, FieldBounds.End.Y + 10f), false);
        AddCornerFlag("CornerFlagBottomRight", FieldBounds.End + new Vector2(10f, 10f), true);
    }

    private void AddCornerFlag(string name, Vector2 position, bool flip)
    {
        Rect(position + new Vector2(-1.5f, -18f), new Vector2(3f, 28f), new Color(0.86f, 0.78f, 0.45f), name + "Pole", -14);
        var flag = new Polygon2D
        {
            Name = name,
            ZIndex = -13,
            Color = new Color(0.88f, 0.16f, 0.12f)
        };
        float side = flip ? -1f : 1f;
        flag.Polygon = new[]
        {
            position + new Vector2(0f, -18f),
            position + new Vector2(side * 19f, -12f),
            position + new Vector2(0f, -6f)
        };
        AddChild(flag);
    }

    private void Rect(Vector2 position, Vector2 size, Color color, string name = "", int z = -20)
    {
        var rect = new ColorRect { Position = position, Size = size, Color = color, ZIndex = z };
        if (!string.IsNullOrEmpty(name))
            rect.Name = name;
        AddChild(rect);
    }

    private void Line(Vector2 from, Vector2 to, float width, Color color, string name, int z = -18)
    {
        var line = new Line2D { Name = name, Width = width, DefaultColor = color, ZIndex = z };
        line.AddPoint(from);
        line.AddPoint(to);
        AddChild(line);
    }

    private void LineRect(Rect2 rect, float width, Color color, string name = "")
    {
        var line = new Line2D { Width = width, DefaultColor = color, ZIndex = -18, Closed = true };
        if (!string.IsNullOrEmpty(name))
            line.Name = name;
        line.AddPoint(rect.Position);
        line.AddPoint(new Vector2(rect.End.X, rect.Position.Y));
        line.AddPoint(rect.End);
        line.AddPoint(new Vector2(rect.Position.X, rect.End.Y));
        AddChild(line);
    }

    private void Oval(string name, Vector2 center, Vector2 radius, Color color, int z)
    {
        var poly = new Polygon2D { Name = name, ZIndex = z, Color = color };
        int seg = 24;
        var pts = new Vector2[seg];
        for (int i = 0; i < seg; i++)
        {
            float angle = Mathf.Tau * i / seg;
            pts[i] = center + new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
        }
        poly.Polygon = pts;
        AddChild(poly);
    }

    private void CircleRing(Vector2 center, float radius, float width, Color color)
    {
        var line = new Line2D { Width = width, DefaultColor = color, ZIndex = -18, Closed = true };
        for (int i = 0; i < 36; i++)
        {
            float angle = Mathf.Tau * i / 36f;
            line.AddPoint(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        AddChild(line);
    }

    private void ArcRing(Vector2 center, float radius, float start, float end, float width, Color color, string name)
    {
        var line = new Line2D { Name = name, Width = width, DefaultColor = color, ZIndex = -18 };
        const int segments = 18;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(start, end, t);
            line.AddPoint(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        AddChild(line);
    }
}
