using Godot;

/// Sahne geçişleri.
public partial class WorldManager : Node
{
    public static WorldManager Instance { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public void GoTo(string sceneName)
    {
        string path = $"res://scenes/{sceneName}.tscn";
        GetTree().ChangeSceneToFile(path);
    }
}
