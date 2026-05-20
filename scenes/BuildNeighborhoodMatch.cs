using Godot;

public partial class BuildNeighborhoodMatch : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: NeighborhoodMatch");

        var temp = new Node();
        var root = new NeighborhoodMatchController { Name = "NeighborhoodMatch" };
        temp.AddChild(root);

        var camera = new Camera2D
        {
            Name = "Camera",
            Position = new Vector2(560, 340),
            Zoom = new Vector2(1.06f, 1.06f),
            LimitLeft = 0,
            LimitTop = 0,
            LimitRight = 1120,
            LimitBottom = 680
        };
        root.AddChild(camera);

        temp.RemoveChild(root);
        temp.Free();
        PackAndSave(root, "res://scenes/NeighborhoodMatch.tscn");
        Quit();
    }
}
