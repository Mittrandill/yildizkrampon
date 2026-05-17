using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildTraining.cs
public partial class BuildTraining : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: training");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "Training";
        temp.AddChild(root);

        var bg = new Sprite2D();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/training_bg.png");
        bg.Position = new Vector2(640, 360);
        bg.ZIndex = -1;
        root.AddChild(bg);

        var coachLabel = new Label();
        coachLabel.Name = "CoachLabel";
        coachLabel.Text = "Kemal Hoca";
        coachLabel.Position = new Vector2(182, 330);
        coachLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        root.AddChild(coachLabel);

        // Timing bar background
        var barBg = new ColorRect();
        barBg.Name = "BarBackground";
        barBg.Color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        barBg.Size = new Vector2(480, 30);
        barBg.Position = new Vector2(400, 540);
        root.AddChild(barBg);

        // Target zone (green hit area)
        var targetZone = new ColorRect();
        targetZone.Name = "TargetZone";
        targetZone.UniqueNameInOwner = true;
        targetZone.Color = new Color(0.2f, 0.85f, 0.2f, 0.7f);
        targetZone.Size = new Vector2(480 * 0.24f, 30);
        targetZone.Position = new Vector2(400 + 480 * 0.38f, 540);
        root.AddChild(targetZone);

        // Moving marker
        var marker = new ColorRect();
        marker.Name = "Marker";
        marker.UniqueNameInOwner = true;
        marker.Color = new Color(1f, 0.85f, 0.1f);
        marker.Size = new Vector2(10, 30);
        marker.Position = new Vector2(400, 540);
        root.AddChild(marker);

        var instrLabel = new Label();
        instrLabel.Name = "InstructionLabel";
        instrLabel.UniqueNameInOwner = true;
        instrLabel.Text = "Şut antrenmanı! SPACE'e bas.";
        instrLabel.Position = new Vector2(440, 505);
        instrLabel.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(instrLabel);

        var resultLabel = new Label();
        resultLabel.Name = "ResultLabel";
        resultLabel.UniqueNameInOwner = true;
        resultLabel.Text = "";
        resultLabel.Position = new Vector2(580, 580);
        resultLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
        root.AddChild(resultLabel);

        var shotsLabel = new Label();
        shotsLabel.Name = "ShotsLabel";
        shotsLabel.UniqueNameInOwner = true;
        shotsLabel.Text = "Şutlar: 5 kaldı";
        shotsLabel.Position = new Vector2(580, 610);
        shotsLabel.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(shotsLabel);

        var title = new Label();
        title.Name = "Title";
        title.Text = "Akşam — Şut Antrenmanı";
        title.Position = new Vector2(20, 20);
        title.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(title);

        root.SetScript(GD.Load("res://scripts/TrainingScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/training.tscn");
    }
}
