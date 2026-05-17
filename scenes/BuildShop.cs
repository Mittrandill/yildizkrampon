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

        var bg = new Sprite2D();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/shop_bg.png");
        bg.Position = new Vector2(640, 360);
        bg.ZIndex = -1;
        root.AddChild(bg);

        var rizaLabel = new Label();
        rizaLabel.Name = "RizaLabel";
        rizaLabel.Text = "Rıza Abi";
        rizaLabel.Position = new Vector2(570, 290);
        rizaLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        root.AddChild(rizaLabel);

        var barLabel = new Label();
        barLabel.Name = "ProteinBarLabel";
        barLabel.Text = "Protein Bar";
        barLabel.Position = new Vector2(895, 245);
        barLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.9f, 0.2f));
        root.AddChild(barLabel);

        var title = new Label();
        title.Name = "Title";
        title.Text = "Öğleden Sonra — Rıza'nın Bakkal";
        title.Position = new Vector2(20, 20);
        title.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(title);

        root.SetScript(GD.Load("res://scripts/ShopScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/shop.tscn");
    }
}
