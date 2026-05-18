using Godot;

/// res://scripts/Player.cs
/// Stardew Valley tarzı 4 yönlü hareket + animasyon + etkileşim.
public partial class Player : CharacterBody2D
{
    private const float WALK_SPEED   = 90f;
    private const float SPRINT_SPEED = 145f;

    public enum Dir { Down, Up, Left, Right }
    public Dir FaceDir { get; private set; } = Dir.Down;

    private AnimatedSprite2D? _anim;
    private Sprite2D? _shadow;
    private float _walkCycle = 0f;
    private const float _BASE_Y = -14f;

    public override void _Ready()
    {
        _BuildSprite();
        _BuildShadow();
    }

    private void _BuildSprite()
    {
        _anim = new AnimatedSprite2D { Name = "Anim", Position = new Vector2(0, _BASE_Y) };

        var frames = new SpriteFrames();
        string[] anims = {
            "idle_down","walk_down","idle_up","walk_up",
            "idle_left","walk_left","idle_right","walk_right"
        };
        foreach (var a in anims)
        {
            frames.AddAnimation(a);
            frames.SetAnimationLoop(a, true);
            frames.SetAnimationSpeed(a, a.StartsWith("walk") ? 8f : 2f);
        }

        _anim.SpriteFrames = frames;
        AddChild(_anim);
        _RefreshFrames();
        _anim.Play("idle_down");
    }

    private void _RefreshFrames()
    {
        if (_anim == null) return;
        var frames = _anim.SpriteFrames;

        // Temel duruş sprite'ları
        Texture2D south = _Tex("res://assets/characters/player_south.png",      new Color(0.9f, 0.2f, 0.2f));
        Texture2D north = _Tex("res://assets/characters/player_north.png",      new Color(0.6f, 0.2f, 0.2f));
        Texture2D side  = _Tex("res://assets/characters/player_side.png",       new Color(0.8f, 0.2f, 0.2f));

        // Yürüyüş kareleri — her yön için A ve B adım
        Texture2D downA = _Tex("res://assets/characters/player_walk_south_a.png", south);
        Texture2D downB = _Tex("res://assets/characters/player_walk_south_b.png", south);
        Texture2D upA   = _Tex("res://assets/characters/player_walk_north_a.png", north);
        Texture2D upB   = _Tex("res://assets/characters/player_walk_north_b.png", north);
        Texture2D sideA = _Tex("res://assets/characters/player_walk_a.png",       side);
        Texture2D sideB = _Tex("res://assets/characters/player_walk_side_b.png",  side);

        // 4-kare akıcı döngü: duruş → adım A → duruş → adım B
        _Fill(frames, "idle_down",  2f, south);
        _Fill(frames, "walk_down",  8f, south, downA, south, downB);
        _Fill(frames, "idle_up",    2f, north);
        _Fill(frames, "walk_up",    8f, north, upA, north, upB);
        // Sol/sağ aynı frame seti, FlipH ile yön ayrılır
        _Fill(frames, "idle_left",  2f, side);
        _Fill(frames, "walk_left",  8f, side, sideA, side, sideB);
        _Fill(frames, "idle_right", 2f, side);
        _Fill(frames, "walk_right", 8f, side, sideA, side, sideB);
    }

    private static Texture2D _Tex(string path, Color fallback)
        => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : _Placeholder(fallback);

    private static Texture2D _Tex(string path, Texture2D fallback)
        => ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : fallback;

    private static void _Fill(SpriteFrames f, string name, float fps, params Texture2D[] texs)
    {
        while (f.GetFrameCount(name) > 0) f.RemoveFrame(name, 0);
        f.SetAnimationSpeed(name, fps);
        foreach (var t in texs) f.AddFrame(name, t);
    }

    private static ImageTexture _Placeholder(Color c)
    {
        var img = Image.CreateEmpty(48, 72, false, Image.Format.Rgba8);
        img.Fill(c);
        return ImageTexture.CreateFromImage(img);
    }

    private void _BuildShadow()
    {
        var img = Image.CreateEmpty(32, 10, false, Image.Format.Rgba8);
        img.Fill(new Color(0f, 0f, 0f, 0.25f));
        _shadow = new Sprite2D
        {
            Texture  = ImageTexture.CreateFromImage(img),
            Position = new Vector2(0, 8),
            ZIndex   = -1
        };
        AddChild(_shadow);
    }

    public override void _PhysicsProcess(double delta)
    {
        var input  = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        bool sprint = Input.IsActionPressed("sprint");
        Velocity   = input * (sprint ? SPRINT_SPEED : WALK_SPEED);
        MoveAndSlide();
        _UpdateFacing(input);
        _UpdateAnim(input, (float)delta);
    }

    private void _UpdateFacing(Vector2 input)
    {
        if (input.LengthSquared() < 0.01f) return;
        FaceDir = Mathf.Abs(input.X) > Mathf.Abs(input.Y)
            ? (input.X > 0 ? Dir.Right : Dir.Left)
            : (input.Y > 0 ? Dir.Down  : Dir.Up);
    }

    private void _UpdateAnim(Vector2 input, float delta)
    {
        if (_anim == null) return;

        bool moving = input.LengthSquared() > 0.01f;

        // Hafif dikey bob — tüm yönlerde tutarlı, uçma hissi yok
        if (moving)
        {
            _walkCycle += delta * 10f;
            _anim.Position = new Vector2(0, _BASE_Y + Mathf.Sin(_walkCycle * Mathf.Pi) * 1.2f);
        }
        else
        {
            _walkCycle = 0f;
            _anim.Position = new Vector2(0, _BASE_Y);
        }

        // Sol yürüyüş: aynı frame seti, sadece yatay çevrilmiş
        _anim.FlipH = FaceDir == Dir.Left;

        string dir = FaceDir switch
        {
            Dir.Up    => "up",
            Dir.Left  => "left",
            Dir.Right => "right",
            _         => "down"
        };

        string animName = (moving ? "walk_" : "idle_") + dir;
        if (_anim.Animation != animName) _anim.Play(animName);
    }

    [Signal]
    public delegate void InteractedEventHandler();

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.E)
            EmitSignal(SignalName.Interacted);
    }

    private Vector2 _FaceDirVec() => FaceDir switch
    {
        Dir.Up    => Vector2.Up,
        Dir.Left  => Vector2.Left,
        Dir.Right => Vector2.Right,
        _         => Vector2.Down
    };
}
