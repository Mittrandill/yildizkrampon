using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildEodSummary.cs
public partial class BuildEodSummary : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: eod_summary");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "EodSummary";
        temp.AddChild(root);

        var bg = new Sprite2D();
        bg.Name = "Background";
        bg.Texture = GD.Load<Texture2D>("res://assets/img/eod_summary_bg.png");
        bg.Position = new Vector2(640, 360);
        bg.ZIndex = -1;
        root.AddChild(bg);

        // Semi-transparent stat panel so text is readable over the parchment
        var statsPanel = new ColorRect();
        statsPanel.Name = "StatsPanel";
        statsPanel.Color = new Color(0.05f, 0.04f, 0.02f, 0.72f);
        statsPanel.Size = new Vector2(340, 340);
        statsPanel.Position = new Vector2(80, 105);
        root.AddChild(statsPanel);

        var header = new Label();
        header.Name = "Header";
        header.Text = "GÜN SONU ÖZETİ";
        header.Position = new Vector2(480, 40);
        header.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        root.AddChild(header);

        _AddStatRow(root, "Enerji", "EnergyValue", new Vector2(100, 130));
        _AddStatRow(root, "Moral", "MoraleValue", new Vector2(100, 165));
        _AddStatRow(root, "Yorgunluk", "FatigueValue", new Vector2(100, 200));
        _AddStatRow(root, "Şut Gücü", "ShotValue", new Vector2(100, 245));
        _AddStatRow(root, "Sprint", "SprintValue", new Vector2(100, 280));
        _AddStatRow(root, "Teknik", "TechValue", new Vector2(100, 315));
        _AddStatRow(root, "GENEL", "OverallValue", new Vector2(100, 360));

        var eventsPanel = new ColorRect();
        eventsPanel.Name = "EventsPanel";
        eventsPanel.Color = new Color(0.02f, 0.06f, 0.02f, 0.72f);
        eventsPanel.Size = new Vector2(560, 380);
        eventsPanel.Position = new Vector2(460, 105);
        root.AddChild(eventsPanel);

        var eventsHeader = new Label();
        eventsHeader.Name = "EventsHeader";
        eventsHeader.Text = "BUGÜN NE OLDU:";
        eventsHeader.Position = new Vector2(480, 120);
        eventsHeader.AddThemeColorOverride("font_color", new Color(0.6f, 0.95f, 0.6f));
        root.AddChild(eventsHeader);

        var eventsLabel = new Label();
        eventsLabel.Name = "EventsLabel";
        eventsLabel.UniqueNameInOwner = true;
        eventsLabel.Text = "";
        eventsLabel.Position = new Vector2(480, 148);
        eventsLabel.Size = new Vector2(520, 320);
        eventsLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        eventsLabel.AddThemeColorOverride("font_color", Colors.White);
        root.AddChild(eventsLabel);

        var continueLabel = new Label();
        continueLabel.Name = "ContinueLabel";
        continueLabel.UniqueNameInOwner = true;
        continueLabel.Text = "Enter'a bas — devam et";
        continueLabel.Position = new Vector2(500, 660);
        continueLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 1f));
        root.AddChild(continueLabel);

        root.SetScript(GD.Load("res://scripts/EodSummaryScene.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/eod_summary.tscn");
    }

    private void _AddStatRow(Node2D root, string statName, string uniqueValueName, Vector2 pos)
    {
        var nameLabel = new Label();
        nameLabel.Name = $"{uniqueValueName}Name";
        nameLabel.Text = $"{statName}:";
        nameLabel.Position = pos;
        nameLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.78f, 0.95f));
        root.AddChild(nameLabel);

        var valueLabel = new Label();
        valueLabel.Name = uniqueValueName;
        valueLabel.UniqueNameInOwner = true;
        valueLabel.Text = "—";
        valueLabel.Position = new Vector2(pos.X + 200, pos.Y);
        valueLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.4f));
        root.AddChild(valueLabel);
    }
}
