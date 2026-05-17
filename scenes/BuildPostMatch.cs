using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildPostMatch.cs
public partial class BuildPostMatch : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: postmatch");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "PostMatch";
        temp.AddChild(root);

        var bg = new Sprite2D();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/postmatch_bg.png");
        bg.Position = new Vector2(640, 360);
        bg.ZIndex = -1;
        root.AddChild(bg);

        var coachLabel = new Label();
        coachLabel.Name = "CoachLabel";
        coachLabel.Text = "Kemal Hoca";
        coachLabel.Position = new Vector2(282, 328);
        coachLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        root.AddChild(coachLabel);

        var title = new Label();
        title.Name = "Title";
        title.Text = "Öğleden Sonra — Saha Kenarı";
        title.Position = new Vector2(20, 20);
        title.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(title);

        root.SetScript(GD.Load("res://scripts/PostMatchScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/postmatch.tscn");
    }
}
