using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildNeighborhood.cs
public partial class BuildNeighborhood : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: neighborhood");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "Neighborhood";
        temp.AddChild(root);

        // Sky
        var sky = new ColorRect();
        sky.Name = "Sky";
        sky.Color = new Color(0.55f, 0.78f, 0.98f);
        sky.Size = new Vector2(1280, 400);
        root.AddChild(sky);

        // Ground / cobblestone street
        var ground = new ColorRect();
        ground.Name = "Ground";
        ground.Color = new Color(0.72f, 0.68f, 0.62f);
        ground.Size = new Vector2(1280, 320);
        ground.Position = new Vector2(0, 400);
        root.AddChild(ground);

        // Riza's shop building
        var shopBuilding = new ColorRect();
        shopBuilding.Name = "RizaShop";
        shopBuilding.Color = new Color(0.92f, 0.78f, 0.58f);
        shopBuilding.Size = new Vector2(220, 280);
        shopBuilding.Position = new Vector2(900, 200);
        root.AddChild(shopBuilding);

        var shopSign = new Label();
        shopSign.Name = "ShopSign";
        shopSign.Text = "RIZA'NIN BAKKAL";
        shopSign.Position = new Vector2(908, 220);
        shopSign.AddThemeColorOverride("font_color", new Color(0.2f, 0.1f, 0.05f));
        root.AddChild(shopSign);

        // Riza Abi NPC
        var riza = new ColorRect();
        riza.Name = "RizaAbi";
        riza.Color = new Color(0.6f, 0.45f, 0.3f);
        riza.Size = new Vector2(48, 80);
        riza.Position = new Vector2(860, 390);
        root.AddChild(riza);

        var rizaLabel = new Label();
        rizaLabel.Name = "RizaLabel";
        rizaLabel.Text = "Rıza Abi";
        rizaLabel.Position = new Vector2(848, 370);
        rizaLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.1f, 0.05f));
        root.AddChild(rizaLabel);

        // Player
        var player = new ColorRect();
        player.Name = "PlayerSprite";
        player.Color = new Color(0.92f, 0.72f, 0.55f);
        player.Size = new Vector2(40, 70);
        player.Position = new Vector2(400, 400);
        root.AddChild(player);

        var title = new Label();
        title.Name = "Title";
        title.Text = "Sabah — Mahalle";
        title.Position = new Vector2(20, 20);
        title.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.3f));
        root.AddChild(title);

        root.SetScript(GD.Load("res://scripts/NeighborhoodScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/neighborhood.tscn");
    }
}
