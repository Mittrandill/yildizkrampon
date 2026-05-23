using Godot;

public partial class WorldScene : Node2D
{
    private Player? _player;
    private Label?  _pitchHint;

    // Sync with BuildWorld.PitchGateWorld
    // ── Interaction hotspots ──────────────────────────────────────────────────
    // PitchGate: entrance on south fence of the stadium (world 1375, 524).
    // TownGate:  north-east area of map leading to town street (world 200, 200).
    private static readonly Vector2 PitchGate = new Vector2(1375f, 524f);
    private static readonly Vector2 TownGate  = new Vector2(200f,  200f);
    private const float INTERACT_DIST = 64f;

    public override void _Ready()
    {
        _player    = GetNodeOrNull<Player>("%Player");
        _pitchHint = GetNodeOrNull<Label>("%PitchHint");

        if (_player != null)
            _player.Interacted += _OnInteract;

        // Hide the old pitch world sprite (non-pixel-art, auto-named, centre ≈ (1386,377)).
        // We replace it with pixel art drawn directly into the terrain texture below.
        HideOldPitchSprite();

        // Pixel-art terrain — sits at ZIndex -10, same as the base green ColorRect
        // but added later so it renders on top, replacing the flat rect visually.
        // The semi-transparent grass-stripe overlays (ZIndex -9) and dirt paths
        // (ZIndex -8) from the scene still show correctly above the terrain.
        BuildPixelArtWorld();

        // Day/night visual layer (sky strip + CanvasModulate + clock).
        var dayNight = new DayNightLayer { ShowSky = true };
        AddChild(dayNight);
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;
        bool nearPitch = _player.GlobalPosition.DistanceTo(PitchGate) < INTERACT_DIST;
        bool nearTown  = _player.GlobalPosition.DistanceTo(TownGate)  < INTERACT_DIST;
        bool near = nearPitch || nearTown;
        if (_pitchHint != null)
        {
            _pitchHint.Visible = near;
            if (nearPitch) _pitchHint.Text = "[E] Mahalle Stadı";
            else if (nearTown) _pitchHint.Text = "[E] Kasaba Caddesi";
        }
    }

    private void _OnInteract()
    {
        if (_player == null) return;
        if (_player.GlobalPosition.DistanceTo(PitchGate) < INTERACT_DIST)
            WorldManager.Instance?.GoTo("NeighborhoodMatch");
        else if (_player.GlobalPosition.DistanceTo(TownGate) < INTERACT_DIST)
            WorldManager.Instance?.GoTo("TownStreet");
    }

    // ── Helper: hide the old non-pixel-art pitch sprite ──────────────────────
    private void HideOldPitchSprite()
    {
        // The Godot editor placed the pitch_world_sprite.png as a Sprite2D near
        // world position (1386, 377). Its node name is auto-generated, so we
        // find it by type + approximate position.
        foreach (var child in GetChildren())
        {
            if (child is Sprite2D spr && spr.Position.DistanceTo(new Vector2(1386f, 377f)) < 40f)
            {
                spr.Visible = false;
                break;
            }
        }
    }

