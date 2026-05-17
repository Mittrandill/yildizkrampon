using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildNeighborhood.cs
public partial class BuildNeighborhood : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: neighborhood");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "Neighborhood";
        temp.AddChild(root);

        var bg = new Sprite2D();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/neighborhood_bg.png");
        bg.Position = new Vector2(640, 360);
        bg.ZIndex = -1;
        root.AddChild(bg);

        var rizaLabel = new Label();
        rizaLabel.Name = "RizaLabel";
        rizaLabel.Text = "Rıza Abi";
        rizaLabel.Position = new Vector2(848, 370);
        rizaLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.85f));
        root.AddChild(rizaLabel);

        var title = new Label();
        title.Name = "Title";
        title.Text = "Sabah — Mahalle";
        title.Position = new Vector2(20, 20);
        title.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(title);

        root.SetScript(GD.Load("res://scripts/NeighborhoodScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/neighborhood.tscn");
    }
}
