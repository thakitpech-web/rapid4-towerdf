using UnityEngine;
using UnityEngine.InputSystem;

/// Pause menu for the GamePlay scene: Resume, Setting (volume) and Back to main menu.
/// Put this on an object that stays active (e.g. the Canvas); the two panels are its children and start hidden.
/// Opens with Escape or a pause button (OnClick -> Pause). While paused the game is frozen (timeScale 0)
/// and gameplay keys are ignored.
/// Escape: closes Setting, else resumes; when not paused it opens the menu, except in the build phase with a
/// tower picked, where Escape first clears the pick (BuildManager does that).
[DefaultExecutionOrder(-50)]   // before BuildManager, so it sees the pick before Escape clears it
public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Tooltip("Resume / Setting / Main menu buttons")]
    [SerializeField] private GameObject pausePanel;
    [Tooltip("Volume sliders (with a VolumeSettings component) and a Back button")]
    [SerializeField] private GameObject settingPanel;
    [Tooltip("Scene name in Build Settings")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private void Awake()
    {
        IsPaused = false;
        ShowPanels(false, false);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

        if (IsPaused)
        {
            if (settingPanel != null && settingPanel.activeSelf) CloseSetting();
            else Resume();
            return;
        }

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.Phase == GamePhase.Build &&
            (gm.Builder.SelectedShopItem.HasValue || gm.Builder.SelectedTower != null))
            return;   // BuildManager clears the pick this frame

        Pause();
    }

    /// Pause button, or Escape.
    public void Pause()
    {
        if (IsPaused || !CanPause()) return;
        SoundManager.Play("Button");
        IsPaused = true;
        Time.timeScale = 0f;
        ShowPanels(true, false);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        SoundManager.Play("Button");
        IsPaused = false;
        Time.timeScale = 1f;
        ShowPanels(false, false);
    }

    public void OpenSetting()
    {
        SoundManager.Play("Button");
        ShowPanels(false, true);
    }

    public void CloseSetting()
    {
        SoundManager.Play("Button");
        ShowPanels(true, false);
    }

    public void BackToMainMenu()
    {
        SoundManager.Play("Button");
        IsPaused = false;
        Time.timeScale = 1f;
        SceneFader.FadeTo(mainMenuScene);
    }

    // Not on the result or game over screens: they already wait for a button.
    private static bool CanPause()
    {
        GameManager gm = GameManager.Instance;
        return gm != null && (gm.Phase == GamePhase.Build || gm.Phase == GamePhase.Survival || gm.Phase == GamePhase.LevelUp);
    }

    private void ShowPanels(bool pause, bool setting)
    {
        if (pausePanel != null) pausePanel.SetActive(pause);
        if (settingPanel != null) settingPanel.SetActive(setting);
    }

    private void OnDestroy()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
