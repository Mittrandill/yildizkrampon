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

        // Background — dark warm bedroom
        var bg = new ColorRect();
        bg.Name = "Background";
        bg.Color = new Color(0.18f, 0.12f, 0.25f);
        bg.Size = new Vector2(1280, 720);
        root.AddChild(bg);

        // Bed
        var bed = new ColorRect();
        bed.Name = "Bed";
        bed.Color = new Color(0.55f, 0.28f, 0.18f);
        bed.Size = new Vector2(220, 110);
        bed.Position = new Vector2(900, 480);
        root.AddChild(bed);

        // Poster placeholders on wall
        var poster1 = new ColorRect();
        poster1.Name = "Poster1";
        poster1.Color = new Color(0.8f, 0.2f, 0.1f);
        poster1.Size = new Vector2(80, 110);
        poster1.Position = new Vector2(120, 150);
        root.AddChild(poster1);

        var poster2 = new ColorRect();
        poster2.Name = "Poster2";
        poster2.Color = new Color(0.1f, 0.4f, 0.8f);
        poster2.Size = new Vector2(80, 110);
        poster2.Position = new Vector2(220, 150);
        root.AddChild(poster2);

        // Old ball in corner
        var ballDeco = new ColorRect();
        ballDeco.Name = "OldBall";
        ballDeco.Color = new Color(0.9f, 0.88f, 0.85f);
        ballDeco.Size = new Vector2(30, 30);
        ballDeco.Position = new Vector2(80, 600);
        root.AddChild(ballDeco);

        // Player sprite in bed
        var playerSprite = new ColorRect();
        playerSprite.Name = "PlayerSprite";
        playerSprite.Color = new Color(0.92f, 0.72f, 0.55f);
        playerSprite.Size = new Vector2(40, 60);
        playerSprite.Position = new Vector2(910, 490);
        root.AddChild(playerSprite);

        // Scene title
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