    // ── Pixel-art world terrain ───────────────────────────────────────────────
    // Generates a 240 × 160 art-pixel image (8 px per block → 1920 × 1280 world).
    // Layers rendered above this image (from the scene's static ColorRects):
    //   ZIndex -9 — semi-transparent vertical grass stripe overlays
    //   ZIndex -8 — dirt path fills
    //   ZIndex -7 — path edge darkening strips
    private void BuildPixelArtWorld()
    {
        const int blockSz = 8;   // screen pixels per "art pixel"
        const int artW    = 240; // 240 × 8 = 1920
        const int artH    = 160; // 160 × 8 = 1280

        // Dirt path art-pixel ranges:
        //   Vertical   path: world x ∈ [944, 976]  → art x ∈ [118, 122]
        //   Horizontal path: world y ∈ [624, 656]  → art y ∈ [78, 82]
        const int vpX0 = 118, vpX1 = 122;
        const int hpY0 = 78,  hpY1 = 82;

        // ── Seasonal colour palette ───────────────────────────────────────────
        Color gL1, gL2, gD1, gD2; // grass: light-band pair + dark-band pair
        Color dL,  dD;             // dirt: light / dark checker

        var season = TimeManager.Instance?.CurrentSeason ?? TimeManager.Season.Spring;
        switch (season)
        {
            case TimeManager.Season.Summer:
                gL1 = new Color(0.267f, 0.608f, 0.157f); gL2 = new Color(0.239f, 0.549f, 0.137f);
                gD1 = new Color(0.200f, 0.478f, 0.114f); gD2 = new Color(0.176f, 0.427f, 0.098f);
                dL  = new Color(0.667f, 0.549f, 0.353f); dD  = new Color(0.588f, 0.467f, 0.290f);
                break;
            case TimeManager.Season.Autumn:
                gL1 = new Color(0.494f, 0.443f, 0.122f); gL2 = new Color(0.447f, 0.400f, 0.102f);
                gD1 = new Color(0.400f, 0.357f, 0.082f); gD2 = new Color(0.357f, 0.318f, 0.067f);
                dL  = new Color(0.635f, 0.510f, 0.318f); dD  = new Color(0.549f, 0.427f, 0.255f);
                break;
            case TimeManager.Season.Winter:
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

        const float mowBandArt = 14f; // mow band height in art pixels (14 × 8 = 112 world px)

        var img = Image.CreateEmpty(artW, artH, false, Image.Format.Rgba8);

        // ── Main terrain loop ─────────────────────────────────────────────────
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
                    // Dirt path: checker with tyre-track ruts.
                    bool ck = (ax + ay) % 2 == 0;
                    c = ck ? dL : dD;
                    if (isDirtV && ay % 5 == 0) c = c.Darkened(0.10f);
                    if (isDirtH && ax % 5 == 0) c = c.Darkened(0.08f);
                    if (isDirtV && isDirtH)      c = c.Lightened(0.06f); // crossroads
                }
                else
                {
                    // Grass: alternating mow bands with sub-pixel checker.
                    bool ck = (ax + ay) % 2 == 0;
                    c = mowLight
                        ? (ck ? gL1 : gL2)
                        : (ck ? gD1 : gD2);

                    if ((ax * 7 + ay * 5)  % 19 == 0) c = c.Lightened(0.07f);
                    if ((ax * 3 + ay * 11) % 23 == 0) c = c.Darkened(0.06f);

                    // Seasonal scatter pixels.
                    if (season == TimeManager.Season.Spring)
                    {
                        if ((ax * 13 + ay *  7) %  89 == 0) c = new Color(0.918f, 0.871f, 0.306f);
                        if ((ax * 11 + ay * 17) % 127 == 0) c = new Color(0.957f, 0.588f, 0.706f);
                    }
                    else if (season == TimeManager.Season.Autumn)
                    {
                        if ((ax *  7 + ay * 13) % 71 == 0) c = new Color(0.808f, 0.451f, 0.082f);
                        if ((ax * 11 + ay *  7) % 61 == 0) c = new Color(0.690f, 0.298f, 0.059f);
                    }
                    else if (season == TimeManager.Season.Winter)
                    {
                        if ((ax * 9 + ay * 11) % 37 == 0) c = new Color(0.96f, 0.98f, 1.00f);
                    }
                }

                img.SetPixel(ax, ay, c);
            }
        }

        // ── Stadium pixel-art zone ────────────────────────────────────────────
        // Painted ON TOP of the main terrain so it overwrites whatever grass was
        // there.  Fence collision bounds from World.tscn:
        //   West  fence: world x ≈ 1114-1124  → art x ≈ 139-140
        //   East  fence: world x ≈ 1626-1636  → art x ≈ 203-204
        //   North fence: world y ≈ 184-194    → art y ≈ 23
        //   Gate  entry: world (1375, 524)     → art (172, 65)  [south opening]
        PaintStadiumZone(img, season, dL, dD);

        // ── Town gate marker (north-west area, world 200,200 → art 25,25) ─────
        PaintTownGate(img, dL, dD);

        var tex = ImageTexture.CreateFromImage(img);
        AddChild(new Sprite2D
        {
            Name          = "WorldTerrainPixelArt",
            Texture       = tex,
            Centered      = false,
            Position      = Vector2.Zero,
            Scale         = new Vector2(blockSz, blockSz),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ZIndex        = -10
        });
    }

    // ── Stadium pixel-art painter ─────────────────────────────────────────────
    // Paints a Stardew Valley-style neighborhood stadium into the terrain image.
    //
    // Art-pixel layout (1 art px = 8 world px):
    //
    //  fY0=23 ─── perimeter wall ─────────────────────────────────── fY0
    //  24-26  ─── north bleacher seats (red/grey alternating rows)
    //  27-28  ─── north concrete approach strip
    //  pY0=29 ─── north touchline (white)
    //             ... pitch grass with mow bands + markings ...
    //  pY1=61 ─── south touchline (white)
    //  62-63  ─── south concrete (right of gate only)
    //  64-65  ─── south wall (right of gate) / open gate entrance (left of gate)
    //  gate centre: art x = 172  (world x = 1376)
    //
    private static void PaintStadiumZone(Image img, TimeManager.Season season,
                                          Color dirtL, Color dirtD)
    {
        // ── Layout constants ─────────────────────────────────────────────────
        const int fX0 = 139, fX1 = 204;  // outer fence X
        const int fY0 = 23,  fY1 = 65;   // outer fence Y
        const int pX0 = 143, pX1 = 200;  // pitch inner X (3-px concrete each side)
        const int pY0 = 29,  pY1 = 61;   // pitch inner Y
        const int pCX = 171, pCY = 45;   // pitch centre art px
        const int gateX = 172;            // gate centre X; left of this = entrance opening

        int imgW = img.GetWidth(), imgH = img.GetHeight();

        // ── Palette ──────────────────────────────────────────────────────────
        // Perimeter wall / fence
        var cWall  = new Color(0.267f, 0.208f, 0.141f);

        // Cobblestone sidewalk (Stardew-style 2-tone tile checker)
        var cCobL  = new Color(0.651f, 0.639f, 0.616f);  // light cobble
        var cCobD  = new Color(0.549f, 0.537f, 0.518f);  // dark cobble

        // Bleacher seats (north stand, behind the wall)
        var cSeatR = new Color(0.773f, 0.192f, 0.153f);  // red seat
        var cSeatG = new Color(0.467f, 0.455f, 0.439f);  // grey seat back

        // Pitch colours (seasonal, identical to NeighborhoodMatchController)
        Color pL1, pL2, pD1, pD2;
        switch (season)
        {
            case TimeManager.Season.Autumn:
                pL1 = new Color(0.494f, 0.443f, 0.122f); pL2 = new Color(0.447f, 0.400f, 0.102f);
                pD1 = new Color(0.400f, 0.357f, 0.082f); pD2 = new Color(0.357f, 0.318f, 0.067f);
                break;
            case TimeManager.Season.Winter:
                pL1 = new Color(0.843f, 0.875f, 0.855f); pL2 = new Color(0.808f, 0.839f, 0.820f);
                pD1 = new Color(0.773f, 0.804f, 0.784f); pD2 = new Color(0.737f, 0.769f, 0.749f);
                break;
            default: // Spring + Summer
                pL1 = new Color(0.353f, 0.710f, 0.141f); pL2 = new Color(0.318f, 0.643f, 0.122f);
                pD1 = new Color(0.255f, 0.569f, 0.094f); pD2 = new Color(0.220f, 0.510f, 0.078f);
                break;
        }
        var cLine = new Color(1f, 1f, 1f, 0.88f);  // white pitch markings

        // ── Paint ────────────────────────────────────────────────────────────
        for (int ay = fY0; ay <= fY1 && ay < imgH; ay++)
        for (int ax = fX0; ax <= fX1 && ax < imgW; ax++)
        {
            Color c;

            // ── Gate/entrance opening (south-west of gate centre) ─────────
            // This strip connects the world-map dirt path to the stadium gate.
            if (ay >= pY1 + 2 && ax < gateX)
            {
                bool ck = (ax + ay) % 2 == 0;
                img.SetPixel(ax, ay, ck ? dirtL : dirtD);
                continue;
            }

            // ── Perimeter wall ─────────────────────────────────────────────
            bool isWall = ax == fX0 || ax == fX1 || ay == fY0
                       || (ay >= pY1 + 1 && ax >= gateX); // partial south wall
            if (isWall) { img.SetPixel(ax, ay, cWall); continue; }

            // ── Pitch interior + border lines ──────────────────────────────
            bool onPitchBorderX = ay >= pY0 && ay <= pY1 && (ax == pX0 || ax == pX1);
            bool onPitchBorderY = ax >= pX0 && ax <= pX1 && (ay == pY0 || ay == pY1);
            bool inPitch        = ax > pX0 && ax < pX1 && ay > pY0 && ay < pY1;

            if (inPitch || onPitchBorderX || onPitchBorderY)
            {
                // Horizontal mow bands every 4 art px
                bool mow = ((ay - pY0) / 4) % 2 == 0;
                bool ck  = (ax + ay) % 2 == 0;
                c = mow ? (ck ? pL1 : pL2) : (ck ? pD1 : pD2);

                // White markings (priority order: border > lines > circle)
                if (onPitchBorderX || onPitchBorderY)
                {
                    c = cLine;
                }
                else
                {
                    float dx = ax - pCX, dy = ay - pCY;
                    float d  = Mathf.Sqrt(dx * dx + dy * dy);
                    if (ax == pCX)                                         c = cLine; // halfway line
                    else if (Mathf.Abs(d - 7f) < 0.85f || d < 1.3f)      c = cLine; // circle + spot
                    else if ((ay <= pY0 + 4 || ay >= pY1 - 4)             // goal area top/bottom
                          && ax >= pCX - 8 && ax <= pCX + 8)              c = cLine;
                }

                img.SetPixel(ax, ay, c);
                continue;
            }

            // ── North bleachers (between north wall and pitch) ─────────────
            if (ay < pY0 && ay > fY0 && ax > fX0 && ax < fX1)
            {
                // First 3 rows (y=24-26): coloured seats — red/grey alternating.
                // Last row before pitch (y=27-28): plain cobblestone approach.
                if (ay <= fY0 + 3)
                {
                    bool ck = ((ax - fX0) % 4 + (ay - fY0) * 2) % 4 < 2;
                    img.SetPixel(ax, ay, ck ? cSeatR : cSeatG);
                }
                else
                {
                    bool ck = ((ax >> 1) + (ay >> 1)) % 2 == 0;
                    img.SetPixel(ax, ay, ck ? cCobL : cCobD);
                }
                continue;
            }

            // ── Everything else: cobblestone sidewalk ──────────────────────
            // (east/west approaches, south concrete right of gate)
            {
                bool ck = ((ax >> 1) + (ay >> 1)) % 2 == 0;
                img.SetPixel(ax, ay, ck ? cCobL : cCobD);
            }
        }
    }

    // ── Town gate painter ─────────────────────────────────────────────────────
    // Draws a small cobblestone arch/gate at art (25, 25) — world (200, 200).
    // This marks the entrance to TownStreet on the world map.
    private static void PaintTownGate(Image img, Color dirtL, Color dirtD)
    {
        // Art-pixel centre: 200/8=25, 200/8=25
        const int gx = 22, gy = 22; // top-left of 8×8 gate art
        var cWall  = new Color(0.549f, 0.537f, 0.518f); // cobble pillars
        var cArch  = new Color(0.400f, 0.380f, 0.357f); // arch shadow
        var cSign  = new Color(0.157f, 0.318f, 0.682f); // blue accent
        var cPath  = dirtL;
        var cPathD = dirtD;

        // Dirt approach path leading south from gate
        for (int ay = gy + 6; ay < gy + 12 && ay < img.GetHeight(); ay++)
            for (int ax = gx + 2; ax < gx + 6 && ax < img.GetWidth(); ax++)
                img.SetPixel(ax, ay, (ax + ay) % 2 == 0 ? cPath : cPathD);

        // Gate pillars (2 px wide each side)
        for (int ay = gy; ay < gy + 7 && ay < img.GetHeight(); ay++)
        {
            if (gx < img.GetWidth())     img.SetPixel(gx,   ay, cWall);
            if (gx+1 < img.GetWidth())   img.SetPixel(gx+1, ay, cWall);
            if (gx+6 < img.GetWidth())   img.SetPixel(gx+6, ay, cWall);
            if (gx+7 < img.GetWidth())   img.SetPixel(gx+7, ay, cWall);
        }

        // Gate arch top (3 px bar)
        for (int ax = gx; ax < gx + 8 && ax < img.GetWidth(); ax++)
        {
            if (gy   < img.GetHeight()) img.SetPixel(ax, gy,   cArch);
            if (gy+1 < img.GetHeight()) img.SetPixel(ax, gy+1, cSign);
            if (gy+2 < img.GetHeight()) img.SetPixel(ax, gy+2, cArch);
        }

        // Gate opening interior (lighter dirt)
        for (int ay = gy + 3; ay < gy + 6 && ay < img.GetHeight(); ay++)
            for (int ax = gx + 2; ax < gx + 6 && ax < img.GetWidth(); ax++)
                img.SetPixel(ax, ay, cPath.Lightened(0.15f));
    }
}
