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

        var bg = new Sprite2D();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/next_morning_bg.png");
        bg.Position = new Vector2(640, 360);
        bg.ZIndex = -1;
        root.AddChild(bg);

        var sign = new Label();
        sign.Name = "AcademySign";
        sign.Text = "AKADEMİ";
        sign.Position = new Vector2(560, 330);
        sign.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        root.AddChild(sign);

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
