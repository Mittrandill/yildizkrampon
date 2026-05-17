using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildMatch.cs
public partial class BuildMatch : SceneBuilderBase
{
    // Pitch bounds
    private const float PitchLeft = 100f;
    private const float PitchRight = 1180f;
    private const float PitchTop = 80f;
    private const float PitchBottom = 640f;
    private const float GoalTop = 280f;
    private const float GoalBottom = 440f;

    public override void _Initialize()
    {
        GD.Print("Generating: match");

        var root = new Node2D();
        root.Name = "Match";

        // ---- Pitch background ----
        var pitchBg = new Sprite2D();
        pitchBg.Name = "PitchBG";
        pitchBg.Texture = GD.Load<Texture2D>("res://assets/img/pitch_bg.png");
        pitchBg.Position = new Vector2(640, 360);
        pitchBg.ZIndex = -1;
        root.AddChild(pitchBg);

        // ---- Static walls ----
        var walls = new StaticBody2D();
        walls.Name = "Walls";
        walls.CollisionLayer = 16;
        walls.CollisionMask = 0;
        _AddWall(walls, new Vector2(640, PitchTop), new Vector2(PitchRight - PitchLeft, 12));      // top
        _AddWall(walls, new Vector2(640, PitchBottom), new Vector2(PitchRight - PitchLeft, 12));   // bottom
        _AddWall(walls, new Vector2(PitchLeft, 180f), new Vector2(12, 200f));     // left top
        _AddWall(walls, new Vector2(PitchLeft, 540f), new Vector2(12, 200f));     // left bottom
        _AddWall(walls, new Vector2(PitchRight, 180f), new Vector2(12, 200f));    // right top
        _AddWall(walls, new Vector2(PitchRight, 540f), new Vector2(12, 200f));    // right bottom
        root.AddChild(walls);

        // ---- Goal areas ----
        var goalOpp = new Area2D();
        goalOpp.Name = "GoalOpponent";
        goalOpp.UniqueNameInOwner = true;
        goalOpp.CollisionLayer = 8;
        goalOpp.CollisionMask = 4;
        goalOpp.AddToGroup("goal_red");
        var goalOppShape = new CollisionShape2D();
        var goalOppRect = new RectangleShape2D();
        goalOppRect.Size = new Vector2(90, GoalBottom - GoalTop);
        goalOppShape.Shape = goalOppRect;
        goalOppShape.Position = new Vector2(55, 360);
        goalOpp.AddChild(goalOppShape);
        root.AddChild(goalOpp);

        var goalPlayer = new Area2D();
        goalPlayer.Name = "GoalPlayer";
        goalPlayer.UniqueNameInOwner = true;
        goalPlayer.CollisionLayer = 8;
        goalPlayer.CollisionMask = 4;
        goalPlayer.AddToGroup("goal_blue");
        var goalPlayerShape = new CollisionShape2D();
        var goalPlayerRect = new RectangleShape2D();
        goalPlayerRect.Size = new Vector2(90, GoalBottom - GoalTop);
        goalPlayerShape.Shape = goalPlayerRect;
        goalPlayerShape.Position = new Vector2(1225, 360);
        goalPlayer.AddChild(goalPlayerShape);
        root.AddChild(goalPlayer);

        // ---- Ball ----
        var ball = new RigidBody2D();
        ball.Name = "Ball";
        ball.Position = new Vector2(640, 360);
        ball.CollisionLayer = 4;
        ball.CollisionMask = 1 | 2 | 16;
        ball.GravityScale = 0;
        var ballShape = new CollisionShape2D();
        var ballCircle = new CircleShape2D();
        ballCircle.Radius = 10f;
        ballShape.Shape = ballCircle;
        ball.AddChild(ballShape);
        var ballSprite = new Sprite2D();
        ballSprite.Name = "BallSprite";
        ballSprite.Texture = GD.Load<Texture2D>("res://assets/img/football.png");
        ballSprite.Scale = new Vector2(0.18f, 0.18f);
        ball.AddChild(ballSprite);
        root.AddChild(ball);
        ball.SetScript(GD.Load("res://scripts/Football.cs"));

        // ---- Players container ----
        var players = new Node2D();
        players.Name = "Players";
        root.AddChild(players);

        _SetupPlayer(players, new Vector2(350, 360));

        // Red team NPCs (slots 0-3)
        Vector2[] redPositions = { new(280, 250), new(280, 470), new(450, 220), new(450, 500) };
        for (int i = 0; i < 4; i++)
        {
            var npc = _MakeNPC($"RedNPC{i}", "res://assets/img/npc_red.png", redPositions[i]);
            npc.AddToGroup("team_red");
            players.AddChild(npc);
            npc.SetScript(GD.Load("res://scripts/FootballAI.cs"));
        }

        // Blue team NPCs (slots 0-4)
        Vector2[] bluePositions = {
            new(930, 360), new(1000, 250), new(1000, 470), new(830, 220), new(830, 500)
        };
        for (int i = 0; i < 5; i++)
        {
            var npc = _MakeNPC($"BlueNPC{i}", "res://assets/img/npc_blue.png", bluePositions[i]);
            npc.AddToGroup("team_blue");
            players.AddChild(npc);
            npc.SetScript(GD.Load("res://scripts/FootballAI.cs"));
        }

        // ---- MatchManager ----
        var mm = new Node();
        mm.Name = "MatchManager";
        root.AddChild(mm);
        mm.SetScript(GD.Load("res://scripts/MatchManager.cs"));

        // ---- HUD CanvasLayer ----
        var hud = new CanvasLayer();
        hud.Name = "HUD";
        hud.Layer = 10;
        root.AddChild(hud);

        var hudControl = new Control();
        hudControl.Name = "HUDControl";
        hudControl.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        hud.AddChild(hudControl);

        // Score panel (top center)
        var scorePanel = new Panel();
        scorePanel.Name = "ScorePanel";
        scorePanel.Size = new Vector2(200, 50);
        scorePanel.Position = new Vector2(540, 10);
        hudControl.AddChild(scorePanel);

        var scoreLabel = new Label();
        scoreLabel.Name = "ScoreLabel";
        scoreLabel.UniqueNameInOwner = true;
        scoreLabel.Text = "0 - 0";
        scoreLabel.Position = new Vector2(60, 12);
        scorePanel.AddChild(scoreLabel);

        var timerLabel = new Label();
        timerLabel.Name = "TimerLabel";
        timerLabel.UniqueNameInOwner = true;
        timerLabel.Text = "02:00";
        timerLabel.Position = new Vector2(80, 30);
        scorePanel.AddChild(timerLabel);

        // Stats panel (top left)
        var statsPanel = new Panel();
        statsPanel.Name = "StatsPanel";
        statsPanel.Size = new Vector2(160, 80);
        statsPanel.Position = new Vector2(10, 10);
        hudControl.AddChild(statsPanel);

        var energyLabel = new Label();
        energyLabel.Name = "EnergyHUD";
        energyLabel.Text = "Enerji: 60";
        energyLabel.Position = new Vector2(8, 8);
        energyLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.9f, 0.2f));
        statsPanel.AddChild(energyLabel);

        var moraleLabel = new Label();
        moraleLabel.Name = "MoraleHUD";
        moraleLabel.Text = "Moral: 70";
        moraleLabel.Position = new Vector2(8, 30);
        moraleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.2f));
        statsPanel.AddChild(moraleLabel);

        var fatigueLabel = new Label();
        fatigueLabel.Name = "FatigueHUD";
        fatigueLabel.Text = "Yorgunluk: 20";
        fatigueLabel.Position = new Vector2(8, 52);
        fatigueLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.3f, 0.2f));
        statsPanel.AddChild(fatigueLabel);

        // Player indicator label
        var playerLabel = new Label();
        playerLabel.Name = "PlayerIndicator";
        playerLabel.Text = "▲ SEN";
        playerLabel.Position = new Vector2(330, 340);
        playerLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        root.AddChild(playerLabel);

        PackAndSave(root, "res://scenes/match.tscn");
    }

    private void _SetupPlayer(Node2D parent, Vector2 pos)
    {
        var p = new CharacterBody2D();
        p.Name = "Player";
        p.Position = pos;
        p.CollisionLayer = 1;
        p.CollisionMask = 1 | 2 | 16;

        var shape = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = 18f;
        shape.Shape = circle;
        p.AddChild(shape);

        var sprite = new Sprite2D();
        sprite.Name = "Sprite";
        sprite.Texture = GD.Load<Texture2D>("res://assets/img/player_sprite.png");
        sprite.Scale = new Vector2(0.20f, 0.20f);
        p.AddChild(sprite);

        var arrow = new Label();
        arrow.Name = "Arrow";
        arrow.Text = "★";
        arrow.Position = new Vector2(-6, -40);
        arrow.AddThemeColorOverride("font_color", Colors.Yellow);
        p.AddChild(arrow);

        parent.AddChild(p);
        p.SetScript(GD.Load("res://scripts/PlayerController.cs"));
    }

    private CharacterBody2D _MakeNPC(string name, string texturePath, Vector2 pos)
    {
        var npc = new CharacterBody2D();
        npc.Name = name;
        npc.Position = pos;

        var shape = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = 16f;
        shape.Shape = circle;
        npc.AddChild(shape);

        var sprite = new Sprite2D();
        sprite.Name = "Sprite";
        sprite.Texture = GD.Load<Texture2D>(texturePath);
        sprite.Scale = new Vector2(0.18f, 0.18f);
        npc.AddChild(sprite);

        return npc;
    }

    private void _AddWall(StaticBody2D parent, Vector2 center, Vector2 size)
    {
        var cs = new CollisionShape2D();
        var rect = new RectangleShape2D();
        rect.Size = size;
        cs.Shape = rect;
        cs.Position = center;
        parent.AddChild(cs);
    }
}
