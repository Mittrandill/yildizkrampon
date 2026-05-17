using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildBedroom.cs
public partial class BuildBedroom : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: bedroom");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "Bedroom";
        temp.AddChild(root);

        var bg = new Sprite2D();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/bedroom_bg.png");
        bg.Position = new Vector2(640, 360);
        bg.ZIndex = -1;
        root.AddChild(bg);

        var title = new Label();
        title.Name = "Title";
        title.Text = "Sabah — Yatak Odası";
        title.Position = new Vector2(20, 20);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.7f));
        root.AddChild(title);

        root.SetScript(GD.Load("res://scripts/BedroomScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/bedroom.tscn");
    }
}
