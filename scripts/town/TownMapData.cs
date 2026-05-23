using Godot;

/// Static map layout for TownStreet.
/// 40 × 30 tile grid, each tile = 16 art-px × 2 scale = 32 world-px.
/// World size: 1280 × 960.
public static class TownMapData
{
    // ── Grid dimensions ──────────────────────────────────────────────────────
    public const int Cols      = 40;
    public const int Rows      = 30;
    public const int TileArt   = 16;   // art pixels per tile
    public const int TileScale = 2;    // sprite scale → 32 world px/tile
    public const int TileWorld = TileArt * TileScale; // 32 world px

    // ── Ground tile types ────────────────────────────────────────────────────
    public enum Ground
    {
        Grass,
        Cobblestone,
        Sidewalk,
        Road,
        Plaza,
        FlowerBed,
        DirtPath,
    }

    // ── Building types ───────────────────────────────────────────────────────
    public enum BuildingType
    {
        HouseA, HouseB, HouseC,
        SportsShop,
        Cafe,
        Bakery,
        Market,
        PitchGate,
        SchoolYard,
    }

    // ── Decoration types ─────────────────────────────────────────────────────
    public enum DecoType
    {
        Lamppost,
        Bench,
        Tree,
        FlowerPot,
        TrashCan,
        FootballPoster,
        BikeStand,
        Flag,
        BusStop,
    }

    // ── Ground layout ────────────────────────────────────────────────────────
    // Returns the ground type for a given tile column + row.
    public static Ground GetGround(int col, int row)
    {
        // Main horizontal road: rows 13-15
        if (row >= 13 && row <= 15) return Ground.Road;

        // Vertical road through centre: cols 19-20
        if (col >= 19 && col <= 20) return Ground.Road;

        // Sidewalks adjacent to roads
        if (row == 12 || row == 16) return Ground.Sidewalk;
        if (col == 18 || col == 21) return Ground.Sidewalk;

        // Plaza at intersection centre
        if (row >= 13 && row <= 15 && col >= 19 && col <= 20) return Ground.Plaza;

        // Decorative flower beds near edge
        if (row >= 2 && row <= 4 && (col >= 3 && col <= 6)) return Ground.FlowerBed;
        if (row >= 2 && row <= 4 && (col >= 33 && col <= 36)) return Ground.FlowerBed;

        // Cobblestone plaza near PitchGate (south-centre)
        if (row >= 24 && row <= 28 && col >= 16 && col <= 23) return Ground.Cobblestone;

        // Grass everywhere else
        return Ground.Grass;
    }

    // ── Building placements ──────────────────────────────────────────────────
    public struct BuildingPlacement
    {
        public BuildingType Type;
        public int Col, Row;          // top-left tile
        public int WidthTiles;        // width in tiles
        public string Label;          // shown on interaction
        public string? TargetScene;   // GoTo scene name (null = dialogue only)
    }

    public static readonly BuildingPlacement[] Buildings = new[]
    {
        // North row — houses left side
        new BuildingPlacement { Type=BuildingType.HouseA,     Col=1,  Row=2,  WidthTiles=4, Label="Ev (Ali)",        TargetScene=null },
        new BuildingPlacement { Type=BuildingType.HouseB,     Col=6,  Row=2,  WidthTiles=4, Label="Ev (Buse)",       TargetScene=null },
        new BuildingPlacement { Type=BuildingType.HouseC,     Col=11, Row=2,  WidthTiles=4, Label="Ev (Cemil)",      TargetScene=null },

        // North row — houses right side
        new BuildingPlacement { Type=BuildingType.HouseA,     Col=25, Row=2,  WidthTiles=4, Label="Ev (Derya)",      TargetScene=null },
        new BuildingPlacement { Type=BuildingType.HouseB,     Col=30, Row=2,  WidthTiles=4, Label="Ev (Emre)",       TargetScene=null },
        new BuildingPlacement { Type=BuildingType.HouseC,     Col=35, Row=2,  WidthTiles=4, Label="Ev (Fatma)",      TargetScene=null },

        // West side — shops
        new BuildingPlacement { Type=BuildingType.SportsShop, Col=1,  Row=17, WidthTiles=5, Label="Spor Mağazası",   TargetScene=null },
        new BuildingPlacement { Type=BuildingType.Cafe,        Col=7,  Row=17, WidthTiles=4, Label="Kafe",             TargetScene=null },
        new BuildingPlacement { Type=BuildingType.Bakery,      Col=12, Row=17, WidthTiles=4, Label="Fırın",            TargetScene=null },

        // East side — shops
        new BuildingPlacement { Type=BuildingType.Market,     Col=23, Row=17, WidthTiles=5, Label="Market",           TargetScene=null },
        new BuildingPlacement { Type=BuildingType.SchoolYard,  Col=29, Row=17, WidthTiles=8, Label="Okul",             TargetScene=null },

        // South — pitch gate
        new BuildingPlacement { Type=BuildingType.PitchGate,  Col=17, Row=24, WidthTiles=6, Label="Mahalle Stadı",   TargetScene="NeighborhoodMatch" },
    };

