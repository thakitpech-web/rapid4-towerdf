using TMPro;
using UnityEngine;

/// Loop result and game over screen. Wire the main button's OnClick to OnButtonClicked and an optional
/// Main Menu button's OnClick to OnMainMenuClicked in the Inspector.
public class ResultPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text buttonLabel;
    [Tooltip("Scene name in Build Settings, loaded by the Main Menu button")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    public void Show(string title, string detail, string button)
    {
        titleText.text = title;
        detailText.text = detail;
        buttonLabel.text = button;
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    public void OnButtonClicked() => GameManager.Instance.ContinueAfterResult();

    /// Main Menu button: leaves the run and goes back to the main menu.
    public void OnMainMenuClicked()
    {
        SoundManager.Play("Button");
        Time.timeScale = 1f;
        SceneFader.FadeTo(mainMenuScene);
    }
}
