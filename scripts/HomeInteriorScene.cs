using Godot;

public partial class HomeInteriorScene : Node2D
{
    private Player?   _player;
    private Camera2D? _camera;
    private Label?    _hint;

    // Kapı eşiği (BuildHomeInterior ile senkron)
    private static readonly Vector2 DoorPos = new Vector2(320f, 456f);
    private static readonly Vector2 BedPos  = new Vector2(80f,  128f);
    private const float DIST = 52f;

    private bool _nearDoor = false;
    private bool _nearBed  = false;

    public override void _Ready()
    {
        _player = GetNodeOrNull<Player>("%Player");
        _camera = GetNodeOrNull<Camera2D>("%Camera");
        _hint   = GetNodeOrNull<Label>("%HintLabel");

        if (_player != null)
            _player.Interacted += _OnInteract;
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;

        _nearDoor = _player.GlobalPosition.DistanceTo(DoorPos) < DIST;
        _nearBed  = _player.GlobalPosition.DistanceTo(BedPos)  < DIST;

        if (_hint != null)
        {
            if      (_nearDoor) { _hint.Text = "E  Dışarı Çık"; _hint.Visible = true; }
            else if (_nearBed)  { _hint.Text = "E  Uyu (Gün +1)"; _hint.Visible = true; }
            else                  _hint.Visible = false;
        }
    }

    private void _OnInteract()
    {
        if (_nearDoor)
        {
            WorldManager.Instance?.GoTo("World");
        }
        else if (_nearBed)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Energy = 100f;
                GameManager.Instance.Day++;
            }
            DialogueManager.Instance?.Show(
                "Yatakta...",
                $"Dinlendirici bir gece geçirdin.\nEnerji yenilendi! Gün {GameManager.Instance?.Day} başlıyor."
            );
        }
    }
}