    // ── Decoration placements ────────────────────────────────────────────────
    public struct DecoPlacement
    {
        public DecoType Type;
        public int Col, Row;
    }

    public static readonly DecoPlacement[] Decorations = new[]
    {
        // Lampposts along main road
        new DecoPlacement { Type=DecoType.Lamppost, Col=2,  Row=12 },
        new DecoPlacement { Type=DecoType.Lamppost, Col=8,  Row=12 },
        new DecoPlacement { Type=DecoType.Lamppost, Col=14, Row=12 },
        new DecoPlacement { Type=DecoType.Lamppost, Col=24, Row=12 },
        new DecoPlacement { Type=DecoType.Lamppost, Col=30, Row=12 },
        new DecoPlacement { Type=DecoType.Lamppost, Col=37, Row=12 },
        new DecoPlacement { Type=DecoType.Lamppost, Col=2,  Row=16 },
        new DecoPlacement { Type=DecoType.Lamppost, Col=37, Row=16 },

        // Benches on sidewalk
        new DecoPlacement { Type=DecoType.Bench, Col=5,  Row=12 },
        new DecoPlacement { Type=DecoType.Bench, Col=11, Row=12 },
        new DecoPlacement { Type=DecoType.Bench, Col=27, Row=12 },
        new DecoPlacement { Type=DecoType.Bench, Col=33, Row=12 },

        // Trees scattered on grass
        new DecoPlacement { Type=DecoType.Tree, Col=2,  Row=7  },
        new DecoPlacement { Type=DecoType.Tree, Col=16, Row=8  },
        new DecoPlacement { Type=DecoType.Tree, Col=22, Row=7  },
        new DecoPlacement { Type=DecoType.Tree, Col=37, Row=7  },
        new DecoPlacement { Type=DecoType.Tree, Col=1,  Row=22 },
        new DecoPlacement { Type=DecoType.Tree, Col=38, Row=22 },

        // Flower pots by shop doors
        new DecoPlacement { Type=DecoType.FlowerPot, Col=6,  Row=17 },
        new DecoPlacement { Type=DecoType.FlowerPot, Col=11, Row=17 },
        new DecoPlacement { Type=DecoType.FlowerPot, Col=22, Row=17 },

        // Trash cans
        new DecoPlacement { Type=DecoType.TrashCan, Col=18, Row=12 },
        new DecoPlacement { Type=DecoType.TrashCan, Col=21, Row=16 },

        // Football posters on house walls
        new DecoPlacement { Type=DecoType.FootballPoster, Col=5,  Row=6  },
        new DecoPlacement { Type=DecoType.FootballPoster, Col=34, Row=6  },

        // Bike stands
        new DecoPlacement { Type=DecoType.BikeStand, Col=7,  Row=16 },
        new DecoPlacement { Type=DecoType.BikeStand, Col=28, Row=16 },

        // Flags near pitch gate
        new DecoPlacement { Type=DecoType.Flag, Col=16, Row=23 },
        new DecoPlacement { Type=DecoType.Flag, Col=23, Row=23 },

        // Bus stop
        new DecoPlacement { Type=DecoType.BusStop, Col=38, Row=13 },
    };
}
