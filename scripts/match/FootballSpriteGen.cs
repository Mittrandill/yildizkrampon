using Godot;
using System;

/// Procedural pixel-art sprite generator for football characters.
///
/// Generated textures are 48 × 64 px per frame, 3/4 top-down perspective
/// (slightly angled), charming/warm Stardew Valley-inspired style.
///
/// Usage:
///   var tex = FootballSpriteGen.BuildPlayerSheet(FootballSpriteGen.PlayerRole.Outfield);
///   var frames = FootballSpriteGen.BuildSpriteFrames(tex);
///
/// Animations produced:
///   Outfield: idle, walk, sprint, receive_ball, pass, shoot, tackle, celebrate
///   Goalkeeper: idle, sidestep, catch, dive_left, dive_right, kick, throw, celebrate
///   Shared: dribble
///
/// Every animation is available for 4 directions: _down, _up, _left, _right.
///
/// Shadow:
///   A soft oval shadow node (Sprite2D) is returned by BuildShadow().
public static class FootballSpriteGen
{
    // ── Constants ─────────────────────────────────────────────────────────────
    public const int FrameW = 48;
    public const int FrameH = 64;

    public enum PlayerRole { Outfield, Goalkeeper }

    // Directions (for multi-dir animation names)
    private static readonly string[] Dirs = { "down", "up", "left", "right" };

    // ── Palette ───────────────────────────────────────────────────────────────
    // Skin tones
    private static readonly Color CSkin    = new(0.918f, 0.765f, 0.600f);
    private static readonly Color CSkinSh  = new(0.769f, 0.600f, 0.455f);
    private static readonly Color CHair    = new(0.271f, 0.176f, 0.098f);
    private static readonly Color CEye     = new(0.157f, 0.118f, 0.078f);

    // Outfield kit: red shirt, dark shorts, white socks
    private static readonly Color CShirtO  = new(0.800f, 0.149f, 0.149f);
    private static readonly Color CShirtOS = new(0.627f, 0.098f, 0.098f);
    private static readonly Color CShortsO = new(0.118f, 0.118f, 0.200f);
    private static readonly Color CSocksO  = new(0.900f, 0.900f, 0.900f);
    private static readonly Color CBootO   = new(0.200f, 0.180f, 0.157f);

    // Goalkeeper kit: yellow shirt, black shorts
    private static readonly Color CShirtGK  = new(0.918f, 0.824f, 0.118f);
    private static readonly Color CShirtGKS = new(0.718f, 0.627f, 0.078f);
    private static readonly Color CShortsGK = new(0.100f, 0.100f, 0.100f);
    private static readonly Color CGlove    = new(0.824f, 0.824f, 0.855f);

    // Ball (white with black pentagon patches)
    private static readonly Color CBallW   = new(0.950f, 0.950f, 0.950f);
    private static readonly Color CBallB   = new(0.100f, 0.100f, 0.100f);
    private static readonly Color CBallSh  = new(0.780f, 0.780f, 0.780f);

    // Outline
    private static readonly Color COut     = new(0.098f, 0.078f, 0.059f);

    // ═════════════════════════════════════════════════════════════════════════
    // ── Public API ────────────────────────────────────────────────────────────
    // ═════════════════════════════════════════════════════════════════════════

    /// Build a SpriteFrames resource with all football animations for the given role.
    public static SpriteFrames BuildSpriteFrames(PlayerRole role)
    {
        var frames = new SpriteFrames();

        // ── Per-direction animations ──────────────────────────────────────────
        var animDefs = role == PlayerRole.Goalkeeper
            ? GKAnimDefs()
            : OutfieldAnimDefs();

        foreach (var (animBase, fps, frameCount) in animDefs)
        {
            foreach (var dir in Dirs)
            {
                string name = $"{animBase}_{dir}";
                frames.AddAnimation(name);
                frames.SetAnimationSpeed(name, fps);
                frames.SetAnimationLoop(name, true);

                for (int f = 0; f < frameCount; f++)
                {
                    var img = DrawFrame(role, animBase, dir, f, frameCount);
                    frames.AddFrame(name, ImageTexture.CreateFromImage(img));
                }
            }
        }

        return frames;
    }

