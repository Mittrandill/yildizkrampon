using Godot;

/// res://scripts/FootballAI.cs
/// Steering AI for NPC football players. Attached to each NPC CharacterBody2D.
public partial class FootballAI : CharacterBody2D
{
    public enum Team { Red, Blue }

    [Export] public Team PlayerTeam = Team.Blue;
    [Export] public int SlotIndex = 0;  // 0-4 among team members

    public const float Speed = 140f;
    public const float SprintSpeedAI = 200f;
    public const float SeparationRadius = 30f;
    public const float SeparationForce = 80f;
    public const float KickRadius = 35f;
    public const float KickForce = 380f;

    private RigidBody2D? _ball;
    private float _kickCooldown = 0f;
    private Node2D? _ownGoal;
    private Node2D? _targetGoal;

    public override void _Ready()
    {
        // Derive team from groups added at build time (see scenes/BuildMatch.cs)
        PlayerTeam = IsInGroup("team_red") ? Team.Red : Team.Blue;
        var nameStr = Name.ToString();
        if (nameStr.Length > 0 && int.TryParse(nameStr[^1..], out int s))
            SlotIndex = s;

        _ball = GetTree().GetFirstNodeInGroup("ball") as RigidBody2D;
        _ownGoal = GetTree().GetFirstNodeInGroup(PlayerTeam == Team.Red ? "goal_red" : "goal_blue") as Node2D;
        _targetGoal = GetTree().GetFirstNodeInGroup(PlayerTeam == Team.Red ? "goal_blue" : "goal_red") as Node2D;
        CollisionLayer = 2;
        CollisionMask = 1 | 2 | 16;
    }

    public override void _PhysicsProcess(double delta)
    {
        _ball ??= GetTree().GetFirstNodeInGroup("ball") as RigidBody2D;
        if (_ball == null || _targetGoal == null) return;

        if (_kickCooldown > 0f)
            _kickCooldown -= (float)delta;

        Vector2 steeringTarget = _GetSteeringTarget();
        Vector2 dir = (steeringTarget - GlobalPosition).Normalized();
        float dist = GlobalPosition.DistanceTo(steeringTarget);

        float speed = dist < 5f ? 0f : Speed;
        var separation = _ComputeSeparation();
        Velocity = dir * speed + separation;
        MoveAndSlide();

        if (_kickCooldown <= 0f && GlobalPosition.DistanceTo(_ball.GlobalPosition) < KickRadius)
            _Kick();
    }

    private Vector2 _GetSteeringTarget()
    {
        if (_ball == null || _targetGoal == null) return GlobalPosition;

        float ballDist = GlobalPosition.DistanceTo(_ball.GlobalPosition);
        bool nearestToBall = _IsNearestToBall();

        if (nearestToBall)
            return _ball.GlobalPosition;

        // Spread: offset position relative to goal-ball axis
        Vector2 goalDir = (_targetGoal.GlobalPosition - _ball.GlobalPosition).Normalized();
        Vector2 perp = new Vector2(-goalDir.Y, goalDir.X);
        float spread = (SlotIndex - 2) * 80f;
        return _ball.GlobalPosition + goalDir * 60f + perp * spread;
    }

    private bool _IsNearestToBall()
    {
        if (_ball == null) return false;
        float myDist = GlobalPosition.DistanceTo(_ball.GlobalPosition);
        string group = PlayerTeam == Team.Red ? "team_red" : "team_blue";
        foreach (Node node in GetTree().GetNodesInGroup(group))
        {
            if (node == this) continue;
            if (node is Node2D n && n.GlobalPosition.DistanceTo(_ball.GlobalPosition) < myDist - 10f)
                return false;
        }
        return true;
    }

    private Vector2 _ComputeSeparation()
    {
        Vector2 force = Vector2.Zero;
        foreach (Node node in GetTree().GetNodesInGroup("team_red"))
            force += _SeparationFrom(node as Node2D);
        foreach (Node node in GetTree().GetNodesInGroup("team_blue"))
            force += _SeparationFrom(node as Node2D);
        return force;
    }

    private Vector2 _SeparationFrom(Node2D? other)
    {
        if (other == null || other == this) return Vector2.Zero;
        float dist = GlobalPosition.DistanceTo(other.GlobalPosition);
        if (dist > SeparationRadius || dist < 0.01f) return Vector2.Zero;
        return (GlobalPosition - other.GlobalPosition).Normalized() * SeparationForce * (1f - dist / SeparationRadius);
    }

    private void _Kick()
    {
        if (_ball == null || _targetGoal == null) return;
        Vector2 dir = (_targetGoal.GlobalPosition - _ball.GlobalPosition).Normalized();
        _ball.LinearVelocity = dir * KickForce;
        _kickCooldown = 0.6f;
    }
}
