using UnityEngine;
using UnityEngine.SceneManagement;

public class UiManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    [Header("UI Panels")]
    public GameObject settingsPanel;

    public void Play()
    {
        SceneManager.LoadSceneAsync("Main");
    }

    public void Setting()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void Exits()
    {
        Application.Quit();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
