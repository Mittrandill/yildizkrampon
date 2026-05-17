using Godot;
using System;
using System.Collections.Generic;

/// res://scripts/DialogueManager.cs
/// Autoload singleton — displays dialogue boxes and choice menus over any scene.
public partial class DialogueManager : Node
{
    [Signal] public delegate void DialogueFinishedEventHandler();
    [Signal] public delegate void ChoiceSelectedEventHandler(int index);

    public static DialogueManager Instance { get; private set; } = null!;

    private Panel _panel = null!;
    private Label _speakerLabel = null!;
    private Label _textLabel = null!;
    private VBoxContainer _choicesContainer = null!;
    private CanvasLayer _canvas = null!;

    private Queue<(string speaker, string text)> _queue = new();
    private Action? _onFinished;
    private bool _waitingForInput = false;

    public override void _Ready()
    {
        Instance = this;
        _BuildUI();
    }

    private void _BuildUI()
    {
        _canvas = new CanvasLayer();
        _canvas.Layer = 50;
        AddChild(_canvas);

        _panel = new Panel();
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        _panel.OffsetTop = -180;
        _panel.OffsetLeft = 20;
        _panel.OffsetRight = -20;
        _panel.OffsetBottom = -20;
        _panel.Visible = false;
        _canvas.AddChild(_panel);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("margin_left", 16);
        vbox.AddThemeConstantOverride("margin_right", 16);
        vbox.AddThemeConstantOverride("margin_top", 12);
        vbox.AddThemeConstantOverride("margin_bottom", 12);
        _panel.AddChild(vbox);

        _speakerLabel = new Label();
        _speakerLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.3f));
        vbox.AddChild(_speakerLabel);

        _textLabel = new Label();
        _textLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        vbox.AddChild(_textLabel);

        _choicesContainer = new VBoxContainer();
        vbox.AddChild(_choicesContainer);
    }

    public override void _Input(InputEvent @event)
    {
        if (_waitingForInput && _choicesContainer.GetChildCount() == 0
            && @event.IsActionPressed("action"))
        {
            _Advance();
        }
    }

    public void Show(string speaker, string text, Action? onFinished = null)
    {
        _queue.Clear();
        _queue.Enqueue((speaker, text));
        _onFinished = onFinished;
        _ShowNext();
    }

    public void ShowSequence(List<(string speaker, string text)> lines, Action? onFinished = null)
    {
        _queue.Clear();
        foreach (var line in lines) _queue.Enqueue(line);
        _onFinished = onFinished;
        _ShowNext();
    }

    public void ShowChoice(string speaker, string text, List<string> options, Action<int> onChoice)
    {
        _panel.Visible = true;
        _speakerLabel.Text = speaker;
        _textLabel.Text = text;
        _waitingForInput = false;

        foreach (var child in _choicesContainer.GetChildren())
            child.QueueFree();

        for (int i = 0; i < options.Count; i++)
        {
            var btn = new Button();
            btn.Text = options[i];
            int captured = i;
            btn.Pressed += () =>
            {
                foreach (var c in _choicesContainer.GetChildren()) c.QueueFree();
                _panel.Visible = false;
                onChoice(captured);
            };
            _choicesContainer.AddChild(btn);
        }
    }

    public void Hide()
    {
        _panel.Visible = false;
        _queue.Clear();
        _waitingForInput = false;
    }

    private void _ShowNext()
    {
        if (_queue.Count == 0)
        {
            _panel.Visible = false;
            _waitingForInput = false;
            _onFinished?.Invoke();
            return;
        }
        var (speaker, text) = _queue.Dequeue();
        _panel.Visible = true;
        _speakerLabel.Text = speaker;
        _textLabel.Text = text;
        _waitingForInput = true;
    }

    private void _Advance()
    {
        _waitingForInput = false;
        _ShowNext();
    }
}
