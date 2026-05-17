using Godot;

/// res://scripts/NextMorningScene.cs
/// Teaser for Academy tryout — end of day 1.
public partial class NextMorningScene : Node2D
{
    public override void _Ready()
    {
        SequenceManager.Instance.FadeIn();
        GameManager.Instance.DayStep = 0;

        var gm = GameManager.Instance;
        string coachNote = gm.WatchedByCoach
            ? "Kemal Hoca seni bekliyordu. Bu şans hayatını değiştirebilir."
            : "Yarın akademide bir şans yakalamak için erkenden kalkman gerekiyor.";

        DialogueManager.Instance.ShowSequence(
            new System.Collections.Generic.List<(string, string)>
            {
                ("Anlatıcı", "Ertesi sabah... Güneş yeni doğmuştu."),
                ("Anlatıcı", coachNote),
                ("Anlatıcı", $"Genel Yetenek: {gm.Overall} — Akademi sınavı seni bekliyor."),
                ("Anlatıcı", "[YILDIZ KRAMPON - Devam eden yapım]"),
            },
            null
        );
    }
}
