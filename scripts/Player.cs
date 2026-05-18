using Godot;

/// res://scripts/Player.cs
/// Stardew Valley tarzı 4 yönlü hareket + animasyon + etkileşim.
public partial class Player : CharacterBody2D
{
    // ─── Sabitler ───────────────────────────────────────────────
    private const float WALK_SPEED   = 90f;
    private const float SPRINT_SPEED = 145f;

    // ─── Yön ────────────────────────────────────────────────────
    public enum Dir { Down, Up, Left, Right }
    public Dir FaceDir { get; private set; } = Dir.Down;

    // ─── Sprite ─────────────────────────────────────────────────
    private AnimatedSprite2D? _anim;

    // ─── Gölge ──────────────────────────────────────────────────
    private Sprite2D? _shadow;

    public override void _Ready()
    {
        _BuildSprite();
        _BuildShadow();
    }

    // ─── Sprite kurulum ─────────────────────────────────────────

    private void _BuildSprite()
    {
        _anim = new AnimatedSprite2D();
        _anim.Name = "Anim";
        _anim.Position = new Vector2(0, -14);

        var frames = new SpriteFrames();

        // Her yön için: idle (1 kare) + walk (3 kare)
        // Şimdilik tüm animasyonları aynı texture ile kur —
        // texture yüklendiğinde _RefreshFrames() günceller
        string[] anims = { "idle_down","walk_down","idle_up","walk_up","idle_left","walk_left","idle_right","walk_right" };
        foreach (var a in anims)
        {
            frames.AddAnimation(a);
            frames.SetAnimationLoop(a, true);
            frames.SetAnimationSpeed(a, a.StartsWith("walk") ? 8f : 4f);
        }

        _anim.SpriteFrames = frames;
        AddChild(_anim);

        // Texture yüklendikten sonra frameları doldur
        _RefreshFrames();
        _anim.Play("idle_down");
    }

    private void _RefreshFrames()
    {
        if (_anim == null) return;
        var frames = _anim.SpriteFrames;

        string refPath  = "res://assets/characters/player_south.png";
        string northPath = "res://assets/characters/player_north.png";
        string sidePath  = "res://assets/characters/player_side.png";
        string walkAPath = "res://assets/characters/player_walk_a.png";
        string walkBPath = "res://assets/characters/player_walk_b.png";

        // Sadece var olan dosyaları yükle — yoksa fallback
        Texture2D south  = ResourceLoader.Exists(refPath)   ? GD.Load<Texture2D>(refPath)   : _MakePlaceholder(new Color(0.9f, 0.2f, 0.2f));
        Texture2D north  = ResourceLoader.Exists(northPath) ? GD.Load<Texture2D>(northPath) : _MakePlaceholder(new Color(0.6f, 0.2f, 0.2f));
        Texture2D side   = ResourceLoader.Exists(sidePath)  ? GD.Load<Texture2D>(sidePath)  : _MakePlaceholder(new Color(0.8f, 0.2f, 0.2f));
        Texture2D walkA  = ResourceLoader.Exists(walkAPath) ? GD.Load<Texture2D>(walkAPath) : south;
        Texture2D walkB  = ResourceLoader.Exists(walkBPath) ? GD.Load<Texture2D>(walkBPath) : side;

        _FillAnim(frames, "idle_down",  4f, south);
        _FillAnim(frames, "walk_down",  8f, south, walkA, south, walkB);
        _FillAnim(frames, "idle_up",    4f, north);
        _FillAnim(frames, "walk_up",    8f, north, walkA, north, walkB);
        _FillAnim(frames, "idle_left",  4f, side);
        _FillAnim(frames, "walk_left",  8f, side, walkA, side, walkB);
        _FillAnim(frames, "idle_right", 4f, side);
        _FillAnim(frames, "walk_right", 8f, side, walkA, side, walkB);
    }

    private static void _FillAnim(SpriteFrames f, string name, float fps, params Texture2D[] texs)
    {
        // Önce eski kareleri sil
        while (f.GetFrameCount(name) > 0) f.RemoveFrame(name, 0);
        f.SetAnimationSpeed(name, fps);
        foreach (var t in texs) f.AddFrame(name, t);
    }

    private static ImageTexture _MakePlaceholder(Color c)
    {
        var img = Image.CreateEmpty(48, 72, false, Image.Format.Rgba8);
        img.Fill(c);
        return ImageTexture.CreateFromImage(img);
    }

    private void _BuildShadow()
    {
        var shadowImg = Image.CreateEmpty(32, 12, false, Image.Format.Rgba8);
        shadowImg.Fill(new Color(0f, 0f, 0f, 0.30f));
        var shadowTex = ImageTexture.CreateFromImage(shadowImg);

        _shadow = new Sprite2D { Texture = shadowTex, Position = new Vector2(0, 6), ZIndex = -1 };
        AddChild(_shadow);
    }

    // ─── Fizik ──────────────────────────────────────────────────

    public override void _PhysicsProcess(double delta)
    {
        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        bool sprint = Input.IsActionPressed("sprint");

        float spd = sprint ? SPRINT_SPEED : WALK_SPEED;
        Velocity = input * spd;
        MoveAndSlide();

        _UpdateFacing(input);
        _UpdateAnim(input);
    }

    private void _UpdateFacing(Vector2 input)
    {
        if (input.LengthSquared() < 0.01f) return;

        if (Mathf.Abs(input.X) > Mathf.Abs(input.Y))
            FaceDir = input.X > 0 ? Dir.Right : Dir.Left;
        else
            FaceDir = input.Y > 0 ? Dir.Down : Dir.Up;
    }

    private void _UpdateAnim(Vector2 input)
    {
        if (_anim == null) return;

        bool moving = input.LengthSquared() > 0.01f;
        string prefix = moving ? "walk" : "idle";

        string dir = FaceDir switch
        {
            Dir.Up    => "up",
            Dir.Left  => "left",
            Dir.Right => "right",
            _         => "down"
        };

        // Sol-sağ aynalama: sadece side sprite var
        if (FaceDir == Dir.Left)
            _anim.FlipH = true;
        else if (FaceDir == Dir.Right)
            _anim.FlipH = false;
        else
            _anim.FlipH = false;

        string animName = $"{prefix}_{dir}";
        if (_anim.Animation != animName) _anim.Play(animName);
    }

    // ─── Etkileşim ──────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.E)
            _TryInteract();
    }

    private void _TryInteract()
    {
        // Önde bir Area2D var mı?
        Vector2 checkPos = GlobalPosition + _FaceDirVec() * 24f;
        // TODO: Area2D overlap check for NPC/object interaction
    }

    private Vector2 _FaceDirVec() => FaceDir switch
    {
        Dir.Up    => Vector2.Up,
        Dir.Left  => Vector2.Left,
        Dir.Right => Vector2.Right,
        _         => Vector2.Down
    };
}
