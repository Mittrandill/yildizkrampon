using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildShop.cs
public partial class BuildShop : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: shop");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "Shop";
        temp.AddChild(root);

        // Background — shop interior
        var bg = new ColorRect();
        bg.Name = "Background";
        bg.Color = new Color(0.88f, 0.82f, 0.7f);
        bg.Size = new Vector2(1280, 720);
        root.AddChild(bg);

        // Counter
        var counter = new ColorRect();
        counter.Name = "Counter";
        counter.Color = new Color(0.55f, 0.4f, 0.25f);
        counter.Size = new Vector2(600, 50);
        counter.Position = new Vector2(340, 420);
        root.AddChild(counter);

        // Shelves
        for (int i = 0; i < 4; i++)
        {
            var shelf = new ColorRect();
            shelf.Name = $"Shelf{i}";
            shelf.Color = new Color(0.65f, 0.5f, 0.32f);
            shelf.Size = new Vector2(300, 14);
            shelf.Position = new Vector2(880, 180 + i * 70);
            root.AddChild(shelf);
        }

        // Riza Abi
        var riza = new ColorRect();
        riza.Name = "RizaSprite";
        riza.Color = new Color(0.6f, 0.45f, 0.3f);
        riza.Size = new Vector2(55, 90);
        riza.Position = new Vector2(580, 310);
        root.AddChild(riza);

        var rizaLabel = new Label();
        rizaLabel.Name = "RizaLabel";
        rizaLabel.Text = "Rıza Abi";
        rizaLabel.Position = new Vector2(570, 290);
        rizaLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.1f, 0.05f));
        root.AddChild(rizaLabel);

        // Player
        var player = new ColorRect();
        player.Name = "PlayerSprite";
        player.Color = new Color(0.92f, 0.72f, 0.55f);
        player.Size = new Vector2(40, 70);
        player.Position = new Vector2(320, 330);
        root.AddChild(player);

        // Protein bar item display
        var barItem = new ColorRect();
        barItem.Name = "ProteinBar";
        barItem.Color = new Color(0.4f, 0.8f, 0.4f);
        barItem.Size = new Vector2(60, 30);
        barItem.Position = new Vector2(900, 260);
        root.AddChild(barItem);

        var barLabel = new Label();
        barLabel.Name = "ProteinBarLabel";
        barLabel.Text = "Protein Bar";
        barLabel.Position = new Vector2(895, 245);
        barLabel.AddThemeColorOverride("font_color", new Color(0.1f, 0.3f, 0.1f));
        root.AddChild(barLabel);

        var title = new Label();
        title.Name = "Title";
        title.Text = "Öğleden Sonra — Rıza'nın Bakkal";
        title.Position = new Vector2(20, 20);
        title.AddThemeColorOverride("font_color", new Color(0.2f, 0.1f, 0.05f));
        root.AddChild(title);

        root.SetScript(GD.Load("res://scripts/ShopScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/shop.tscn");
    }
}
