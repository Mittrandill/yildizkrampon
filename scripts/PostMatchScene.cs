using Godot;
using System.Collections.Generic;

/// res://scripts/PostMatchScene.cs
/// Coach Kemal Hoca notices the player after the match.
public partial class PostMatchScene : Node2D
{
    public override void _Ready()
    {
        SequenceManager.Instance.FadeIn();
        GameManager.Instance.DayStep = 1;

        var lines = new List<(string, string)>
        {
            ("Kemal Hoca", "Dur bir dakika genç. Seninle konuşmak istiyorum."),
            ("Sen", "Ben mi hocam?"),
            ("Kemal Hoca", "Evet sen. Güzel iki ayak var sende. Teknik çalışmak lazım ama potansiyel var."),
            ("Kemal Hoca", "Yarın sabah akademi maçımız var. Saat 9'da sahada olabilir misin?"),
            ("Sen", "T-tabii hocam! Mutlaka gelirim!"),
            ("Anlatıcı", "Kemal Hoca seni fark etti. Bu şans kaçırılmamalı."),
        };

        GameManager.Instance.WatchedByCoach = true;
        GameManager.Instance.Morale = Mathf.Min(100, GameManager.Instance.Morale + 20);
        GameManager.Instance.AddEvent("Kemal Hoca seni keşfetti! (+20 Moral)");

        DialogueManager.Instance.ShowSequence(lines, _OnDialogueEnd);
    }

    private void _OnDialogueEnd()
    {
        SequenceManager.Instance.GoToNextScene();
    }
}
