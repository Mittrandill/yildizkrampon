using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildNextMorning.cs
public partial class BuildNextMorning : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: next_morning");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "NextMorning";
        temp.AddChild(root);

        // Dawn gradient sky
        var sky = new ColorRect();
        sky.Name = "Sky";
        sky.Color = new Color(0.98f, 0.6f, 0.32f);
        sky.Size = new Vector2(1280, 500);
        root.AddChild(sky);

        // Ground
        var ground = new ColorRect();
        ground.Name = "Ground";
        ground.Color = new Color(0.3f, 0.2f, 0.12f);
        ground.Size = new Vector2(1280, 220);
        ground.Position = new Vector2(0, 500);
        root.AddChild(ground);

        // Academy silhouette (distant building)
        var academy = new ColorRect();
        academy.Name = "Academy";
        academy.Color = new Color(0.12f, 0.1f, 0.16f);
        academy.Size = new Vector2(400, 200);
        academy.Position = new Vector2(440, 300);
        root.AddChild(academy);

        // Academy sign
        var sign = new Label();
        sign.Name = "AcademySign";
        sign.Text = "AKADEMİ";
        sign.Position = new Vector2(560, 330);
        sign.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        root.AddChild(sign);

        // Player silhouette walking toward academy
        var player = new ColorRect();
        player.Name = "PlayerSprite";
        player.Color = new Color(0.12f, 0.1f, 0.16f);
        player.Size = new Vector2(32, 58);
        player.Position = new Vector2(200, 460);
        root.AddChild(player);

        // Sun
        var sun = new ColorRect();
        sun.Name = "Sun";
        sun.Color = new Color(1f, 0.92f, 0.2f);
        sun.Size = new Vector2(80, 80);
        sun.Position = new Vector2(1100, 60);
        root.AddChild(sun);

        // Title label
        var title = new Label();
        title.Name = "Title";
        title.Text = "Ertesi Sabah — Akademi Yolu";
        title.Position = new Vector2(20, 20);
        title.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(title);

        root.SetScript(GD.Load("res://scripts/NextMorningScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/next_morning.tscn");
    }
}
