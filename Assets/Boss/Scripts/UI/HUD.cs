using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Timer, loop, lives and HP bars. Reads the game state every frame (display only).
public class HUD : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text loopText;
    [SerializeField] private TMP_Text livesText;

    [Header("Level and exp")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image expFill;

    [Header("HP bars (Image Type = Filled)")]
    [SerializeField] private Image baseHpFill;
    [SerializeField] private TMP_Text baseHpText;
    [SerializeField] private Image playerHpFill;
    [SerializeField] private TMP_Text playerHpText;

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        int secondsLeft = Mathf.CeilToInt(gm.Timer.TimeLeft());
        timerText.text = $"{secondsLeft / 60}:{secondsLeft % 60:00}";
        loopText.text = $"Loop {gm.CurrentLoop}/{gm.TotalLoops}";
        livesText.text = $"Lives {gm.MaxLosses - gm.Losses}";

        LevelSystem levels = gm.Levels;
        levelText.text = $"Lv {levels.Level}  ·  +{levels.LevelsThisLoop} orb this loop";
        expFill.fillAmount = (float)levels.Exp / levels.ExpToNext();

        ShowHP(gm.Base, baseHpFill, baseHpText);
        ShowHP(gm.Player, playerHpFill, playerHpText);
    }

    private static void ShowHP(Unit unit, Image fill, TMP_Text label)
    {
        fill.fillAmount = unit.MaxHP > 0f ? unit.HP / unit.MaxHP : 0f;
        label.text = $"{Mathf.CeilToInt(unit.HP)}/{Mathf.CeilToInt(unit.MaxHP)}";
    }
}
