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

    [Header("Base shield (Heal tier-3 overheal)")]
    [Tooltip("Image Type = Filled; empty while the base has no shield")]
    [SerializeField] private Image baseShieldFill;
    [Tooltip("Shield (as a share of base max HP) that fills the bar; 0.3 = the Heal tower's cap")]
    [SerializeField] private float shieldFullAt = 0.3f;

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // Every field is optional: leave one empty (or delete its object) to drop it from the HUD.
        int secondsLeft = Mathf.CeilToInt(gm.Timer.TimeLeft());
        if (timerText != null) timerText.text = $"{secondsLeft / 60}:{secondsLeft % 60:00}";
        if (loopText != null) loopText.text = $"Loop {gm.CurrentLoop}/{gm.TotalLoops}";
        if (livesText != null) livesText.text = $"Lives {gm.MaxLosses - gm.Losses}";

        LevelSystem levels = gm.Levels;
        if (levelText != null) levelText.text = $"Lv {levels.Level}  ·  +{levels.LevelsThisLoop} orb this loop";
        if (expFill != null) expFill.fillAmount = (float)levels.Exp / levels.ExpToNext();

        ShowHP(gm.Base, baseHpFill, baseHpText);
        ShowHP(gm.Player, playerHpFill, playerHpText);

        if (baseShieldFill != null)
        {
            Base b = gm.Base;
            baseShieldFill.fillAmount = b.MaxHP > 0f && shieldFullAt > 0f ? b.Shield / (b.MaxHP * shieldFullAt) : 0f;
        }
    }

    private static void ShowHP(Unit unit, Image fill, TMP_Text label)
    {
        if (fill != null) fill.fillAmount = unit.MaxHP > 0f ? unit.HP / unit.MaxHP : 0f;
        if (label == null) return;
        string shield = unit.Shield >= 1f ? $" +{Mathf.FloorToInt(unit.Shield)}" : "";
        label.text = $"{Mathf.CeilToInt(unit.HP)}/{Mathf.CeilToInt(unit.MaxHP)}{shield}";
    }
}
