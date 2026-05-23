using Godot;
using static TownMapData;

/// Generates pixel-art ImageTextures for every tile, building, and decoration
/// in the TownStreet scene.  All art is 16 × 16 art-px (displayed at 2× scale).
public static class TownTileGen
{
    // ── Shared palette ────────────────────────────────────────────────────────
    private static readonly Color CGrassL  = new(0.278f, 0.627f, 0.153f);
    private static readonly Color CGrassD  = new(0.247f, 0.565f, 0.133f);
    private static readonly Color CCobL    = new(0.635f, 0.624f, 0.600f);
    private static readonly Color CCobD    = new(0.549f, 0.537f, 0.514f);
    private static readonly Color CSidewL  = new(0.729f, 0.714f, 0.686f);
    private static readonly Color CSidewD  = new(0.655f, 0.639f, 0.612f);
    private static readonly Color CRoadL   = new(0.373f, 0.373f, 0.380f);
    private static readonly Color CRoadD   = new(0.314f, 0.314f, 0.322f);
    private static readonly Color CPlazaL  = new(0.784f, 0.757f, 0.706f);
    private static readonly Color CPlazaD  = new(0.706f, 0.682f, 0.635f);
    private static readonly Color CFlowerL = new(0.278f, 0.627f, 0.153f);
    private static readonly Color CFlowerD = new(0.247f, 0.565f, 0.133f);
    private static readonly Color CDirtL   = new(0.635f, 0.510f, 0.318f);
    private static readonly Color CDirtD   = new(0.549f, 0.427f, 0.255f);

    // ── Season-aware colours ─────────────────────────────────────────────────
    private static Color SeasonGrassL(TimeManager.Season s) => s switch
    {
        TimeManager.Season.Summer => new(0.267f, 0.608f, 0.157f),
        TimeManager.Season.Autumn => new(0.494f, 0.443f, 0.122f),
        TimeManager.Season.Winter => new(0.843f, 0.875f, 0.855f),
        _                         => CGrassL,
    };
    private static Color SeasonGrassD(TimeManager.Season s) => s switch
    {
        TimeManager.Season.Summer => new(0.200f, 0.478f, 0.114f),
        TimeManager.Season.Autumn => new(0.400f, 0.357f, 0.082f),
        TimeManager.Season.Winter => new(0.773f, 0.804f, 0.784f),
        _                         => CGrassD,
    };

    // ── Ground tile textures ──────────────────────────────────────────────────
    public static ImageTexture MakeGroundTile(Ground g, TimeManager.Season season,
                                               int col, int row)
    {
        var img = Img();
        Color cL, cD;

        switch (g)
        {
            case Ground.Cobblestone:
                cL = CCobL; cD = CCobD;
                Fill2x2(img, cL, cD);
                break;

            case Ground.Sidewalk:
                cL = CSidewL; cD = CSidewD;
                FillChecker(img, cL, cD);
                // Edge line on one side
                for (int x = 0; x < TileArt; x++) img.SetPixel(x, 0, cD.Darkened(0.12f));
                break;

            case Ground.Road:
                cL = CRoadL; cD = CRoadD;
                FillSolid(img, cL);
                // Dashed centre line
                if (row == 14) // middle of 3-row road
                    for (int x = 0; x < TileArt; x += 4)
                    {
                        img.SetPixel(x,   7, new Color(0.94f, 0.89f, 0.27f));
                        img.SetPixel(x+1, 7, new Color(0.94f, 0.89f, 0.27f));
                    }
                // Road texture noise
                for (int y = 0; y < TileArt; y++)
                    for (int x = 0; x < TileArt; x++)
                        if ((x * 7 + y * 13) % 17 == 0) img.SetPixel(x, y, cD);
                break;

            case Ground.Plaza:
                cL = CPlazaL; cD = CPlazaD;
                FillBrick(img, cL, cD);
                break;

            case Ground.FlowerBed:
                cL = CFlowerL; cD = CFlowerD;
                FillSolid(img, cL);
                // Scatter: yellow flowers, pink flowers
                Paint(img, 4, 4, new Color(0.92f, 0.85f, 0.20f));
                Paint(img, 9, 6, new Color(0.96f, 0.55f, 0.70f));
                Paint(img, 12, 3, new Color(0.92f, 0.85f, 0.20f));
                Paint(img, 2, 11, new Color(0.80f, 0.90f, 0.28f));
                Paint(img, 7, 12, new Color(0.96f, 0.55f, 0.70f));
                break;

            case Ground.DirtPath:
                cL = CDirtL; cD = CDirtD;
                FillChecker(img, cL, cD);
                for (int y = 0; y < TileArt; y++)
                    for (int x = 0; x < TileArt; x++)
                        if ((x * 5 + y * 7) % 11 == 0) img.SetPixel(x, y, cD.Darkened(0.1f));
                break;

            default: // Grass
                cL = SeasonGrassL(season); cD = SeasonGrassD(season);
                FillChecker(img, cL, cD);
                // Mow band every 4 rows
                bool mow = (row / 4) % 2 == 0;
                if (!mow) FillChecker(img, cL.Darkened(0.06f), cD.Darkened(0.06f));
                // Scatter
                if (season == TimeManager.Season.Spring)
                {
                    for (int y = 0; y < TileArt; y++)
                        for (int x = 0; x < TileArt; x++)
                        {
                            if ((x * 13 + y *  7 + col * 3 + row * 5) % 89 == 0)
                                img.SetPixel(x, y, new Color(0.918f, 0.871f, 0.306f));
                            else if ((x * 11 + y * 17 + col * 7 + row * 3) % 127 == 0)
                                img.SetPixel(x, y, new Color(0.957f, 0.588f, 0.706f));
                        }
                }
                break;
        }

        return ImageTexture.CreateFromImage(img);
    }

