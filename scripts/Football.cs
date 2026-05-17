using Godot;

/// res://scripts/Football.cs
/// Top fiziği — RigidBody2D. _IntegrateForces ile kontrol/şut yönetimi.
public partial class Football : RigidBody2D
{
    public static Football? Instance { get; private set; }

    public FieldPlayer?  BallController       { get; private set; }
    public FieldPlayer?  LastToucher          { get; private set; }
    public FieldPlayer?  SecondLastToucher    { get; private set; }

    public bool IsControlled => BallController != null;
    public bool CanBeClaimed => !IsControlled && _claimCooldown <= 0f;

    private float   _claimCooldown   = 0f;
    private Vector2 _pendingVelocity = Vector2.Zero;

    public override void _Ready()
    {
        Instance      = this;
        LinearDamp    = 2.2f;
        AngularDamp   = 6f;
        GravityScale  = 0f;
        CollisionLayer = 4;
        CollisionMask  = 16; // walls only when free
        AddToGroup("ball");
    }

    public override void _Process(double delta)
    {
        if (_claimCooldown > 0f) _claimCooldown -= (float)delta;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
        if (BallController != null)
        {
            // Topu kontrolcünün önünde tut
            Vector2 dir = BallController.FacingDir;
            if (dir == Vector2.Zero)
                dir = BallController.PlayerTeam == FieldPlayer.Team.Red ? Vector2.Right : Vector2.Left;
            Vector2 target = BallController.GlobalPosition + dir * 25f;
            state.Transform       = new Transform2D(0f, target);
            state.LinearVelocity  = Vector2.Zero;
            state.AngularVelocity = 0f;
        }
        if (_pendingVelocity != Vector2.Zero)
        {
            state.LinearVelocity = _pendingVelocity;
            _pendingVelocity     = Vector2.Zero;
        }
    }

    public void GiveControl(FieldPlayer player)
    {
        if (IsControlled && BallController != player) return;
        BallController         = player;
        CollisionLayer         = 0;
        CollisionMask          = 0;
        SecondLastToucher      = LastToucher;
        LastToucher            = player;
    }

    public void Kick(Vector2 velocity)
    {
        BallController         = null;
        CollisionLayer         = 4;
        CollisionMask          = 16;
        _pendingVelocity       = velocity;
        _claimCooldown         = 0.32f;
    }

    public void ForceRelease()
    {
        BallController         = null;
        CollisionLayer         = 4;
        CollisionMask          = 16;
        _claimCooldown         = 0.18f;
    }

    public void ResetTo(Vector2 pos)
    {
        BallController         = null;
        CollisionLayer         = 4;
        CollisionMask          = 16;
        GlobalPosition         = pos;
        LinearVelocity         = Vector2.Zero;
        AngularVelocity        = 0f;
        _pendingVelocity       = Vector2.Zero;
        _claimCooldown         = 0.6f;
    }
}
