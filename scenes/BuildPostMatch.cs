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

        // Background — afternoon pitch sideline
        var bg = new ColorRect();
        bg.Name = "Background";
        bg.Color = new Color(0.3f, 0.65f, 0.3f);
        bg.Size = new Vector2(1280, 720);
        root.AddChild(bg);

        // Sky strip
        var sky = new ColorRect();
        sky.Name = "Sky";
        sky.Color = new Color(0.6f, 0.82f, 0.98f);
        sky.Size = new Vector2(1280, 260);
        root.AddChild(sky);

        // Coach Kemal Hoca
        var coach = new ColorRect();
        coach.Name = "CoachSprite";
        coach.Color = new Color(0.3f, 0.3f, 0.55f);
        coach.Size = new Vector2(55, 90);
        coach.Position = new Vector2(300, 350);
        root.AddChild(coach);

        var coachLabel = new Label();
        coachLabel.Name = "CoachLabel";
        coachLabel.Text = "Kemal Hoca";
        coachLabel.Position = new Vector2(282, 328);
        coachLabel.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(coachLabel);

        // Player sprite
        var player = new ColorRect();
        player.Name = "PlayerSprite";
        player.Color = new Color(0.92f, 0.72f, 0.55f);
        player.Size = new Vector2(40, 70);
        player.Position = new Vector2(750, 370);
        root.AddChild(player);

        // Eren and Baran in background
        var eren = new ColorRect();
        eren.Name = "Eren";
        eren.Color = new Color(0.7f, 0.5f, 0.3f);
        eren.Size = new Vector2(36, 62);
        eren.Position = new Vector2(880, 390);
        root.AddChild(eren);

        var baran = new ColorRect();
        baran.Name = "Baran";
        baran.Color = new Color(0.4f, 0.35f, 0.6f);
        baran.Size = new Vector2(36, 62);
        baran.Position = new Vector2(950, 390);
        root.AddChild(baran);

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
