using Godot;
using System.Collections.Generic;

/// res://scripts/NeighborhoodScene.cs
/// Neighborhood walk — meet Riza Abi, then head to the pitch.
public partial class NeighborhoodScene : Node2D
{
    public override void _Ready()
    {
        SequenceManager.Instance.FadeIn();
        GameManager.Instance.DayStep = 0;

        var lines = new List<(string, string)>
        {
            ("Anlatıcı", "Dar sokaktan geçip mahalleye çıktın. Denizin sesi uzaktan geliyor."),
            ("Rıza Abi", "Gel gel genç! Bugün maç var mı parkte? Delikanlının biri antreman yaparken gördüm sabahtan."),
            ("Sen", "Evet Rıza Abi! Baran da geliyormuş..."),
            ("Rıza Abi", "Baran mı? O çocuk iyi topçu ama gururu var biraz. İyi şanslar!"),
            ("Anlatıcı", "Parkın yolunu tuttun."),
        };
        DialogueManager.Instance.ShowSequence(lines, _OnDialogueEnd);
    }

    private void _OnDialogueEnd()
    {
        SequenceManager.Instance.GoToNextScene();
    }
}
