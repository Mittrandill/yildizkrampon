using Godot;

/// godot --headless --script scenes/BuildHomeInterior.cs
/// Stardew Valley tarzı Ege evi iç mekanı.
public partial class BuildHomeInterior : SceneBuilderBase
{
    // 20 x 15 tile, 32 px/tile = 640 x 480
    private const int TS = 32;
    private const int RW = 20;
    private const int RH = 15;
    private const float W  = RW * TS;   // 640
    private const float H  = RH * TS;   // 480
    private const float WALL_H = 72f;   // üst duvar yüksekliği

    // Renk paleti — Ege / Akdeniz tonları
    private static readonly Color C_FLOOR      = new Color(0.88f, 0.83f, 0.70f);
    private static readonly Color C_FLOOR2     = new Color(0.84f, 0.79f, 0.67f);
    private static readonly Color C_WALL       = new Color(0.94f, 0.92f, 0.86f);
    private static readonly Color C_WALL_TOP   = new Color(0.80f, 0.58f, 0.38f);  // terracotta
    private static readonly Color C_SKIRTING   = new Color(0.65f, 0.50f, 0.32f);
    private static readonly Color C_WOOD_DARK  = new Color(0.38f, 0.24f, 0.10f);
    private static readonly Color C_WOOD_MID   = new Color(0.55f, 0.38f, 0.18f);
    private static readonly Color C_WOOD_LIGHT = new Color(0.70f, 0.52f, 0.28f);
    private static readonly Color C_AEGEAN     = new Color(0.22f, 0.48f, 0.78f);
    private static readonly Color C_WIN_GLOW   = new Color(1.00f, 0.92f, 0.60f, 0.35f);
    private static readonly Color C_SHEET      = new Color(0.82f, 0.90f, 0.96f);  // yatak çarşafı
    private static readonly Color C_PILLOW     = new Color(0.96f, 0.94f, 0.92f);
    private static readonly Color C_RUG_BASE   = new Color(0.68f, 0.22f, 0.14f);
    private static readonly Color C_RUG_TRIM   = new Color(0.90f, 0.72f, 0.20f);
    private static readonly Color C_TERRA      = new Color(0.75f, 0.40f, 0.22f);
    private static readonly Color C_GREEN      = new Color(0.22f, 0.58f, 0.22f);
    private static readonly Color C_IRON       = new Color(0.32f, 0.32f, 0.35f);

    public override void _Initialize()
    {
        GD.Print("Generating: HomeInterior (Ege evi)");
        var temp = new Node();
        var root = new HomeInteriorScene(); root.Name = "HomeInterior";
        temp.AddChild(root);

        _BuildFloor(root);
        _BuildWalls(root);
        _BuildWindows(root);
        _BuildDoor(root);
        _BuildBed(root);
        _BuildBookshelf(root);
        _BuildDesk(root);
        _BuildRug(root);
        _BuildChest(root);
        _BuildPlant(root);
        _BuildBoundaries(root);
        _BuildPlayer(root);
        _BuildCamera(root);
        _BuildHUD(root);

        temp.RemoveChild(root); temp.Free();
        PackAndSave(root, "res://scenes/HomeInterior.tscn");
        Quit();
    }

    // ─── Zemin ───────────────────────────────────────────────────

    private void _BuildFloor(Node root)
    {
        // Ana zemin rengi
        _R(root, Vector2.Zero, new Vector2(W, H), C_FLOOR, -10);

        // Dama deseni — her iki sırada hafif ton farkı
        for (int y = 0; y < RH; y++)
        {
            for (int x = 0; x < RW; x++)
            {
                if ((x + y) % 2 == 0)
                    _R(root, new Vector2(x * TS, y * TS + WALL_H), new Vector2(TS, TS),
                       C_FLOOR2 with { A = 0.40f }, -9);
            }
        }

        // Süpürgelik
        _R(root, new Vector2(0, H - 12), new Vector2(W, 12), C_SKIRTING, -8);
    }

    // ─── Duvarlar ────────────────────────────────────────────────

