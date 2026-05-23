using Godot;

public partial class WorldScene : Node2D
{
    private Player? _player;
    private Label?  _pitchHint;

    // Sync with BuildWorld.PitchGateWorld
    private static readonly Vector2 PitchGate = new Vector2(1375f, 524f);
    private const float INTERACT_DIST = 64f;

    public override void _Ready()
    {
        _player    = GetNodeOrNull<Player>("%Player");
        _pitchHint = GetNodeOrNull<Label>("%PitchHint");

        if (_player != null)
            _player.Interacted += _OnInteract;

        // Pixel-art terrain — sits at ZIndex -10, same as the base green ColorRect
        // but added later in the tree so it renders on top, hiding the flat rect.
        // The semi-transparent grass-stripe overlays (ZIndex -9) and dirt paths
        // (ZIndex -8) defined in the scene still show correctly above the terrain.
        BuildPixelArtWorld();

        // Add the day/night visual layer (sky strip + CanvasModulate + clock).
        var dayNight = new DayNightLayer { ShowSky = true };
        AddChild(dayNight);
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;
        bool near = _player.GlobalPosition.DistanceTo(PitchGate) < INTERACT_DIST;
        if (_pitchHint != null) _pitchHint.Visible = near;
    }

    private void _OnInteract()
    {
        if (_player == null) return;
        if (_player.GlobalPosition.DistanceTo(PitchGate) < INTERACT_DIST)
            WorldManager.Instance?.GoTo("NeighborhoodMatch");
    }

