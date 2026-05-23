using Godot;
using static TownMapData;

/// Controller for the TownStreet scene.
/// Generates terrain, buildings, decorations, collision, and handles
/// player interaction / scene transitions.
public partial class TownStreetController : Node2D
{
    // ── Scene-tree references ─────────────────────────────────────────────────
    private Player?  _player;
    private Camera2D? _camera;
    private Label?   _hint;

    // ── World constants ───────────────────────────────────────────────────────
    private const int W = Cols * TileWorld; // 40 × 32 = 1280
    private const int H = Rows * TileWorld; // 30 × 32 = 960

    // ── Interaction ───────────────────────────────────────────────────────────
    private const float INTERACT_RADIUS = 56f;
    private BuildingPlacement? _nearBuilding;

    // ── Season ────────────────────────────────────────────────────────────────
    private TimeManager.Season _season;

    // ─────────────────────────────────────────────────────────────────────────
    public override void _Ready()
    {
        _season  = TimeManager.Instance?.CurrentSeason ?? TimeManager.Season.Spring;
        _player  = GetNodeOrNull<Player>("%Player");
        _camera  = GetNodeOrNull<Camera2D>("%Camera");
        _hint    = GetNodeOrNull<Label>("%HintLabel");

        if (_player != null) _player.Interacted += OnInteract;

        // Camera limits
        if (_camera != null)
        {
            _camera.LimitLeft   = 0;
            _camera.LimitTop    = 0;
            _camera.LimitRight  = W;
            _camera.LimitBottom = H;
        }

        BuildGround();
        BuildBuildings();
        BuildDecorations();

        // Day/night overlay
        var dayNight = new DayNightLayer { ShowSky = false };
        AddChild(dayNight);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public override void _Process(double delta)
    {
        if (_player == null) return;

        _nearBuilding = null;
        Vector2 pp = _player.GlobalPosition;

        foreach (var b in Buildings)
        {
            // Door is roughly at bottom-centre of the building
            var doorPos = new Vector2(
                (b.Col + b.WidthTiles / 2f) * TileWorld,
                (b.Row + 2) * TileWorld // 2 tile tall buildings, door bottom
            );
            if (pp.DistanceTo(doorPos) < INTERACT_RADIUS)
            {
                _nearBuilding = b;
                break;
            }
        }

        if (_hint != null)
        {
            _hint.Visible = _nearBuilding.HasValue;
            if (_nearBuilding.HasValue)
                _hint.Text = $"[E] {_nearBuilding.Value.Label}";
        }

        // Y-sort: update player ZIndex every frame based on feet position
        _player.ZIndex = (int)(_player.GlobalPosition.Y / TileWorld);
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void OnInteract()
    {
        if (_nearBuilding == null) return;
        var b = _nearBuilding.Value;

        if (b.TargetScene != null)
        {
            WorldManager.Instance?.GoTo(b.TargetScene);
        }
        else
        {
            DialogueManager.Instance?.Show(b.Label, "İçeri girmek için henüz hazır değilsin.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── Ground layer ─────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────
    private void BuildGround()
    {
        var groundNode = new Node2D { Name = "GroundLayer", ZIndex = -10 };
        AddChild(groundNode);

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                var g  = GetGround(col, row);
                var tx = TownTileGen.MakeGroundTile(g, _season, col, row);
                var sp = new Sprite2D
                {
                    Texture       = tx,
                    Centered      = false,
                    Position      = new Vector2(col * TileWorld, row * TileWorld),
                    Scale         = new Vector2(TileScale, TileScale),
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                };
                groundNode.AddChild(sp);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── Buildings ─────────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────
    private void BuildBuildings()
    {
        var buildingNode = new Node2D { Name = "Buildings" };
        AddChild(buildingNode);

        foreach (var b in Buildings)
        {
            var tex = TownTileGen.MakeBuildingTex(b.Type, b.WidthTiles);

            // Sprite — 2-tile-tall building, top-left anchored
            float worldX = b.Col * TileWorld;
            float worldY = b.Row * TileWorld;
            float bW = b.WidthTiles * TileWorld;
            float bH = 2 * TileWorld; // buildings are 2 tiles tall

            var sp = new Sprite2D
            {
                Name          = b.Type.ToString(),
                Texture       = tex,
                Centered      = false,
                Position      = new Vector2(worldX, worldY),
                Scale         = new Vector2(TileScale, TileScale),
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                ZIndex        = b.Row + 1, // Y-sort by tile row
            };
            buildingNode.AddChild(sp);

            // Collision body
            var body = new StaticBody2D
            {
                Name     = $"Coll_{b.Type}_{b.Col}_{b.Row}",
                Position = new Vector2(worldX, worldY),
            };
            var shape = new CollisionShape2D
            {
                Shape    = new RectangleShape2D { Size = new Vector2(bW, bH - TileWorld * 0.35f) },
                Position = new Vector2(bW / 2f, bH / 2f - TileWorld * 0.175f),
            };
            body.AddChild(shape);
            buildingNode.AddChild(body);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── Decorations ───────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────
    private void BuildDecorations()
    {
        var decoNode = new Node2D { Name = "Decorations" };
        AddChild(decoNode);

        foreach (var d in Decorations)
        {
            var tex = TownTileGen.MakeDecoTex(d.Type);
            float worldX = d.Col * TileWorld + TileWorld / 4f; // centre-ish in tile
            float worldY = d.Row * TileWorld;

            var sp = new Sprite2D
            {
                Name          = d.Type.ToString(),
                Texture       = tex,
                Centered      = false,
                Position      = new Vector2(worldX, worldY),
                Scale         = new Vector2(TileScale, TileScale),
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                ZIndex        = d.Row,
            };
            decoNode.AddChild(sp);

            // Solid decorations get a small collision shape
            if (d.Type == DecoType.Lamppost || d.Type == DecoType.Tree ||
                d.Type == DecoType.BusStop)
            {
                var body = new StaticBody2D { Position = new Vector2(worldX + 8, worldY + 24) };
                body.AddChild(new CollisionShape2D
                {
                    Shape = new CircleShape2D { Radius = 6f }
                });
                decoNode.AddChild(body);
            }
        }
    }
}
