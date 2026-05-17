using Godot;
using System.Collections.Generic;

/// res://scripts/KitchenScene.cs
/// Breakfast choice — affects Energy stat for the day.
public partial class KitchenScene : Node2D
{
    public override void _Ready()
    {
        SequenceManager.Instance.FadeIn();
        GameManager.Instance.DayStep = 0;

        var lines = new List<(string, string)>
        {
            ("Anne", "Hayırlı sabahlar oğlum! Hızlı ye, bugün maçın var değil mi?"),
            ("Baba", "Futbolcu olmak güzel ama önce okul. Tamam mı? Şimdi ne yiyeceksin?"),
        };
        DialogueManager.Instance.ShowSequence(lines, _ShowBreakfastChoice);
    }

    private void _ShowBreakfastChoice()
    {
        DialogueManager.Instance.ShowChoice(
            "Anne",
            "Ne yemek istersin?",
            new List<string> { "Ekmek + Peynir (+15 Enerji)", "Protein Bar (+20 Enerji, +3 Teknik)" },
            _OnChoice
        );
    }

    private void _OnChoice(int index)
    {
        if (index == 0)
        {
            GameManager.Instance.Energy += 15;
            GameManager.Instance.AddEvent("Kahvaltı: Ekmek (+15 Enerji)");
            DialogueManager.Instance.Show("Anne", "Afiyetler! Koş bakalım.", () => SequenceManager.Instance.GoToNextScene());
        }
        else
        {
            GameManager.Instance.Energy += 20;
            GameManager.Instance.Technique += 3;
            GameManager.Instance.AddEvent("Kahvaltı: Protein Bar (+20 Enerji, +3 Teknik)");
            DialogueManager.Instance.Show("Sen", "Spor kafeden aldığım bar... Günü iyi geçecek.", () => SequenceManager.Instance.GoToNextScene());
        }
        GameManager.Instance.ClampStats();
    }
}
