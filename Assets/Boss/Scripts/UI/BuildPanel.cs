using TMPro;
using UnityEngine;

/// <summary>
/// Build phase panel. Step 3: only a title and the Start button (wire OnClick to OnStartClicked).
/// The tower shop is added in step 5.
/// </summary>
public class BuildPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;

    public void Show()
    {
        GameManager gm = GameManager.Instance;
        titleText.text = $"Build phase - next: loop {gm.CurrentLoop}/{gm.TotalLoops}";
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    public void OnStartClicked() => GameManager.Instance.StartLoop();
}
