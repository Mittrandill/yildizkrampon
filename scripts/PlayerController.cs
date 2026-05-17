using Godot;

/// res://scripts/PlayerController.cs
/// Player-controlled character for the 5v5 match scene.
public partial class PlayerController : CharacterBody2D
{
    public const float BaseSpeed = 180f;
    public const float SprintSpeed = 270f;
    public const float KickForce = 500f;
    public const float KickRadius = 40f;

    private RigidBody2D? _ball;
    private bool _canKick = true;
    private float _kickCooldown = 0f;

    public override void _Ready()
    {
        _ball = GetTree().GetFirstNodeInGroup("ball") as RigidBody2D;
        CollisionLayer = 1;
        CollisionMask = 1 | 2 | 8 | 16;
    }

    public override void _PhysicsProcess(double delta)
    {
        _ball ??= GetTree().GetFirstNodeInGroup("ball") as RigidBody2D;

        if (_kickCooldown > 0f)
        {
            _kickCooldown -= (float)delta;
            if (_kickCooldown <= 0f) _canKick = true;
        }

        var dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        bool sprinting = Input.IsActionPressed("sprint");
        float speed = sprinting ? SprintSpeed : BaseSpeed;

        // Apply energy modifier from GameManager
        float energyMod = Mathf.Clamp(GameManager.Instance.Energy / 100f, 0.4f, 1.0f);
        Velocity = dir * speed * energyMod;
        MoveAndSlide();

        if (Input.IsActionJustPressed("action") && _canKick)
            _TryKick();
    }

    private void _TryKick()
    {
        if (_ball == null) return;
        float dist = GlobalPosition.DistanceTo(_ball.GlobalPosition);
        if (dist > KickRadius) return;

        var direction = (_ball.GlobalPosition - GlobalPosition).Normalized();
        _ball.LinearVelocity = direction * KickForce;
        _canKick = false;
        _kickCooldown = 0.4f;

        GameManager.Instance.Energy = Mathf.Max(0, GameManager.Instance.Energy - 1);
        GameManager.Instance.Fatigue = Mathf.Min(100, GameManager.Instance.Fatigue + 1);
    }
}
