using Godot;
using System.Collections.Generic;

/// res://scripts/ShopScene.cs
/// Riza's shop — buy protein bar for training XP bonus.
public partial class ShopScene : Node2D
{
    public override void _Ready()
    {
        SequenceManager.Instance.FadeIn();
        GameManager.Instance.DayStep = 1;

        var lines = new List<(string, string)>
        {
            ("Rıza Abi", "Aaaa! Galiba iyi oynadın bugün. Bir şeyler almak ister misin?"),
        };
        DialogueManager.Instance.ShowSequence(lines, _ShowShopChoice);
    }

    private void _ShowShopChoice()
    {
        DialogueManager.Instance.ShowChoice(
            "Rıza Abi",
            "Ne alacaksın?",
            new List<string> { "Protein Bar (Antrenman XP +2x)", "Geçiyorum" },
            _OnChoice
        );
    }

    private void _OnChoice(int index)
    {
        if (index == 0)
        {
            GameManager.Instance.BoughtProteinBar = true;
            GameManager.Instance.AddEvent("Protein Bar aldın (Antrenman bonusu aktif)");
            DialogueManager.Instance.Show("Rıza Abi", "Antrenman öncesi yedin mi olur! Başarılar!", () => SequenceManager.Instance.GoToNextScene());
        }
        else
        {
            DialogueManager.Instance.Show("Rıza Abi", "Tamam, güle güle! Yarın akademi maçı var değil mi?", () => SequenceManager.Instance.GoToNextScene());
        }
    }
}
