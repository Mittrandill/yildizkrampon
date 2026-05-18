using Godot;
using System;

/// Basit diyalog sistemi — metni kutu içinde göster, callback ile kapat.
public partial class DialogueManager : CanvasLayer
{
    public static DialogueManager Instance { get; private set; } = null!;

    private Panel?   _box;
    private Label?   _nameLabel;
    private Label?   _bodyLabel;
    private Button?  _continueBtn;
    private Action?  _onClose;

    public override void _Ready()
    {
        Instance = this;
        Layer = 100;
        _Build();
        _box!.Visible = false;
    }

    private void _Build()
    {
        _box = new Panel();
        _box.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _box.Position = new Vector2(0, -180);
        _box.Size = new Vector2(1280, 180);
        var sf = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.06f, 0.04f, 0.94f),
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            BorderColor = new Color(0.75f, 0.60f, 0.30f, 0.90f)
        };
        sf.BorderWidthTop = sf.BorderWidthLeft = sf.BorderWidthRight = sf.BorderWidthBottom = 2;
        _box.AddThemeStyleboxOverride("panel", sf);
        AddChild(_box);

        _nameLabel = new Label
        {
            Text = "", Position = new Vector2(28, 14)
        };
        _nameLabel.AddThemeFontSizeOverride("font_size", 20);
        _nameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.20f));
        _box.AddChild(_nameLabel);

        _bodyLabel = new Label
        {
            Text = "", Position = new Vector2(28, 46),
            AutowrapMode = TextServer.AutowrapMode.Word,
            Size = new Vector2(1180, 100)
        };
        _bodyLabel.AddThemeFontSizeOverride("font_size", 17);
        _bodyLabel.AddThemeColorOverride("font_color", Colors.White);
        _box.AddChild(_bodyLabel);

        _continueBtn = new Button
        {
            Text = "Devam (E)",
            Position = new Vector2(1130, 140),
            Size = new Vector2(120, 30)
        };
        _continueBtn.AddThemeFontSizeOverride("font_size", 13);
        _continueBtn.Pressed += _Close;
        _box.AddChild(_continueBtn);
    }

    public void Show(string speaker, string body, Action? onClose = null)
    {
        _nameLabel!.Text = speaker;
        _bodyLabel!.Text = body;
        _onClose = onClose;
        _box!.Visible = true;
        GetTree().Paused = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_box?.Visible != true) return;
        if (@event is InputEventKey k && k.Pressed && !k.Echo
            && (k.Keycode == Key.E || k.Keycode == Key.Enter || k.Keycode == Key.Space))
            _Close();
    }

    private void _Close()
    {
        _box!.Visible = false;
        GetTree().Paused = false;
        _onClose?.Invoke();
        _onClose = null;
    }
}
