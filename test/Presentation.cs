using Godot;

/// Automated presentation capture — drives the full first-day sequence without player input.
/// Run: dotnet build && godot --path . --write-movie screenshots/result/1/frame.png --fixed-fps 30 --quit-after 900 --script test/Presentation.cs
public partial class Presentation : SceneTree
{
    private float _elapsed = 0f;
    private float _sceneElapsed = 0f;
    private string _prevScene = "";
    private float _lastActionTime = -99f;
    private float _lastDirChangeTime = 0f;
    private int _dirIndex = 0;
    private bool _actionWasPressed = false;

    // How long to stay in each scene before forcing advance
    private static readonly System.Collections.Generic.Dictionary<string, float> SceneLimits = new()
    {
        { "Bedroom",      3.0f },
        { "Kitchen",      3.0f },
        { "Neighborhood", 3.0f },
        { "Match",        8.0f },   // match ends via PresentationMode (5s) + dialogue + transition
        { "PostMatch",    3.5f },
        { "Shop",         3.0f },
        { "Training",     5.0f },
        { "EodSummary",   3.0f },
        { "NextMorning",  3.5f },
    };

    private static readonly string[] DirectionActions = { "move_right", "move_up", "move_left", "move_down" };
    private string _currentDir = "";

    public override void _Initialize()
    {
        ChangeSceneToFile("res://scenes/CharacterCreation.tscn");
    }

    public override bool _Process(double delta)
    {
        _elapsed += (float)delta;
        _sceneElapsed += (float)delta;

        var scene = Root.GetNodeOrNull<Node>("Bedroom")
            ?? Root.GetNodeOrNull<Node>("Kitchen")
            ?? Root.GetNodeOrNull<Node>("Neighborhood")
            ?? Root.GetNodeOrNull<Node>("Match")
            ?? Root.GetNodeOrNull<Node>("PostMatch")
            ?? Root.GetNodeOrNull<Node>("Shop")
            ?? Root.GetNodeOrNull<Node>("Training")
            ?? Root.GetNodeOrNull<Node>("EodSummary")
            ?? Root.GetNodeOrNull<Node>("NextMorning");

        string sceneName = scene?.Name ?? "";

        if (sceneName != _prevScene)
        {
            _prevScene = sceneName;
            _sceneElapsed = 0f;
            GD.Print($"SCENE: {sceneName} at t={_elapsed:F1}s");
        }

        // Release action if it was pressed last frame
        if (_actionWasPressed)
        {
            _FireInput("action", false);
            _actionWasPressed = false;
        }

        // Auto-advance dialogue every 0.75s
        if (_elapsed - _lastActionTime > 0.75f)
        {
            _FireInput("action", true);
            _actionWasPressed = true;
            _lastActionTime = _elapsed;
        }

        // In match scene: simulate player movement toward ball
        if (sceneName == "Match")
        {
            _SimulateMatchInput();
        }
        else
        {
            _StopMovement();
        }

        // Force scene advance if time limit exceeded
        if (SceneLimits.TryGetValue(sceneName, out float limit) && _sceneElapsed > limit)
        {
            _sceneElapsed = 0f;
            if (sceneName == "NextMorning")
            {
                // Last scene — let quit-after handle exit
            }
            else
            {
                var sm = _FindAutoload("SequenceManager");
                if (sm != null)
                {
                    sm.Call("GoToNextScene");
                }
            }
        }

        return false; // keep running — quit-after handles exit
    }

    private void _FireInput(string action, bool pressed)
    {
        var ev = new InputEventAction();
        ev.Action = action;
        ev.Pressed = pressed;
        Input.ParseInputEvent(ev);
    }

    private void _SimulateMatchInput()
    {
        if (_elapsed - _lastDirChangeTime > 0.8f)
        {
            _StopMovement();
            _dirIndex = (_dirIndex + 1) % DirectionActions.Length;
            _currentDir = DirectionActions[_dirIndex];
            _FireInput(_currentDir, true);
            _lastDirChangeTime = _elapsed;
        }
    }

    private void _StopMovement()
    {
        foreach (var dir in DirectionActions)
            _FireInput(dir, false);
        _currentDir = "";
    }

    private Node? _FindAutoload(string name)
    {
        foreach (var child in Root.GetChildren())
        {
            if (child.Name == name) return child;
        }
        return null;
    }
}