    // ── Building textures ─────────────────────────────────────────────────────
    // Each building returns a texture of widthTiles*16 × 32 art-px (2 tiles tall).
    public static ImageTexture MakeBuildingTex(BuildingType bt, int widthTiles)
    {
        int artW = widthTiles * TileArt;
        const int artH = 32; // 2 tiles tall in art px
        var img = Image.CreateEmpty(artW, artH, false, Image.Format.Rgba8);

        switch (bt)
        {
            case BuildingType.HouseA:   DrawHouseA(img, artW, artH); break;
            case BuildingType.HouseB:   DrawHouseB(img, artW, artH); break;
            case BuildingType.HouseC:   DrawHouseC(img, artW, artH); break;
            case BuildingType.SportsShop: DrawShop(img, artW, artH, new Color(0.20f, 0.45f, 0.80f)); break;
            case BuildingType.Cafe:     DrawShop(img, artW, artH, new Color(0.60f, 0.30f, 0.15f)); break;
            case BuildingType.Bakery:   DrawShop(img, artW, artH, new Color(0.85f, 0.55f, 0.20f)); break;
            case BuildingType.Market:   DrawShop(img, artW, artH, new Color(0.20f, 0.65f, 0.35f)); break;
            case BuildingType.PitchGate: DrawPitchGate(img, artW, artH); break;
            case BuildingType.SchoolYard: DrawSchool(img, artW, artH); break;
        }
        return ImageTexture.CreateFromImage(img);
    }