    private void _BuildWalls(Node root)
    {
        // Tavan/üst duvar — terracotta
        _R(root, Vector2.Zero, new Vector2(W, WALL_H), C_WALL_TOP, -7);
        // İç duvar yüzeyi
        _R(root, new Vector2(0, WALL_H), new Vector2(W, 16), C_WALL, -7);
        // Yan duvarlar (ince şerit)
        _R(root, new Vector2(0, WALL_H), new Vector2(12, H - WALL_H), C_SKIRTING with { A = 0.5f }, -7);
        _R(root, new Vector2(W - 12, WALL_H), new Vector2(12, H - WALL_H), C_SKIRTING with { A = 0.5f }, -7);
    }

    // ─── Pencereler ─────────────────────────────────────────────

    private void _BuildWindows(Node root)
    {
        _Window(root, 128f);   // sol pencere
        _Window(root, 384f);   // sağ pencere
    }

    private void _Window(Node root, float cx)
    {
        float wy = 8f;
        float ww = 80f, wh = 56f;
        float x = cx - ww / 2f;

        // Dış çerçeve (Ege mavisi)
        _R(root, new Vector2(x - 4, wy - 4), new Vector2(ww + 8, wh + 8), C_AEGEAN, -6);
        // Içerik / ışık
        _R(root, new Vector2(x, wy), new Vector2(ww, wh), C_WIN_GLOW, -5);
        // Çapraz çizgiler (çerçeve kolu)
        _R(root, new Vector2(cx - 2, wy), new Vector2(4, wh), C_AEGEAN, -4);
        _R(root, new Vector2(x, wy + wh / 2f - 2), new Vector2(ww, 4), C_AEGEAN, -4);
        // Pencerenin altı: çiçek kutusu
        _R(root, new Vector2(x - 2, wy + wh + 4), new Vector2(ww + 4, 10), C_TERRA, -4);
        _R(root, new Vector2(x + 4, wy + wh + 2), new Vector2(8, 8), C_GREEN, -3);
        _R(root, new Vector2(x + ww - 14, wy + wh + 2), new Vector2(8, 8), C_GREEN, -3);
    }

    // ─── Kapı ────────────────────────────────────────────────────

    private void _BuildDoor(Node root)
    {
        float dx = 288f, dy = H - 64f;
        float dw = 64f, dh = 64f;

        // Kapı çerçevesi
        _R(root, new Vector2(dx - 6, dy - 4), new Vector2(dw + 12, dh + 4), C_SKIRTING, -6);
        // Kapı yüzeyi
        _R(root, new Vector2(dx, dy), new Vector2(dw, dh), C_WOOD_DARK, -5);
        // Kapı paneli detayı
        _R(root, new Vector2(dx + 8, dy + 8), new Vector2(dw - 16, dh - 28), C_WOOD_MID, -4);
        _R(root, new Vector2(dx + 8, dy + dh - 20), new Vector2(dw - 16, 12), C_WOOD_MID, -4);
        // Kapı kolu
        _R(root, new Vector2(dx + dw - 14, dy + dh / 2f - 6), new Vector2(8, 12), C_IRON, -3);
        // Eşik çizgisi
        _R(root, new Vector2(dx - 4, dy + dh - 4), new Vector2(dw + 8, 8), C_WOOD_LIGHT, -3);

        // "E: Dışarı Çık" hint label
        var hint = new Label { Name = "HintLabel", Text = "E  Dışarı Çık" };
        hint.UniqueNameInOwner = true;
        hint.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        hint.Position = new Vector2(W / 2f - 80, H - 96);
        hint.AddThemeFontSizeOverride("font_size", 14);
        hint.AddThemeColorOverride("font_color", Colors.White);
        hint.Visible = false;
        hint.ZIndex = 20;
        root.AddChild(hint);
    }

    // ─── Yatak ───────────────────────────────────────────────────

