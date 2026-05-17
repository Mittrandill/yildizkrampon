using Godot;

/// res://scripts/Football.cs
/// Ball physics. Attached to RigidBody2D.
public partial class Football : RigidBody2D
{
    public override void _Ready()
    {
        LinearDamp = 1.8f;
        AngularDamp = 3f;
        CollisionLayer = 4;
        CollisionMask = 1 | 2 | 16;
        AddToGroup("ball");
    }
}
