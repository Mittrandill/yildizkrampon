using Godot;
using System.Collections.Generic;

/// res://scripts/BedroomScene.cs
/// Bedroom wake-up scene. Shows dialogue then advances to kitchen.
public partial class BedroomScene : Node2D
{
    public override void _Ready()
    {
        SequenceManager.Instance.FadeIn();
        GameManager.Instance.DayStep = 0;

        var lines = new List<(string, string)>
        {
            ("Anlatıcı", "Sabah güneşi yatağına vurdu. 14 yaşındasın ve rüyalarını yaşamak için bugün önemli bir gün."),
            ("Sen", "Bugün parkta oynayacağız! Hep antrenör Kemal Hoca'nın dikkatini çekmek istemistim."),
            ("Anlatıcı", "Yataktan fırladın. Odanda her yerde futbol posterleri ve eski topun var."),
        };
        DialogueManager.Instance.ShowSequence(lines, _OnDialogueEnd);
    }

    private void _OnDialogueEnd()
    {
        SequenceManager.Instance.GoToNextScene();
    }
}
