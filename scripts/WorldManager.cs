using Godot;
using System.Collections.Generic;

/// res://scripts/WorldManager.cs
/// Autoload — konum geçişlerini yönetir. SequenceManager'ın yerini alır.
/// Kullanım: WorldManager.Instance.GoTo("HomeInterior");
public partial class WorldManager : Node
{
    public static WorldManager Instance { get; private set; } = null!;

    public string CurrentLocation  { get; private set; } = "CharacterCreation";
    public string PreviousLocation { get; private set; } = "";

    private static readonly Dictionary<string, string> LocationPaths = new()
    {
        { "CharacterCreation",  "res://scenes/CharacterCreation.tscn"   },
        { "WorldMap",           "res://scenes/WorldMap.tscn"            },
        { "HomeInterior",       "res://scenes/HomeInterior.tscn"        },
        { "BakkalInterior",     "res://scenes/BakkalInterior.tscn"      },
        { "SporTesisi",         "res://scenes/SporTesisiInterior.tscn"  },
        { "Fitness",            "res://scenes/FitnessSalonu.tscn"       },
        { "CayBahcesi",         "res://scenes/CayBahcesi.tscn"          },
        { "Match",              "res://scenes/match.tscn"               },
    };

    private ColorRect _fadeRect = null!;
    private Tween?    _tween;
    private bool      _transitioning = false;

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
        _fadeRect.Color    = Colors.Black;
        _fadeRect.Modulate = new Color(0, 0, 0, 0);
        _fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        canvas.AddChild(_fadeRect);
        AddChild(canvas);
    }

    public void GoTo(string location)
    {
        if (_transitioning) return;

        if (!LocationPaths.TryGetValue(location, out string? path))
        {
            GD.PrintErr($"WorldManager: bilinmeyen konum '{location}'");
            return;
        }

        // Hedef sahne henüz yapılmadıysa match'e fallback
        if (!ResourceLoader.Exists(path))
        {
            GD.PrintErr($"WorldManager: sahne bulunamadı '{path}' — match'e yönlendiriliyor");
            path     = LocationPaths["Match"];
            location = "Match";
        }

        _transitioning    = true;
        PreviousLocation  = CurrentLocation;
        CurrentLocation   = location;

        GameTime.Instance?.Pause();

        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(_fadeRect, "modulate:a", 1.0f, 0.3f);
        _tween.TweenCallback(Callable.From(() =>
        {
            DialogueManager.Instance.Hide();
            GetTree().ChangeSceneToFile(path);
            _transitioning = false;
        }));
        _tween.TweenProperty(_fadeRect, "modulate:a", 0.0f, 0.3f);
    }

    public void GoBack() => GoTo(PreviousLocation);

    public void FadeIn()
    {
        _fadeRect.Modulate = new Color(0, 0, 0, 1);
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(_fadeRect, "modulate:a", 0.0f, 0.4f);
    }
}