    // ── Decoration textures ───────────────────────────────────────────────────
    // 8 × 16 art-px each (half tile wide, full tile tall) for most.
    public static ImageTexture MakeDecoTex(DecoType dt)
    {
        var img = Image.CreateEmpty(8, 16, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        switch (dt)
        {
            case DecoType.Lamppost:     DrawLamppost(img);      break;
            case DecoType.Bench:        DrawBench(img);         break;
            case DecoType.Tree:         DrawTree(img);          break;
            case DecoType.FlowerPot:    DrawFlowerPot(img);     break;
            case DecoType.TrashCan:     DrawTrashCan(img);      break;
            case DecoType.FootballPoster: DrawPoster(img);      break;
            case DecoType.BikeStand:    DrawBikeStand(img);     break;
            case DecoType.Flag:         DrawFlag(img);          break;
            case DecoType.BusStop:      DrawBusStop(img);       break;
        }
        return ImageTexture.CreateFromImage(img);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ── Building painters ────────────────────────────────────────────────────
    // ═════════════════════════════════════════════════════════════════════════

    private static void DrawHouseA(Image img, int w, int h)
    {
        var cWall = new Color(0.933f, 0.878f, 0.773f); // warm cream
        var cRoof = new Color(0.659f, 0.282f, 0.204f); // terracotta
        var cDoor = new Color(0.510f, 0.282f, 0.141f); // dark wood
        var cWin  = new Color(0.592f, 0.784f, 0.898f); // light blue glass
        var cOut  = new Color(0.235f, 0.180f, 0.118f); // outline

        FillRect(img, 0, 0, w, h, cWall);
        // Roof
        FillRect(img, 0, 0, w, 10, cRoof);
        // Roof outline
        for (int x = 0; x < w; x++) img.SetPixel(x, 10, cOut);
        // Chimney
        FillRect(img, 2, 0, 3, 5, new Color(0.549f, 0.439f, 0.357f));
        // Window
        FillRect(img, w/2-3, 12, 6, 5, cWin);
        OutlineRect(img, w/2-3, 12, 6, 5, cOut);
        // Door
        int dx = w/2-2;
        FillRect(img, dx, 22, 4, h-22, cDoor);
        OutlineRect(img, dx, 22, 4, h-22, cOut);
        // Door knob
        img.SetPixel(dx+3, 27, new Color(0.85f, 0.73f, 0.20f));
        // Wall outline
        OutlineRect(img, 0, 0, w, h, cOut);
    }

    private static void DrawHouseB(Image img, int w, int h)
    {
        var cWall = new Color(0.875f, 0.784f, 0.702f); // slightly warmer
        var cRoof = new Color(0.337f, 0.467f, 0.278f); // green roof
        var cDoor = new Color(0.380f, 0.200f, 0.100f);
        var cWin  = new Color(0.592f, 0.784f, 0.898f);
        var cOut  = new Color(0.235f, 0.180f, 0.118f);

        FillRect(img, 0, 0, w, h, cWall);
        FillRect(img, 0, 0, w, 10, cRoof);
        for (int x = 0; x < w; x++) img.SetPixel(x, 10, cOut);
        // Two windows
        FillRect(img, 2, 13, 5, 5, cWin); OutlineRect(img, 2, 13, 5, 5, cOut);
        FillRect(img, w-7, 13, 5, 5, cWin); OutlineRect(img, w-7, 13, 5, 5, cOut);
        // Door centred
        int dx = w/2-2;
        FillRect(img, dx, 21, 4, h-21, cDoor); OutlineRect(img, dx, 21, 4, h-21, cOut);
        img.SetPixel(dx+3, 27, new Color(0.85f, 0.73f, 0.20f));
        OutlineRect(img, 0, 0, w, h, cOut);
    }

    private static void DrawHouseC(Image img, int w, int h)
    {
        var cWall = new Color(0.918f, 0.855f, 0.745f);
        var cRoof = new Color(0.424f, 0.318f, 0.220f); // dark brown roof
        var cDoor = new Color(0.180f, 0.341f, 0.565f); // blue door
        var cWin  = new Color(0.592f, 0.784f, 0.898f);
        var cOut  = new Color(0.235f, 0.180f, 0.118f);

        FillRect(img, 0, 0, w, h, cWall);
        // Triangular gable roof
        for (int y = 0; y < 10; y++)
        {
            int margin = y;
            for (int x = 0; x < w; x++)
            {
                if (x >= margin && x < w - margin) img.SetPixel(x, y, cRoof);
            }
        }
        // Window
        FillRect(img, w/2-3, 12, 6, 5, cWin); OutlineRect(img, w/2-3, 12, 6, 5, cOut);
        // Door
        int dx = w/2-2;
        FillRect(img, dx, 22, 4, h-22, cDoor); OutlineRect(img, dx, 22, 4, h-22, cOut);
        img.SetPixel(dx+3, 27, new Color(0.85f, 0.73f, 0.20f));
        OutlineRect(img, 0, 0, w, h, cOut);
    }

    private static void DrawShop(Image img, int w, int h, Color signColor)
    {
        var cWall = new Color(0.910f, 0.875f, 0.820f);
        var cBase = new Color(0.700f, 0.660f, 0.600f);
        var cWin  = new Color(0.592f, 0.784f, 0.898f);
        var cDoor = new Color(0.510f, 0.282f, 0.141f);
        var cOut  = new Color(0.235f, 0.180f, 0.118f);
        var cSign = signColor;

        FillRect(img, 0, 0, w, h, cWall);
        // Awning / sign strip
        FillRect(img, 0, 5, w, 6, cSign);
        for (int x = 0; x < w; x++) img.SetPixel(x, 5, cOut);
        for (int x = 0; x < w; x++) img.SetPixel(x, 11, cOut);
        // Window display (large)
        FillRect(img, 2, 13, w-4, 8, cWin);
        OutlineRect(img, 2, 13, w-4, 8, cOut);
        // Door
        int dx = w/2-2;
        FillRect(img, dx, 21, 4, h-21, cDoor);
        OutlineRect(img, dx, 21, 4, h-21, cOut);
        img.SetPixel(dx+3, 28, new Color(0.85f, 0.73f, 0.20f));
        // Base step
        FillRect(img, 0, h-4, w, 4, cBase);
        OutlineRect(img, 0, 0, w, h, cOut);
    }

    private static void DrawPitchGate(Image img, int w, int h)
    {
        var cWall  = new Color(0.267f, 0.208f, 0.141f); // dark fence
        var cBrick = new Color(0.651f, 0.639f, 0.616f); // cobble pillar
        var cMetal = new Color(0.502f, 0.502f, 0.518f); // gate bars
        var cOut   = new Color(0.157f, 0.118f, 0.078f);

        FillRect(img, 0, 0, w, h, cBrick);
        // Gate frame pillars
        FillRect(img, 0, 0, 4, h, cWall);
        FillRect(img, w-4, 0, 4, h, cWall);
        // Gate bars
        int opening = w - 8;
        for (int x = 4; x < w-4; x += 3)
            for (int y = 0; y < h-4; y++)
                img.SetPixel(x, y, cMetal);
        // Sign above
        FillRect(img, 4, 0, w-8, 6, new Color(0.20f, 0.20f, 0.20f));
        // Star decoration on sign
        img.SetPixel(w/2, 3, new Color(0.94f, 0.85f, 0.20f));
        img.SetPixel(w/2-2, 3, new Color(0.94f, 0.85f, 0.20f));
        img.SetPixel(w/2+2, 3, new Color(0.94f, 0.85f, 0.20f));
        OutlineRect(img, 0, 0, w, h, cOut);
    }

    private static void DrawSchool(Image img, int w, int h)
    {
        var cWall = new Color(0.878f, 0.820f, 0.706f);
        var cRoof = new Color(0.529f, 0.196f, 0.196f);
        var cWin  = new Color(0.592f, 0.784f, 0.898f);
        var cDoor = new Color(0.510f, 0.282f, 0.141f);
        var cOut  = new Color(0.235f, 0.180f, 0.118f);

        FillRect(img, 0, 0, w, h, cWall);
        FillRect(img, 0, 0, w, 8, cRoof);
        for (int x = 0; x < w; x++) img.SetPixel(x, 8, cOut);
        // Multiple windows across
        for (int i = 0; i < w/8; i++)
        {
            int wx = i * 8 + 2;
            if (wx + 4 >= w) break;
            FillRect(img, wx, 11, 4, 5, cWin);
            OutlineRect(img, wx, 11, 4, 5, cOut);
        }
        // Centre double door
        int dx = w/2-4;
        FillRect(img, dx, 21, 8, h-21, cDoor);
        OutlineRect(img, dx, 21, 8, h-21, cOut);
        img.SetPixel(dx+3, 28, new Color(0.85f, 0.73f, 0.20f));
        img.SetPixel(dx+5, 28, new Color(0.85f, 0.73f, 0.20f));
        OutlineRect(img, 0, 0, w, h, cOut);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ── Decoration painters ───────────────────────────────────────────────────
    // ═════════════════════════════════════════════════════════════════════════

    private static void DrawLamppost(Image img)
    {
        var cPole   = new Color(0.424f, 0.424f, 0.439f);
        var cLamp   = new Color(0.957f, 0.918f, 0.608f);
        var cHead   = new Color(0.314f, 0.314f, 0.325f);
        // Pole
        for (int y = 3; y < 15; y++) img.SetPixel(4, y, cPole);
        // Head
        FillRect(img, 2, 1, 5, 3, cHead);
        // Light glow
        img.SetPixel(4, 2, cLamp);
        img.SetPixel(3, 2, cLamp.Lightened(0.3f));
        img.SetPixel(5, 2, cLamp.Lightened(0.3f));
        // Base
        FillRect(img, 3, 14, 3, 2, cHead);
    }

    private static void DrawBench(Image img)
    {
        var cWood = new Color(0.549f, 0.357f, 0.204f);
        var cMetal = new Color(0.490f, 0.490f, 0.498f);
        // Seat
        FillRect(img, 1, 8, 6, 2, cWood);
        // Legs
        img.SetPixel(1, 10, cMetal); img.SetPixel(1, 11, cMetal);
        img.SetPixel(6, 10, cMetal); img.SetPixel(6, 11, cMetal);
        // Back
        FillRect(img, 1, 5, 6, 2, cWood);
        img.SetPixel(1, 7, cMetal); img.SetPixel(6, 7, cMetal);
    }

    private static void DrawTree(Image img)
    {
        var cTrunk = new Color(0.424f, 0.282f, 0.161f);
        var cLeaf1 = new Color(0.200f, 0.549f, 0.118f);
        var cLeaf2 = new Color(0.157f, 0.459f, 0.090f);
        // Trunk
        FillRect(img, 3, 10, 2, 6, cTrunk);
        // Crown (circle approximation)
        for (int y = 0; y < 11; y++)
            for (int x = 0; x < 8; x++)
            {
                float dx = x - 3.5f, dy = y - 5f;
                if (dx*dx + dy*dy < 18f)
                    img.SetPixel(x, y, (x + y) % 2 == 0 ? cLeaf1 : cLeaf2);
            }
    }

    private static void DrawFlowerPot(Image img)
    {
        var cPot  = new Color(0.714f, 0.380f, 0.235f);
        var cSoil = new Color(0.424f, 0.318f, 0.204f);
        var cFlwr = new Color(0.957f, 0.588f, 0.706f);
        var cStem = new Color(0.278f, 0.627f, 0.153f);
        // Pot
        FillRect(img, 2, 10, 4, 5, cPot);
        img.SetPixel(1, 11, cPot); img.SetPixel(6, 11, cPot);
        FillRect(img, 1, 9, 6, 2, cPot.Darkened(0.1f));
        // Soil
        FillRect(img, 2, 10, 4, 2, cSoil);
        // Stem
        img.SetPixel(4, 7, cStem); img.SetPixel(4, 8, cStem); img.SetPixel(4, 9, cStem);
        // Flower
        img.SetPixel(4, 6, cFlwr); img.SetPixel(3, 6, cFlwr); img.SetPixel(5, 6, cFlwr);
        img.SetPixel(4, 5, cFlwr);
    }

    private static void DrawTrashCan(Image img)
    {
        var cCan  = new Color(0.404f, 0.506f, 0.396f);
        var cLid  = new Color(0.298f, 0.380f, 0.290f);
        var cOut  = new Color(0.200f, 0.200f, 0.200f);
        FillRect(img, 2, 7, 4, 8, cCan);
        FillRect(img, 1, 6, 6, 2, cLid);
        img.SetPixel(4, 5, cLid.Darkened(0.1f)); // handle
        OutlineRect(img, 2, 7, 4, 8, cOut);
    }

    private static void DrawPoster(Image img)
    {
        var cPaper = new Color(0.957f, 0.949f, 0.878f);
        var cText  = new Color(0.200f, 0.200f, 0.200f);
        var cStar  = new Color(0.157f, 0.318f, 0.682f);
        FillRect(img, 1, 4, 6, 9, cPaper);
        OutlineRect(img, 1, 4, 6, 9, cText);
        img.SetPixel(4, 6, cStar); img.SetPixel(3, 7, cStar);
        img.SetPixel(5, 7, cStar); img.SetPixel(4, 8, cStar);
        // Ball (tiny circle)
        img.SetPixel(4, 10, new Color(0.9f, 0.9f, 0.9f));
    }

    private static void DrawBikeStand(Image img)
    {
        var cMetal = new Color(0.502f, 0.502f, 0.518f);
        var cWheel = new Color(0.200f, 0.200f, 0.210f);
        // Frame
        for (int y = 6; y <= 14; y++) img.SetPixel(4, y, cMetal);
        FillRect(img, 1, 12, 6, 1, cMetal);
        // Wheels
        img.SetPixel(1, 10, cWheel); img.SetPixel(1, 11, cWheel); img.SetPixel(1, 12, cWheel);
        img.SetPixel(6, 10, cWheel); img.SetPixel(6, 11, cWheel); img.SetPixel(6, 12, cWheel);
    }

    private static void DrawFlag(Image img)
    {
        var cPole = new Color(0.502f, 0.502f, 0.518f);
        var cFlag = new Color(0.773f, 0.192f, 0.153f);
        // Pole
        for (int y = 2; y <= 15; y++) img.SetPixel(2, y, cPole);
        // Flag
        FillRect(img, 3, 2, 5, 4, cFlag);
        img.SetPixel(5, 3, new Color(1f, 1f, 1f));
        img.SetPixel(5, 4, new Color(1f, 1f, 1f));
    }

    private static void DrawBusStop(Image img)
    {
        var cFrame = new Color(0.502f, 0.502f, 0.518f);
        var cRoof  = new Color(0.200f, 0.420f, 0.706f);
        var cGlass = new Color(0.700f, 0.880f, 0.980f, 0.70f);
        // Roof
        FillRect(img, 0, 2, 8, 2, cRoof);
        // Frame sides
        img.SetPixel(0, 4, cFrame); img.SetPixel(0, 5, cFrame);
        img.SetPixel(0, 6, cFrame); img.SetPixel(0, 7, cFrame);
        img.SetPixel(7, 4, cFrame); img.SetPixel(7, 5, cFrame);
        img.SetPixel(7, 6, cFrame); img.SetPixel(7, 7, cFrame);
        // Glass panel
        FillRect(img, 1, 4, 6, 4, cGlass);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ── Helpers ───────────────────────────────────────────────────────────────
    // ═════════════════════════════════════════════════════════════════════════

    private static Image Img() =>
        Image.CreateEmpty(TileArt, TileArt, false, Image.Format.Rgba8);

    private static void FillSolid(Image img, Color c)
    {
        for (int y = 0; y < img.GetHeight(); y++)
            for (int x = 0; x < img.GetWidth(); x++)
                img.SetPixel(x, y, c);
    }

    private static void FillChecker(Image img, Color cL, Color cD)
    {
        for (int y = 0; y < img.GetHeight(); y++)
            for (int x = 0; x < img.GetWidth(); x++)
                img.SetPixel(x, y, (x + y) % 2 == 0 ? cL : cD);
    }

    private static void Fill2x2(Image img, Color cL, Color cD)
    {
        for (int y = 0; y < img.GetHeight(); y++)
            for (int x = 0; x < img.GetWidth(); x++)
                img.SetPixel(x, y, ((x >> 1) + (y >> 1)) % 2 == 0 ? cL : cD);
    }

    private static void FillBrick(Image img, Color cL, Color cD)
    {
        int w = img.GetWidth(), h = img.GetHeight();
        for (int y = 0; y < h; y++)
        {
            int rowParity = (y / 4) % 2;
            for (int x = 0; x < w; x++)
            {
                int offset = rowParity * 4;
                bool mortar = (y % 4 == 3) || ((x + offset) % 8 == 0);
                img.SetPixel(x, y, mortar ? cD : cL);
            }
        }
    }

    private static void FillRect(Image img, int x0, int y0, int rw, int rh, Color c)
    {
        int imgW = img.GetWidth(), imgH = img.GetHeight();
        for (int y = y0; y < y0 + rh && y < imgH; y++)
            for (int x = x0; x < x0 + rw && x < imgW; x++)
                img.SetPixel(x, y, c);
    }

    private static void OutlineRect(Image img, int x0, int y0, int rw, int rh, Color c)
    {
        int imgW = img.GetWidth(), imgH = img.GetHeight();
        for (int x = x0; x < x0 + rw && x < imgW; x++)
        {
            if (y0 < imgH)         img.SetPixel(x, y0,          c);
            if (y0+rh-1 < imgH)    img.SetPixel(x, y0 + rh - 1, c);
        }
        for (int y = y0; y < y0 + rh && y < imgH; y++)
        {
            if (x0 < imgW)         img.SetPixel(x0,          y, c);
            if (x0+rw-1 < imgW)    img.SetPixel(x0 + rw - 1, y, c);
        }
    }

    private static void Paint(Image img, int x, int y, Color c)
    {
        if (x >= 0 && x < img.GetWidth() && y >= 0 && y < img.GetHeight())
            img.SetPixel(x, y, c);
    }
}