    // ── Pixel-art world terrain ───────────────────────────────────────────────
    // Generates a 240 × 160 art-pixel image (8 px per block → 1920 × 1280 world).
    // Stardew Valley–style mow bands, seasonal colours, and dirt crossroads that
    // line up with the ColorRect paths already defined in World.tscn.
    private void BuildPixelArtWorld()
    {
        const int blockSz = 8;   // screen pixels per "art pixel"
        const int artW    = 240; // 240 × 8 = 1920
        const int artH    = 160; // 160 × 8 = 1280

        // Dirt path art-pixel ranges (from world coordinates):
        //   Vertical   path: world x ∈ [944, 976] → art x ∈ [118, 122]
        //   Horizontal path: world y ∈ [624, 656] → art y ∈ [78, 82]
        const int vpX0 = 118, vpX1 = 122;
        const int hpY0 = 78,  hpY1 = 82;

        // ── Seasonal colour palette ───────────────────────────────────────────
        Color gL1, gL2, gD1, gD2; // grass: light-band pair + dark-band pair
        Color dL,  dD;             // dirt: light / dark checker

        var season = TimeManager.Instance?.CurrentSeason ?? TimeManager.Season.Spring;
        switch (season)
        {
            case TimeManager.Season.Summer:
                // Warmer, slightly dry green.
                gL1 = new Color(0.267f, 0.608f, 0.157f); gL2 = new Color(0.239f, 0.549f, 0.137f);
                gD1 = new Color(0.200f, 0.478f, 0.114f); gD2 = new Color(0.176f, 0.427f, 0.098f);
                dL  = new Color(0.667f, 0.549f, 0.353f); dD  = new Color(0.588f, 0.467f, 0.290f);
                break;
            case TimeManager.Season.Autumn:
                // Yellow-brown dying grass.
                gL1 = new Color(0.494f, 0.443f, 0.122f); gL2 = new Color(0.447f, 0.400f, 0.102f);
                gD1 = new Color(0.400f, 0.357f, 0.082f); gD2 = new Color(0.357f, 0.318f, 0.067f);
                dL  = new Color(0.635f, 0.510f, 0.318f); dD  = new Color(0.549f, 0.427f, 0.255f);
                break;
            case TimeManager.Season.Winter:
                // Snow-dusted pale grass.
                gL1 = new Color(0.843f, 0.875f, 0.855f); gL2 = new Color(0.808f, 0.839f, 0.820f);
                gD1 = new Color(0.773f, 0.804f, 0.784f); gD2 = new Color(0.737f, 0.769f, 0.749f);
                dL  = new Color(0.710f, 0.718f, 0.729f); dD  = new Color(0.647f, 0.655f, 0.667f);
                break;
            default: // Spring — Stardew Valley lush vivid green.
                gL1 = new Color(0.278f, 0.627f, 0.153f); gL2 = new Color(0.247f, 0.565f, 0.133f);
                gD1 = new Color(0.208f, 0.498f, 0.106f); gD2 = new Color(0.180f, 0.443f, 0.090f);
                dL  = new Color(0.635f, 0.510f, 0.318f); dD  = new Color(0.549f, 0.427f, 0.255f);
                break;
        }

        // Mow band height in art pixels (14 art px × 8 = 112 world px per stripe).
        const float mowBandArt = 14f;

        var img = Image.CreateEmpty(artW, artH, false, Image.Format.Rgba8);

        for (int ay = 0; ay < artH; ay++)
        {
            bool isDirtH  = ay >= hpY0 && ay < hpY1;
            bool mowLight = ((int)(ay / mowBandArt)) % 2 == 0;

            for (int ax = 0; ax < artW; ax++)
            {
                bool isDirtV = ax >= vpX0 && ax < vpX1;
                Color c;

                if (isDirtH || isDirtV)
                {
                    // ── Dirt path ─────────────────────────────────────────────
                    bool ck = (ax + ay) % 2 == 0;
                    c = ck ? dL : dD;
                    // Tyre-track ruts parallel to path direction.
                    if (isDirtV && ay % 5 == 0) c = c.Darkened(0.10f);
                    if (isDirtH && ax % 5 == 0) c = c.Darkened(0.08f);
                    // Crossroads intersection: slightly lighter gravel.
                    if (isDirtV && isDirtH)      c = c.Lightened(0.06f);
                }
                else
                {
                    // ── Grass ─────────────────────────────────────────────────
                    bool ck = (ax + ay) % 2 == 0;
                    c = mowLight
                        ? (ck ? gL1 : gL2)
                        : (ck ? gD1 : gD2);

                    // Occasional bright grass blade.
                    if ((ax * 7 + ay * 5)  % 19 == 0) c = c.Lightened(0.07f);
                    // Occasional shadow cluster.
                    if ((ax * 3 + ay * 11) % 23 == 0) c = c.Darkened(0.06f);

                    // Seasonal detail pixels:
                    if (season == TimeManager.Season.Spring)
                    {
                        if ((ax * 13 + ay *  7) % 89  == 0) c = new Color(0.918f, 0.871f, 0.306f); // yellow dandelion
                        if ((ax * 11 + ay * 17) % 127 == 0) c = new Color(0.957f, 0.588f, 0.706f); // pink flower
                    }
                    else if (season == TimeManager.Season.Autumn)
                    {
                        if ((ax *  7 + ay * 13) % 71 == 0) c = new Color(0.808f, 0.451f, 0.082f); // orange leaf
                        if ((ax * 11 + ay *  7) % 61 == 0) c = new Color(0.690f, 0.298f, 0.059f); // red leaf
                    }
                    else if (season == TimeManager.Season.Winter)
                    {
                        if ((ax * 9 + ay * 11) % 37 == 0) c = new Color(0.96f, 0.98f, 1.00f); // snow sparkle
                    }
                }

                img.SetPixel(ax, ay, c);
            }
        }

        var tex = ImageTexture.CreateFromImage(img);
        AddChild(new Sprite2D
        {
            Name          = "WorldTerrainPixelArt",
            Texture       = tex,
            Centered      = false,
            Position      = Vector2.Zero,
            Scale         = new Vector2(blockSz, blockSz),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ZIndex        = -10   // same level as base ColorRect; drawn on top because added later
        });
    }
}
