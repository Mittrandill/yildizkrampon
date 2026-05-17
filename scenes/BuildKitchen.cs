using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildKitchen.cs
public partial class BuildKitchen : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: kitchen");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "Kitchen";
        temp.AddChild(root);

        var bg = new Sprite2D();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/kitchen_bg.png");
        bg.Position = new Vector2(640, 360);
        bg.ZIndex = -1;
        root.AddChild(bg);

        var momLabel = new Label();
        momLabel.Name = "MomLabel";
        momLabel.Text = "Anne";
        momLabel.Position = new Vector2(296, 310);
        momLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.1f, 0.3f));
        root.AddChild(momLabel);

        var dadLabel = new Label();
        dadLabel.Name = "DadLabel";
        dadLabel.Text = "Baba";
        dadLabel.Position = new Vector2(894, 310);
        dadLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.2f, 0.5f));
        root.AddChild(dadLabel);

        var title = new Label();
        title.Name = "Title";
        title.Text = "Sabah — Mutfak";
        title.Position = new Vector2(20, 20);
        title.AddThemeColorOverride("font_color", new Color(0.15f, 0.1f, 0.05f));
        root.AddChild(title);

        root.SetScript(GD.Load("res://scripts/KitchenScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/kitchen.tscn");
    }
}
