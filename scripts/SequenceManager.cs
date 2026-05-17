using Godot;

/// res://scripts/SequenceManager.cs
/// Autoload singleton — manages the first-day sequence and fade transitions.
public partial class SequenceManager : Node
{
    public static SequenceManager Instance { get; private set; } = null!;

    // Scene paths in sequence order
    private static readonly string[] DayScenes = new[]
    {
        "res://scenes/bedroom.tscn",
        "res://scenes/kitchen.tscn",
        "res://scenes/neighborhood.tscn",
        "res://scenes/match.tscn",
        "res://scenes/postmatch.tscn",
        "res://scenes/shop.tscn",
        "res://scenes/training.tscn",
        "res://scenes/eod_summary.tscn",
        "res://scenes/next_morning.tscn",
    };

    public int CurrentStep { get; private set; } = 0;

    private ColorRect _fadeRect = null!;
    private Tween? _tween;
    private bool _transitioning = false;

    public override void _Ready()
    {
        Instance = this;
        _SetupFadeOverlay();
    }

    private void _SetupFadeOverlay()
    {
        var canvas = new CanvasLayer();
        canvas.Layer = 100;
        _fadeRect = new ColorRect();
        _fadeRect.Color = Colors.Black;
        _fadeRect.Modulate = new Color(0, 0, 0, 0);
        _fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        canvas.AddChild(_fadeRect);
        AddChild(canvas);
    }

    public void GoToNextScene()
    {
        if (_transitioning) return;
        CurrentStep++;
        if (CurrentStep >= DayScenes.Length)
        {
            CurrentStep = DayScenes.Length - 1;
            return;
        }
        _FadeAndChange(DayScenes[CurrentStep]);
    }

    public void GoToScene(string path)
    {
        if (_transitioning) return;
        _FadeAndChange(path);
    }

    private void _FadeAndChange(string scenePath)
    {
        _transitioning = true;
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(_fadeRect, "modulate:a", 1.0f, 0.4f);
        _tween.TweenCallback(Callable.From(() =>
        {
            // Clear any pending dialogue state so stale callbacks don't fire in the new scene
            DialogueManager.Instance.Hide();
            GetTree().ChangeSceneToFile(scenePath);
            _transitioning = false;
        }));
        _tween.TweenProperty(_fadeRect, "modulate:a", 0.0f, 0.4f);
    }

    public void FadeIn()
    {
        _fadeRect.Modulate = new Color(0, 0, 0, 1);
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(_fadeRect, "modulate:a", 0.0f, 0.5f);
    }
}
