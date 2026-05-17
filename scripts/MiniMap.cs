using Godot;

/// res://scripts/MiniMap.cs
/// Mini harita — saha oyuncularını ve topu renkli nokta olarak çizer.
public partial class MiniMap : Control
{
    private const float PITCH_W = FieldPlayer.PITCH_W;
    private const float PITCH_H = FieldPlayer.PITCH_H;

    // Ekran üzerinde mini haritanın boyutu
    private const float MAP_W = 240f;
    private const float MAP_H = 96f;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(MAP_W, MAP_H);
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Arka plan
        DrawRect(new Rect2(0, 0, MAP_W, MAP_H), new Color(0.12f, 0.38f, 0.12f));

        // Saha çizgileri
        DrawLine(new Vector2(MAP_W / 2f, 0), new Vector2(MAP_W / 2f, MAP_H), new Color(1,1,1,0.3f), 1f);
        DrawCircle(new Vector2(MAP_W / 2f, MAP_H / 2f), 12f, new Color(1,1,1,0.15f));

        // Oyuncuları çiz
        foreach (Node n in GetTree().GetNodesInGroup("team_red"))
        {
            if (n is FieldPlayer fp)
                _DrawDot(_WorldToMap(fp.GlobalPosition), new Color(0.9f, 0.2f, 0.2f));
        }
        foreach (Node n in GetTree().GetNodesInGroup("team_blue"))
        {
            if (n is FieldPlayer fp)
                _DrawDot(_WorldToMap(fp.GlobalPosition), new Color(0.2f, 0.4f, 0.9f));
        }
        foreach (Node n in GetTree().GetNodesInGroup("goalkeepers"))
        {
            if (n is GoalkeeperAI gk)
            {
                bool isRed = gk.GKTeam == GoalkeeperAI.Team.Red;
                _DrawDot(_WorldToMap(gk.GlobalPosition), isRed ? new Color(1f,0.5f,0.5f) : new Color(0.5f,0.5f,1f));
            }
        }

        // Topu çiz (sarı)
        if (Football.Instance != null)
            _DrawDot(_WorldToMap(Football.Instance.GlobalPosition), new Color(1f, 0.95f, 0.1f), 4f);

        // Kenarlık
        DrawRect(new Rect2(0, 0, MAP_W, MAP_H), new Color(1,1,1,0.5f), false, 1f);
    }

    private void _DrawDot(Vector2 pos, Color color, float radius = 3f)
    {
        DrawCircle(pos, radius, color);
    }

    private Vector2 _WorldToMap(Vector2 worldPos)
    {
        return new Vector2(
            Mathf.Clamp(worldPos.X / PITCH_W * MAP_W, 0, MAP_W),
            Mathf.Clamp(worldPos.Y / PITCH_H * MAP_H, 0, MAP_H)
        );
    }
}
