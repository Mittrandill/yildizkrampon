using Godot;

/// Sahne builder'ları için temel sınıf — SceneTree'den türer.
/// Kullanım: godot --headless --script scenes/BuildXxx.cs
public abstract partial class SceneBuilderBase : SceneTree
{
    protected void PackAndSave(Node root, string path)
    {
        var packed = new PackedScene();
        var err = packed.Pack(root);
        if (err != Error.Ok) { GD.PrintErr($"Pack failed: {err}"); return; }
        err = ResourceSaver.Save(packed, path);
        if (err != Error.Ok) GD.PrintErr($"Save failed: {err}");
        else GD.Print($"BUILT: {_Count(root)} nodes -> {path}");
    }

    private static int _Count(Node n)
    {
        int c = 1;
        foreach (Node ch in n.GetChildren()) c += _Count(ch);
        return c;
    }
}