    private void _BuildBed(Node root)
    {
        float bx = 20f, by = WALL_H + 20f;

        // Yatak çerçevesi (koyu ahşap)
        _R(root, new Vector2(bx, by), new Vector2(96, 128), C_WOOD_DARK, 1);
        // Şilte
        _R(root, new Vector2(bx + 6, by + 20), new Vector2(84, 98), C_SHEET, 2);
        // Yastık
        _R(root, new Vector2(bx + 10, by + 24), new Vector2(76, 26), C_PILLOW, 3);
        // Yastık dikiş
        _R(root, new Vector2(bx + 16, by + 30), new Vector2(28, 14), C_SHEET with { A = 0.4f }, 4);
        _R(root, new Vector2(bx + 52, by + 30), new Vector2(28, 14), C_SHEET with { A = 0.4f }, 4);
        // Baş panel
        _R(root, new Vector2(bx + 6, by + 6), new Vector2(84, 16), C_WOOD_MID, 3);
        // Ayaklar
        _R(root, new Vector2(bx + 4, by + 4), new Vector2(10, 10), C_WOOD_DARK, 4);
        _R(root, new Vector2(bx + 82, by + 4), new Vector2(10, 10), C_WOOD_DARK, 4);
        _R(root, new Vector2(bx + 4, by + 114), new Vector2(10, 10), C_WOOD_DARK, 4);
        _R(root, new Vector2(bx + 82, by + 114), new Vector2(10, 10), C_WOOD_DARK, 4);
    }

    // ─── Kitaplık ────────────────────────────────────────────────

    private void _BuildBookshelf(Node root)
    {
        float sx = W - 108f, sy = WALL_H + 16f;

        // Raf gövdesi
        _R(root, new Vector2(sx, sy), new Vector2(88, 96), C_WOOD_DARK, 1);
        // Arka panel
        _R(root, new Vector2(sx + 6, sy + 6), new Vector2(76, 84), C_WOOD_MID, 2);
        // Raflar
        _R(root, new Vector2(sx + 6, sy + 32), new Vector2(76, 6), C_WOOD_DARK, 3);
        _R(root, new Vector2(sx + 6, sy + 62), new Vector2(76, 6), C_WOOD_DARK, 3);
        // Kitaplar — üst raf
        _Books(root, sx + 8, sy + 8,  24, new Color(0.65f, 0.15f, 0.15f));
        _Books(root, sx + 34, sy + 8, 16, new Color(0.15f, 0.35f, 0.65f));
        _Books(root, sx + 52, sy + 8, 20, new Color(0.25f, 0.55f, 0.25f));
        // Kitaplar — alt raf
        _Books(root, sx + 8, sy + 38, 18, new Color(0.60f, 0.45f, 0.15f));
        _Books(root, sx + 28, sy + 38, 22, new Color(0.55f, 0.20f, 0.50f));
        _Books(root, sx + 52, sy + 38, 20, new Color(0.70f, 0.35f, 0.12f));
    }

    private void _Books(Node root, float x, float y, float w, Color c)
    {
        _R(root, new Vector2(x, y), new Vector2(w, 22), c, 4);
        _R(root, new Vector2(x + 2, y + 2), new Vector2(w - 4, 4), c with { R = c.R * 1.2f, A = 0.5f }, 5);
    }

    // ─── Çalışma masası ──────────────────────────────────────────

    private void _BuildDesk(Node root)
    {
        float dx = W - 148f, dy = WALL_H + 120f;

        // Masa yüzeyi
        _R(root, new Vector2(dx, dy), new Vector2(128, 16), C_WOOD_MID, 1);
        // Çekmeceler
        _R(root, new Vector2(dx, dy + 16), new Vector2(56, 48), C_WOOD_DARK, 1);
        _R(root, new Vector2(dx + 60, dy + 16), new Vector2(68, 48), C_WOOD_DARK, 1);
        // Çekmece tutamaçları
        _R(root, new Vector2(dx + 20, dy + 38), new Vector2(16, 6), C_IRON, 2);
        _R(root, new Vector2(dx + 80, dy + 38), new Vector2(16, 6), C_IRON, 2);
        // Masa ayakları
        _R(root, new Vector2(dx + 4, dy + 64), new Vector2(8, 24), C_WOOD_DARK, 2);
        _R(root, new Vector2(dx + 116, dy + 64), new Vector2(8, 24), C_WOOD_DARK, 2);
        // Masa üstü: not defteri
        _R(root, new Vector2(dx + 14, dy - 20), new Vector2(40, 30), new Color(0.95f, 0.93f, 0.82f), 3);
        _R(root, new Vector2(dx + 18, dy - 16), new Vector2(32, 4), new Color(0.55f, 0.75f, 0.90f, 0.6f), 4);
        _R(root, new Vector2(dx + 18, dy - 10), new Vector2(32, 4), new Color(0.55f, 0.75f, 0.90f, 0.6f), 4);
        // Kalem
        _R(root, new Vector2(dx + 60, dy - 22), new Vector2(4, 28), new Color(0.85f, 0.30f, 0.10f), 3);
    }

