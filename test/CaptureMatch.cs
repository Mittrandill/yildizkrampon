using Godot;

/// Capture proof for Task 8: match HUD, controls, rating, tactical hints.
/// Run: dotnet build && godot --path . --write-movie screenshots/result/1/frame.png --fixed-fps 30 --quit-after 450 --script test/CaptureMatch.cs
public partial class CaptureMatch : SceneTree
{
    private float _elapsed      = 0f;
    private bool  _kickedOff   = false;
    private float _shootHold    = 0f;   // how long Space has been held
    private float _shootCycle   = 3.0f; // countdown to next shoot attempt
    private float _passCycle    = 5.0f; // countdown to next pass attempt
    private bool  _passReleased = true;
    private static readonly string[] Dirs = { "move_right", "move_up", "move_left", "move_down" };

    public override void _Initialize()
    {
        ChangeSceneToFile("res://scenes/match.tscn");
    }

    public override bool _Process(double delta)
    {
        _elapsed += (float)delta;

        // Give scene 1 frame to settle
        if (_elapsed < 0.05f) return false;

        // Kick off: press action once at t=0.1s
        if (!_kickedOff && _elapsed >= 0.1f)
        {
            _Fire("action", true);
            _kickedOff = true;
        }
        if (_kickedOff && _elapsed >= 0.2f)
            _Fire("action", false);

        // Closed-loop steering: chase the ball
        _SteerToBall();

        // Pass cycle (F key)
        _passCycle -= (float)delta;
        if (_passCycle <= 0f && _passReleased)
        {
            _Fire("pass", true);
            _passReleased = false;
            _passCycle = 0.08f; // hold briefly, then release
        }
        else if (!_passReleased)
        {
            _Fire("pass", false);
            _passReleased = true;
            _passCycle = 4.0f + (GD.Randf() * 2.0f);
        }

        // Shoot cycle (Space charge + release)
        _shootCycle -= (float)delta;
        if (_shootCycle <= 0f)
        {
            if (_shootHold < 0.7f)
            {
                _Fire("action", true); // hold Space to charge
                _shootHold += (float)delta;
            }
            else
            {
                _Fire("action", false); // release
                _shootHold  = 0f;
                _shootCycle = 5.0f + (GD.Randf() * 3.0f);
            }
        }

        return false; // quit-after handles exit
    }

    private void _SteerToBall()
    {
        var ball = Football.Instance;
        if (ball == null || MatchManager.Instance == null) return;

        // Find human player (nearest red to ball, as MatchManager would pick)
        FieldPlayer? human = null;
        foreach (Node n in Root.GetTree().GetNodesInGroup("team_red"))
        {
            if (n is FieldPlayer fp && MatchManager.Instance.IsHumanControlled(fp))
            {
                human = fp;
                break;
            }
        }
        if (human == null) return;

        Vector2 diff = ball.GlobalPosition - human.GlobalPosition;
        float dist = diff.Length();

        if (dist < 30f)
        {
            // Ball is close — face toward opponent goal
            diff = new Vector2(1f, 0f);
        }

        // Release all directions first
        foreach (var d in Dirs) _Fire(d, false);

        // Steer toward ball
        bool goRight  = diff.X > 25f;
        bool goLeft   = diff.X < -25f;
        bool goDown   = diff.Y > 25f;
        bool goUp     = diff.Y < -25f;

        if (goRight) _Fire("move_right", true);
        if (goLeft)  _Fire("move_left",  true);
        if (goDown)  _Fire("move_down",  true);
        if (goUp)    _Fire("move_up",    true);

        // Sprint when far from ball
        _Fire("sprint", dist > 150f);
    }

    private static void _Fire(string action, bool pressed)
    {
        var ev = new InputEventAction();
        ev.Action    = action;
        ev.Pressed   = pressed;
        Input.ParseInputEvent(ev);
    }
}
