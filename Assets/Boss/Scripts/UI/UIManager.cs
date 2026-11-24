using System.Collections.Generic;
using UnityEngine;

/// Shows and hides the panels. GameManager tells it what to show; it never changes phase.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private ResultPanel resultPanel;
    [SerializeField] private BuildPanel buildPanel;
    [SerializeField] private LevelUpPanel levelUpPanel;

    [Header("Survival overlay (shows and hides itself)")]
    [SerializeField] private SkillBarView skillBar;

    public SkillBarView SkillBar => skillBar;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void HidePanels()
    {
        resultPanel.Hide();
        buildPanel.Hide();
        levelUpPanel.Hide();
    }

    public void ShowLevelUp(List<Upgrade> choices) => levelUpPanel.Show(choices);

    public void HideLevelUp() => levelUpPanel.Hide();

    public void ShowResult(bool won, string reason, int levelOrbs, int bonusOrbs)
    {
        GameManager gm = GameManager.Instance;
        HidePanels();

        string orbs = $"+{levelOrbs} orb from levels, +{bonusOrbs} bonus (total {gm.Orbs})";
        if (won)
        {
            resultPanel.Show(
                $"Loop {gm.CurrentLoop - 1} cleared",
                $"{reason}. {orbs}. Build before loop {gm.CurrentLoop}.",
                "Build towers");
        }
        else
        {
            resultPanel.Show(
                $"Loop {gm.CurrentLoop} lost",
                $"{reason}. Losses {gm.Losses}/{gm.MaxLosses}. {orbs}. The same loop will be replayed.",
                "Build towers");
        }
    }

    public void ShowGameOver(bool victory, string reason)
    {
        GameManager gm = GameManager.Instance;
        HidePanels();

        if (victory)
            resultPanel.Show($"All {gm.TotalLoops} loops cleared!", $"{reason}. Losses {gm.Losses}/{gm.MaxLosses}.", "Play again");
        else
            resultPanel.Show($"{gm.MaxLosses} losses", $"{reason}. The game ended on loop {gm.CurrentLoop}.", "Play again");
    }

    public void ShowBuild()
    {
        HidePanels();
        buildPanel.Show();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