    // ─── Kilim ───────────────────────────────────────────────────

    private void _BuildRug(Node root)
    {
        float rx = 176f, ry = H / 2f - 64f;
        float rw = 288f, rh = 128f;

        // Ana kilim
        _R(root, new Vector2(rx, ry), new Vector2(rw, rh), C_RUG_BASE, 0);
        // Bordür
        _R(root, new Vector2(rx + 8, ry + 8), new Vector2(rw - 16, rh - 16), C_RUG_TRIM with { A = 0.3f }, 1);
        // İç çerçeve
        _R(root, new Vector2(rx + 16, ry + 16), new Vector2(rw - 32, rh - 32), C_RUG_BASE with { R = 0.55f }, 1);
        // Merkez desen
        float cx2 = rx + rw / 2f, cy2 = ry + rh / 2f;
        _R(root, new Vector2(cx2 - 32, cy2 - 24), new Vector2(64, 48), C_RUG_TRIM with { A = 0.6f }, 2);
        _R(root, new Vector2(cx2 - 16, cy2 - 32), new Vector2(32, 64), C_RUG_TRIM with { A = 0.6f }, 2);
        // Köşe desenleri
        foreach (var (ox, oy) in new (float, float)[] { (20,20),(20,-20),(-20,20),(-20,-20) })
        {
            _R(root, new Vector2(cx2 + ox * 2 - 10, cy2 + oy - 10), new Vector2(20, 20), C_RUG_TRIM with { A = 0.5f }, 3);
        }
        // Saçaklar
        for (int i = 0; i < 8; i++)
        {
            _R(root, new Vector2(rx + i * 36 + 8, ry - 8), new Vector2(8, 10), C_RUG_TRIM, 1);
            _R(root, new Vector2(rx + i * 36 + 8, ry + rh - 2), new Vector2(8, 10), C_RUG_TRIM, 1);
        }
    }

    // ─── Sandık ──────────────────────────────────────────────────

    private void _BuildChest(Node root)
    {
        float cx = 20f, cy = H - 148f;

        // Gövde
        _R(root, new Vector2(cx, cy), new Vector2(80, 60), C_WOOD_DARK, 1);
        // Kapak
        _R(root, new Vector2(cx, cy), new Vector2(80, 20), C_WOOD_MID, 2);
        // Metal çerçeve
        _R(root, new Vector2(cx + 2, cy + 2), new Vector2(76, 4), C_IRON, 3);
        _R(root, new Vector2(cx + 2, cy + 18), new Vector2(76, 4), C_IRON, 3);
        // Kilit
        _R(root, new Vector2(cx + 32, cy + 12), new Vector2(16, 16), C_IRON, 4);
        _R(root, new Vector2(cx + 36, cy + 8),  new Vector2(8, 6),  C_IRON, 5);
        // Metal bantlar
        _R(root, new Vector2(cx + 20, cy), new Vector2(6, 60), C_IRON with { A = 0.5f }, 3);
        _R(root, new Vector2(cx + 54, cy), new Vector2(6, 60), C_IRON with { A = 0.5f }, 3);
    }

    // ─── Saksı bitki ─────────────────────────────────────────────

    private void _BuildPlant(Node root)
    {
        float px = W - 68f, py = H - 120f;

        // Saksı
        _R(root, new Vector2(px, py + 28), new Vector2(36, 28), C_TERRA, 1);
        _R(root, new Vector2(px + 4, py + 24), new Vector2(28, 8), C_TERRA with { R = 0.85f }, 2);
        // Toprak
        _R(root, new Vector2(px + 4, py + 24), new Vector2(28, 6), new Color(0.32f, 0.22f, 0.12f), 3);
        // Gövde
        _R(root, new Vector2(px + 15, py + 4), new Vector2(6, 24), new Color(0.28f, 0.42f, 0.18f), 2);
        // Yapraklar (çokgen benzeri ColorRect + dönme yoksa dikdörtgen)
        _R(root, new Vector2(px - 8, py - 8), new Vector2(20, 14), C_GREEN, 3);
        _R(root, new Vector2(px + 14, py - 14), new Vector2(18, 12), C_GREEN with { G = 0.68f }, 3);
        _R(root, new Vector2(px + 4, py + 4), new Vector2(16, 10), C_GREEN, 3);
    }

