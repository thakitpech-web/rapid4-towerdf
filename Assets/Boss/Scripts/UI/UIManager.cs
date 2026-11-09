using UnityEngine;

/// <summary>Shows and hides the panels. GameManager tells it what to show; it never changes phase.</summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private ResultPanel resultPanel;
    [SerializeField] private BuildPanel buildPanel;

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
    }

    public void ShowResult(bool won, string reason, int orbsGained)
    {
        GameManager gm = GameManager.Instance;
        HidePanels();

        if (won)
        {
            resultPanel.Show(
                $"Loop {gm.CurrentLoop - 1} cleared",
                $"{reason}. +{orbsGained} orb (total {gm.Orbs}). Build before loop {gm.CurrentLoop}.",
                "Build towers");
        }
        else
        {
            resultPanel.Show(
                $"Loop {gm.CurrentLoop} lost",
                $"{reason}. Losses {gm.Losses}/{gm.MaxLosses}. +{orbsGained} orb (total {gm.Orbs}). The same loop will be replayed.",
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
