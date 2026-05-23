using Godot;

/// Node2D child that draws a filled semi-transparent region polygon.
/// Add as a child of any Area2D to get editor/test visualisation.
/// Parent Area2D's Visible controls whether this draws.
public partial class DebugRegionDraw : Node2D
{
    public Vector2[] Points       = System.Array.Empty<Vector2>();
    public Color     Fill         = new Color(0f, 1f, 0f, 0.18f);
    public Color     Border       = new Color(0f, 1f, 0f, 0.85f);
    public string    RegionLabel  = "";
    public float     BorderWidth  = 2f;

    public override void _Draw()
    {
        if (Points.Length < 3) return;

        // Filled interior
        DrawColoredPolygon(Points, Fill);

        // Closed border line
        var closed = new Vector2[Points.Length + 1];
        Points.CopyTo(closed, 0);
        closed[Points.Length] = Points[0];
        DrawPolyline(closed, Border, BorderWidth, false);

        // Label at centroid
        if (RegionLabel.Length > 0)
        {
            var cx = 0f; var cy = 0f;
            foreach (var p in Points) { cx += p.X; cy += p.Y; }
            cx /= Points.Length; cy /= Points.Length;

            DrawString(ThemeDB.FallbackFont, new Vector2(cx - 40, cy + 6),
                       RegionLabel, HorizontalAlignment.Left,
                       -1, 11, new Color(1, 1, 1, 0.9f));
        }
    }
}