    // ─── Fizik sınırları ─────────────────────────────────────────

    private void _BuildBoundaries(Node root)
    {
        _Wall(root, "WallTop",    new Vector2(W / 2f, WALL_H - 10), new Vector2(W, 20));
        _Wall(root, "WallLeft",   new Vector2(-10, H / 2f),         new Vector2(20, H));
        _Wall(root, "WallRight",  new Vector2(W + 10, H / 2f),      new Vector2(20, H));
        _Wall(root, "WallBot",    new Vector2(W / 2f, H + 10),      new Vector2(W, 20));
        // Mobilya çarpışmaları
        _Wall(root, "ColBed",     new Vector2(68f, WALL_H + 84f),   new Vector2(96, 128));
        _Wall(root, "ColShelf",   new Vector2(W - 64f, WALL_H + 64f), new Vector2(88, 96));
        _Wall(root, "ColDesk",    new Vector2(W - 84f, WALL_H + 156f), new Vector2(128, 72));
        _Wall(root, "ColChest",   new Vector2(60f, H - 118f),       new Vector2(80, 60));
    }

    private static void _Wall(Node p, string name, Vector2 center, Vector2 size)
    {
        var sb = new StaticBody2D { Name = name, CollisionLayer = 16u, CollisionMask = 1u };
        var col = new CollisionShape2D();
        col.Shape = new RectangleShape2D { Size = size };
        col.Position = center;
        sb.AddChild(col);
        p.AddChild(sb);
    }

    // ─── Oyuncu ──────────────────────────────────────────────────

    private void _BuildPlayer(Node root)
    {
        var p = new Player { Name = "Player", UniqueNameInOwner = true };
        p.Position = new Vector2(320f, 420f);   // kapı önü
        p.ZIndex = 5;

        var col = new CollisionShape2D();
        col.Shape = new CapsuleShape2D { Radius = 8f, Height = 20f };
        col.Position = new Vector2(0, 4f);
        p.AddChild(col);

        root.AddChild(p);
    }

    // ─── Kamera ──────────────────────────────────────────────────

    private void _BuildCamera(Node root)
    {
        var cam = new Camera2D
        {
            Name                     = "Camera",
            UniqueNameInOwner        = true,
            Zoom                     = new Vector2(2.0f, 2.0f),
            PositionSmoothingEnabled = true,
            PositionSmoothingSpeed   = 6f,
            LimitLeft   = 0,
            LimitTop    = 0,
            LimitRight  = (int)W,
            LimitBottom = (int)H
        };
        root.AddChild(cam);
    }

    // ─── HUD ─────────────────────────────────────────────────────

    private void _BuildHUD(Node root)
    {
        var hud = new CanvasLayer { Name = "HUD", Layer = 10 };
        root.AddChild(hud);

        var locLbl = new Label { Text = "Ev" };
        locLbl.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        locLbl.Position = new Vector2(12, 12);
        locLbl.AddThemeFontSizeOverride("font_size", 14);
        locLbl.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.70f));
        hud.AddChild(locLbl);

        var ctrl = new Label { Text = "WASD: Hareket   E: Etkileşim" };
        ctrl.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        ctrl.Position = new Vector2(10, -24);
        ctrl.AddThemeFontSizeOverride("font_size", 11);
        ctrl.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        hud.AddChild(ctrl);
    }

    // ─── Yardımcılar ─────────────────────────────────────────────

    private static void _R(Node p, Vector2 pos, Vector2 size, Color c, int z)
    {
        var r = new ColorRect { Color = c, Position = pos, Size = size, ZIndex = z };
        p.AddChild(r);
    }
}
