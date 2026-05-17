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

        // Background — pitch at dusk
        var bg = new ColorRect();
        bg.Name = "Background";
        bg.Color = new Color(0.22f, 0.5f, 0.22f);
        bg.Size = new Vector2(1280, 720);
        root.AddChild(bg);

        var sky = new ColorRect();
        sky.Name = "Sky";
        sky.Color = new Color(0.98f, 0.68f, 0.38f);
        sky.Size = new Vector2(1280, 240);
        root.AddChild(sky);

        // Goal visual
        var goal = new ColorRect();
        goal.Name = "Goal";
        goal.Color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
        goal.Size = new Vector2(16, 160);
        goal.Position = new Vector2(1180, 280);
        root.AddChild(goal);

        var goalTop = new ColorRect();
        goalTop.Name = "GoalTop";
        goalTop.Color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
        goalTop.Size = new Vector2(80, 16);
        goalTop.Position = new Vector2(1116, 280);
        root.AddChild(goalTop);

        // Coach
        var coach = new ColorRect();
        coach.Name = "CoachSprite";
        coach.Color = new Color(0.3f, 0.3f, 0.55f);
        coach.Size = new Vector2(50, 82);
        coach.Position = new Vector2(200, 350);
        root.AddChild(coach);

        var coachLabel = new Label();
        coachLabel.Name = "CoachLabel";
        coachLabel.Text = "Kemal Hoca";
        coachLabel.Position = new Vector2(182, 330);
        coachLabel.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(coachLabel);

        // Player
        var player = new ColorRect();
        player.Name = "PlayerSprite";
        player.Color = new Color(0.92f, 0.72f, 0.55f);
        player.Size = new Vector2(40, 70);
        player.Position = new Vector2(400, 355);
        root.AddChild(player);

        // Timing bar background
        var barBg = new ColorRect();
        barBg.Name = "BarBackground";
        barBg.Color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        barBg.Size = new Vector2(480, 30);
        barBg.Position = new Vector2(400, 540);
        root.AddChild(barBg);

        // Target zone (green hit area)
        var targetZone = new ColorRect();
        targetZone.Name = "TargetZone";
        targetZone.UniqueNameInOwner = true;
        targetZone.Color = new Color(0.2f, 0.85f, 0.2f, 0.6f);
        targetZone.Size = new Vector2(480 * 0.24f, 30);  // 24% of bar = 38-62%
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

        // Instruction label
        var instrLabel = new Label();
        instrLabel.Name = "InstructionLabel";
        instrLabel.UniqueNameInOwner = true;
        instrLabel.Text = "Şut antrenmanı! SPACE'e bas.";
        instrLabel.Position = new Vector2(440, 505);
        instrLabel.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(instrLabel);

        // Result label
        var resultLabel = new Label();
        resultLabel.Name = "ResultLabel";
        resultLabel.UniqueNameInOwner = true;
        resultLabel.Text = "";
        resultLabel.Position = new Vector2(580, 580);
        resultLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
        root.AddChild(resultLabel);

        // Shots label
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
