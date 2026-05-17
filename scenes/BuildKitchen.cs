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

        // Background — warm yellow kitchen
        var bg = new ColorRect();
        bg.Name = "Background";
        bg.Color = new Color(0.98f, 0.92f, 0.72f);
        bg.Size = new Vector2(1280, 720);
        root.AddChild(bg);

        // Table
        var table = new ColorRect();
        table.Name = "Table";
        table.Color = new Color(0.72f, 0.48f, 0.28f);
        table.Size = new Vector2(400, 80);
        table.Position = new Vector2(440, 420);
        root.AddChild(table);

        // Mom NPC placeholder
        var mom = new ColorRect();
        mom.Name = "Mom";
        mom.Color = new Color(0.85f, 0.6f, 0.8f);
        mom.Size = new Vector2(48, 80);
        mom.Position = new Vector2(300, 330);
        root.AddChild(mom);

        var momLabel = new Label();
        momLabel.Name = "MomLabel";
        momLabel.Text = "Anne";
        momLabel.Position = new Vector2(296, 310);
        momLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.1f, 0.3f));
        root.AddChild(momLabel);

        // Dad NPC placeholder
        var dad = new ColorRect();
        dad.Name = "Dad";
        dad.Color = new Color(0.5f, 0.65f, 0.85f);
        dad.Size = new Vector2(48, 80);
        dad.Position = new Vector2(900, 330);
        root.AddChild(dad);

        var dadLabel = new Label();
        dadLabel.Name = "DadLabel";
        dadLabel.Text = "Baba";
        dadLabel.Position = new Vector2(894, 310);
        dadLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.2f, 0.5f));
        root.AddChild(dadLabel);

        // Player sprite
        var player = new ColorRect();
        player.Name = "PlayerSprite";
        player.Color = new Color(0.92f, 0.72f, 0.55f);
        player.Size = new Vector2(40, 70);
        player.Position = new Vector2(620, 340);
        root.AddChild(player);

        var title = new Label();
        title.Name = "Title";
        title.Text = "Sabah — Mutfak";
        title.Position = new Vector2(20, 20);
        title.AddThemeColorOverride("font_color", new Color(0.3f, 0.2f, 0.1f));
        root.AddChild(title);

        root.SetScript(GD.Load("res://scripts/KitchenScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/kitchen.tscn");
    }
}