    /// Returns a small oval shadow Sprite2D (position at feet = (0, 0)).
    public static Sprite2D BuildShadow()
    {
        var img = Image.CreateEmpty(24, 8, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 24; x++)
            {
                float nx = (x - 12f) / 12f;
                float ny = (y - 4f)  / 4f;
                if (nx * nx + ny * ny < 1f)
                    img.SetPixel(x, y, new Color(0f, 0f, 0f, 0.28f));
            }
        return new Sprite2D
        {
            Texture       = ImageTexture.CreateFromImage(img),
            Position      = new Vector2(0, 4),
            ZIndex        = -1,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ── Frame renderer ────────────────────────────────────────────────────────
    // ═════════════════════════════════════════════════════════════════════════

    private static Image DrawFrame(PlayerRole role, string anim, string dir,
                                    int frameIdx, int totalFrames)
    {
        var img = Image.CreateEmpty(FrameW, FrameH, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);

        bool hasBall = anim is "shoot" or "pass" or "receive_ball" or "dribble";
        bool isGK    = role == PlayerRole.Goalkeeper;

        Color shirt   = isGK ? CShirtGK  : CShirtO;
        Color shirtSh = isGK ? CShirtGKS : CShirtOS;
        Color shorts  = isGK ? CShortsGK : CShortsO;

        // ── Body pivot (feet at bottom-centre) ────────────────────────────────
        // Layout in the 48×64 frame:
        //   Feet:  (24, 58)
        //   Knees: (24, 48)
        //   Hip:   (24, 40)
        //   Chest: (24, 26)
        //   Head:  (24, 14) centre

        float phase     = totalFrames > 1 ? (float)frameIdx / totalFrames : 0f;
        float legSwing  = anim is "sprint" ? 14f : anim is "walk" or "dribble" ? 8f : 0f;
        float bodyBob   = anim is "sprint" ? 3f  : anim is "walk" or "dribble" ? 1.5f : 0f;
        bool  isCrouch  = anim is "tackle" or "dive_left" or "dive_right" or "catch";
        bool  isJump    = anim is "celebrate";

        // ── Direction-dependent transforms ────────────────────────────────────
        bool  flipH = dir == "left";
        float faceY = dir is "up" ? -1f : 1f;    // visual vertical tilt
        float bodyOX = dir is "left" or "right" ? 0f : 0f;

        // Body offsets for this frame
        float bobOff   = (float)Math.Sin(phase * Math.PI * 2) * bodyBob;
        float lLegOff  = (float)Math.Sin(phase * Math.PI * 2) * legSwing;
        float rLegOff  = -(float)Math.Sin(phase * Math.PI * 2) * legSwing;
        if (isCrouch) { bobOff = 5f; lLegOff = 0; rLegOff = 0; }
        if (isJump)   { bobOff = (float)-Math.Abs(Math.Sin(phase * Math.PI * 2)) * 8f; }

        // Base character positions
        int cx = 24; // centre X
        int hy = 58; // "hip" baseline from top (feet at hy+6, head at hy-40)
        hy += (int)bobOff;

        // ── Draw layers ───────────────────────────────────────────────────────
        switch (dir)
        {
            case "down":
                DrawPlayerDown(img, cx, hy, shirt, shirtSh, shorts, isGK,
                               lLegOff, rLegOff, isCrouch, phase, anim);
                break;
            case "up":
                DrawPlayerUp(img, cx, hy, shirt, shirtSh, shorts, isGK,
                             lLegOff, rLegOff, isCrouch, phase, anim);
                break;
            default: // left / right (same body, flip handled at sprite level)
                DrawPlayerSide(img, cx, hy, shirt, shirtSh, shorts, isGK,
                               lLegOff, isCrouch, phase, anim, dir);
                break;
        }

        // Ball (when visible)
        if (hasBall)
            DrawBall(img, anim, dir, cx, hy, phase, frameIdx);

        // Goalkeeper gloves (overlay)
        if (isGK && anim is "catch" or "dive_left" or "dive_right" or "throw")
            DrawGloves(img, cx, hy, dir, anim, phase);

        // Flip for left direction
        if (flipH) img = FlipH(img);

        return img;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── Player body painters ──────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────

    private static void DrawPlayerDown(Image img, int cx, int hy,
                                        Color shirt, Color shirtSh, Color shorts,
                                        bool isGK, float lL, float rL,
                                        bool crouch, float phase, string anim)
    {
        // Legs (behind body)
        DrawLeg(img, cx - 4, hy + 2 + (int)lL, CSocksO, CBootO, 6, 14, false);
        DrawLeg(img, cx + 4, hy + 2 + (int)rL, CSocksO, CBootO, 6, 14, false);

        // Shorts
        FillRect(img, cx - 7, hy - 8, 14, 10, shorts);
        FillRect(img, cx - 6, hy - 7, 12, 8,  shorts.Lightened(0.08f));

        // Shirt / torso
        int sh = crouch ? 10 : 16;
        FillRect(img, cx - 8, hy - 24, 16, sh, shirt);
        FillRect(img, cx - 7, hy - 23, 14, sh - 2, shirt.Lightened(0.06f));
        // Shirt shadow sides
        FillCol(img, cx - 8, hy - 24, sh, shirtSh);
        FillCol(img, cx + 7, hy - 24, sh, shirtSh);
        // Kit number (tiny)
        img.SetPixel(cx - 1, hy - 18, Colors.White);
        img.SetPixel(cx,     hy - 18, Colors.White);
        img.SetPixel(cx + 1, hy - 18, Colors.White);

        // Arms
        DrawArm(img, cx - 9,  hy - 20, anim, false, phase);
        DrawArm(img, cx + 9,  hy - 20, anim, true,  phase);

        // Head (facing down — full face)
        DrawHead(img, cx, hy - 30, true);

        // Outline body
        OutlineRect(img, cx - 8, hy - 24, 16, sh, COut);
    }

    private static void DrawPlayerUp(Image img, int cx, int hy,
                                      Color shirt, Color shirtSh, Color shorts,
                                      bool isGK, float lL, float rL,
                                      bool crouch, float phase, string anim)
    {
        // Facing away — back of head, shirt back
        DrawLeg(img, cx - 4, hy + 2 + (int)lL, CSocksO, CBootO, 6, 14, false);
        DrawLeg(img, cx + 4, hy + 2 + (int)rL, CSocksO, CBootO, 6, 14, false);

        FillRect(img, cx - 7, hy - 8, 14, 10, shorts);
        int sh = crouch ? 10 : 16;
        FillRect(img, cx - 8, hy - 24, 16, sh, shirtSh); // back is darker
        // Hair / back of head
        DrawHeadBack(img, cx, hy - 30);
    }

    private static void DrawPlayerSide(Image img, int cx, int hy,
                                        Color shirt, Color shirtSh, Color shorts,
                                        bool isGK, float legSwing,
                                        bool crouch, float phase, string anim, string dir)
    {
        // Front leg (lower z)
        DrawLeg(img, cx + 2, hy + 2 + (int)legSwing, CSocksO, CBootO, 5, 14, true);
        // Back leg
        DrawLeg(img, cx - 3, hy + 2 - (int)legSwing, CSocksO, CBootO, 5, 12, true);

        int sh = crouch ? 10 : 16;
        // Torso (side view, narrower)
        FillRect(img, cx - 5, hy - 24, 12, sh, shirt);
        FillRect(img, cx - 4, hy - 23, 10, sh - 2, shirt.Lightened(0.05f));
        FillCol(img, cx - 5, hy - 24, sh, shirtSh);

        // Shorts
        FillRect(img, cx - 5, hy - 8, 12, 10, shorts);

        // Arms (front arm swings opposite to front leg)
        int armOff = crouch ? 4 : (int)(-legSwing * 0.6f);
        DrawArm(img, cx + 6, hy - 22 + armOff, anim, true, phase);
        DrawArm(img, cx - 6, hy - 20 - armOff, anim, false, phase);

        // Head (side view, 3/4)
        DrawHead(img, cx + 1, hy - 31, false);

        OutlineRect(img, cx - 5, hy - 24, 12, sh, COut);
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static void DrawHead(Image img, int cx, int cy, bool facingDown)
    {
        // Head blob (oval)
        for (int dy = -8; dy <= 6; dy++)
            for (int dx = -7; dx <= 7; dx++)
            {
                float nx = dx / 7f, ny = dy / 7f;
                if (nx * nx + ny * ny < 1.1f)
                    img.SetPixel(cx + dx, cy + dy, CSkin);
            }
        // Hair
        for (int dx = -6; dx <= 6; dx++)
            for (int dy = -8; dy <= -4; dy++)
            {
                float nx = dx / 6f, ny = (dy + 6) / 3f;
                if (nx * nx + ny * ny < 1.2f) img.SetPixel(cx + dx, cy + dy, CHair);
            }
        if (facingDown)
        {
            // Eyes
            img.SetPixel(cx - 3, cy + 1, CEye);
            img.SetPixel(cx + 3, cy + 1, CEye);
            // Mouth
            img.SetPixel(cx - 1, cy + 4, COut);
            img.SetPixel(cx,     cy + 4, COut);
            img.SetPixel(cx + 1, cy + 4, COut);
        }
        // Head shadow underside
        for (int dx = -5; dx <= 5; dx++)
            img.SetPixel(cx + dx, cy + 6, CSkinSh);
    }

    private static void DrawHeadBack(Image img, int cx, int cy)
    {
        // Back of head — hair blob only
        for (int dy = -7; dy <= 5; dy++)
            for (int dx = -7; dx <= 7; dx++)
            {
                float nx = dx / 7f, ny = dy / 6f;
                if (nx * nx + ny * ny < 1.1f)
                    img.SetPixel(cx + dx, cy + dy, CHair);
            }
    }

    private static void DrawLeg(Image img, int cx, int ty, Color sock, Color boot,
                                  int w, int h, bool side)
    {
        // Upper leg (darker shorts colour at top)
        int upper = h / 2;
        FillRect(img, cx - w / 2, ty,         w, upper - 2, sock.Darkened(0.30f));
        // Sock
        FillRect(img, cx - w / 2, ty + upper, w, upper - 4, sock);
        // Boot
        FillRect(img, cx - w / 2 - 1, ty + h - 5, w + 2, 5, boot);
        OutlineRect(img, cx - w / 2 - 1, ty + h - 5, w + 2, 5, COut);
    }

    private static void DrawArm(Image img, int x, int y, string anim, bool right, float phase)
    {
        int h = 8;
        // Raise arm for shoot/celebrate
        int yOff = anim is "shoot" or "celebrate" ? (right ? -6 : -4) : 0;
        // Tackle: arms spread
        if (anim == "tackle") yOff = right ? 4 : 2;

        Color c = CSkin.Darkened(0.05f);
        FillRect(img, x - 2, y + yOff, 4, h, CShirtO.Darkened(0.05f));
        FillRect(img, x - 2, y + yOff + h, 4, 4, c);
    }

    private static void DrawBall(Image img, string anim, string dir,
                                  int cx, int hy, float phase, int frameIdx)
    {
        // Ball position relative to player
        int bx = cx + (dir == "right" ? 10 : dir == "left" ? -10 : 0);
        int by = hy + 6;
        if (anim == "dribble") bx = cx + (int)(Math.Sin(phase * Math.PI * 4) * 8);
        if (anim == "shoot" && frameIdx > 0) { bx += 16; by -= 4; }

        DrawBallAt(img, bx, by, 5);
    }

    private static void DrawBallAt(Image img, int cx, int cy, int r)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy > r * r) continue;
                // Pentagon-style patches
                bool patch = ((dx * 3 + dy * 5) % 7 < 2) && dx * dx + dy * dy > r * r * 0.3f;
                img.SetPixel(cx + dx, cy + dy, patch ? CBallB : CBallW);
            }
        // Highlight
        img.SetPixel(cx - 1, cy - r + 1, Colors.White);
        img.SetPixel(cx - 2, cy - r + 1, Colors.White);
        // Shadow
        for (int dx = -r + 1; dx < r; dx++)
            img.SetPixel(cx + dx, cy + r - 1, CBallSh);
    }

    private static void DrawGloves(Image img, int cx, int hy, string dir,
                                    string anim, float phase)
    {
        int spread = anim is "dive_left" or "dive_right" ? 14 : 6;
        int raise  = anim == "catch" ? 8 : 4;
        FillRect(img, cx - spread - 3, hy - 20 - raise, 6, 5, CGlove);
        FillRect(img, cx + spread - 3, hy - 20 - raise, 6, 5, CGlove);
        OutlineRect(img, cx - spread - 3, hy - 20 - raise, 6, 5, COut);
        OutlineRect(img, cx + spread - 3, hy - 20 - raise, 6, 5, COut);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── Animation definitions ─────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────

    // Returns (animName, fps, frameCount) for each animation
    private static (string, float, int)[] OutfieldAnimDefs() => new[]
    {
        ("idle",         4f, 2),
        ("walk",         8f, 4),
        ("sprint",      10f, 4),
        ("receive_ball", 8f, 3),
        ("pass",        10f, 3),
        ("shoot",       10f, 4),
        ("tackle",       8f, 3),
        ("celebrate",    6f, 4),
        ("dribble",      8f, 4),
    };

    private static (string, float, int)[] GKAnimDefs() => new[]
    {
        ("idle",         4f, 2),
        ("sidestep",     8f, 4),
        ("catch",        8f, 3),
        ("dive_left",    8f, 3),
        ("dive_right",   8f, 3),
        ("kick",        10f, 3),
        ("throw",        8f, 3),
        ("celebrate",    6f, 4),
        ("dribble",      8f, 4),
    };

    // ─────────────────────────────────────────────────────────────────────────
    // ── Pixel helpers ─────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────

    private static void FillRect(Image img, int x0, int y0, int w, int h, Color c)
    {
        int iw = img.GetWidth(), ih = img.GetHeight();
        for (int y = y0; y < y0 + h && y < ih; y++)
            for (int x = x0; x < x0 + w && x < iw; x++)
                if (x >= 0 && y >= 0) img.SetPixel(x, y, c);
    }

    private static void FillCol(Image img, int x, int y0, int h, Color c)
    {
        int ih = img.GetHeight();
        for (int y = y0; y < y0 + h && y < ih; y++)
            if (x >= 0 && x < img.GetWidth() && y >= 0) img.SetPixel(x, y, c);
    }

    private static void OutlineRect(Image img, int x0, int y0, int w, int h, Color c)
    {
        int iw = img.GetWidth(), ih = img.GetHeight();
        for (int x = x0; x < x0 + w && x < iw; x++)
        {
            if (y0 >= 0 && y0 < ih)         img.SetPixel(x, y0,          c);
            if (y0 + h - 1 >= 0 && y0 + h - 1 < ih) img.SetPixel(x, y0 + h - 1, c);
        }
        for (int y = y0; y < y0 + h && y < ih; y++)
        {
            if (x0 >= 0 && x0 < iw)         img.SetPixel(x0,          y, c);
            if (x0 + w - 1 >= 0 && x0 + w - 1 < iw) img.SetPixel(x0 + w - 1, y, c);
        }
    }

    private static Image FlipH(Image src)
    {
        int w = src.GetWidth(), h = src.GetHeight();
        var dst = Image.CreateEmpty(w, h, false, src.GetFormat());
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                dst.SetPixel(w - 1 - x, y, src.GetPixel(x, y));
        return dst;
    }
}
