using TMPro;
using UnityEngine;

/// <summary>Loop result and game over screen. Wire the button's OnClick to OnButtonClicked in the Inspector.</summary>
public class ResultPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text buttonLabel;

    public void Show(string title, string detail, string button)
    {
        titleText.text = title;
        detailText.text = detail;
        buttonLabel.text = button;
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    public void OnButtonClicked() => GameManager.Instance.ContinueAfterResult();
}
