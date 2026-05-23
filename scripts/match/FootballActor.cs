using Godot;
using static FootballSpriteGen;

/// Pixel-art football actor (outfield player or goalkeeper).
/// Uses FootballSpriteGen to build all animation frames procedurally.
///
/// Integration in MatchScene:
///   var actor = new FootballActor { Role = PlayerRole.Outfield };
///   matchScene.YSortLayer!.AddChild(actor);
///   actor.GlobalPosition = FieldRegions.FieldCenter;
///
/// Animation API:
///   PlayAnim("idle")        — all directions auto-handled
///   SetFacing(Vector2)      — updates direction from velocity
///   TriggerAction("shoot")  — one-shot action, returns to idle after
///
/// Y-sort depth:
///   ZIndex is updated every frame to (int)GlobalPosition.Y so actors
///   depth-sort naturally relative to each other and the foreground.
public partial class FootballActor : CharacterBody2D
{
    // ── Config ────────────────────────────────────────────────────────────────
    [Export] public PlayerRole Role { get; set; } = PlayerRole.Outfield;
    [Export] public float WalkSpeed   { get; set; } = 85f;
    [Export] public float SprintSpeed { get; set; } = 135f;

    // ── Animation state ───────────────────────────────────────────────────────
    private AnimatedSprite2D? _anim;
    private Sprite2D?          _shadow;
    private string _currentAnim  = "idle";
    private string _dir          = "down";
    private string? _actionAnim  = null;   // active one-shot action
    private float   _actionTimer = 0f;
    private const float ACTION_DURATION = 0.5f;

    // Pivot offset so feet land at CharacterBody2D origin
    private const float PIVOT_Y = -28f; // move sprite up by 28px (feet at 0)

    // ─────────────────────────────────────────────────────────────────────────
    public override void _Ready()
    {
        BuildSprite();
        _shadow = BuildShadow();
        AddChild(_shadow);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public override void _Process(double delta)
    {
        // Y-sort depth by feet position
        ZIndex = (int)(GlobalPosition.Y * 0.1f);

        // Action timer
        if (_actionAnim != null)
        {
            _actionTimer -= (float)delta;
            if (_actionTimer <= 0f)
            {
                _actionAnim = null;
                PlayAnim(_currentAnim);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── Movement API (called by AI / player controller) ───────────────────────
    // ─────────────────────────────────────────────────────────────────────────

    public void Move(Vector2 direction, bool sprint = false)
    {
        Velocity = direction.Normalized() * (sprint ? SprintSpeed : WalkSpeed);
        MoveAndSlide();

        if (direction.LengthSquared() > 0.01f)
        {
            SetFacing(direction);
            if (_actionAnim == null) PlayAnim(sprint ? "sprint" : "walk");
        }
        else
        {
            if (_actionAnim == null) PlayAnim("idle");
        }
    }

    /// Update facing direction from a velocity vector.
    public void SetFacing(Vector2 v)
    {
        if (v.LengthSquared() < 0.01f) return;
        _dir = Mathf.Abs(v.X) > Mathf.Abs(v.Y)
            ? (v.X > 0 ? "right" : "left")
            : (v.Y > 0 ? "down"  : "up");
    }

    /// Trigger a one-shot action animation ("shoot", "pass", "tackle", etc.)
    public void TriggerAction(string action)
    {
        _actionAnim  = action;
        _actionTimer = ACTION_DURATION;
        PlayAnim(action);
    }

    /// Set looping animation base name ("idle", "walk", "sprint", …)
    public void PlayAnim(string animBase)
    {
        if (_anim == null) return;
        string fullName = $"{animBase}_{_dir}";

        if (!_anim.SpriteFrames.HasAnimation(fullName))
            fullName = $"idle_{_dir}"; // fallback

        if (_anim.Animation.ToString() != fullName)
            _anim.Play(fullName);

        _currentAnim = animBase;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── Build ─────────────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildSprite()
    {
        var frames = FootballSpriteGen.BuildSpriteFrames(Role);

        _anim = new AnimatedSprite2D
        {
            Name          = "Anim",
            SpriteFrames  = frames,
            Position      = new Vector2(0, PIVOT_Y),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };

        // Optional white-bg removal shader (reuse project shader)
        if (ResourceLoader.Exists("res://shaders/remove_white_bg.gdshader"))
            _anim.Material = new ShaderMaterial
            {
                Shader = GD.Load<Shader>("res://shaders/remove_white_bg.gdshader")
            };

        AddChild(_anim);
        _anim.Play("idle_down");
    }
}
