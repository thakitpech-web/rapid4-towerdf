using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using System;

// Put this on the Canvas in the Main scene.
// Hook the buttons' OnClick to PlayGame() and QuitGame().
public class MainMenu : MonoBehaviour
{
    [Header("Sound")]
    public AudioMixer audioMixer;
    [Header("Panel")]
    public GameObject settingPanel;
    [Header("Sound Slider")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [SerializeField] private string gameplayScene = "GamePlay";

    private void Start()
    {
        settingPanel.SetActive(false);
        Time.timeScale = 1f; // in case we came back from a paused Game Over
        MusicManager.Play("Main");

        if (PlayerPrefs.HasKey("musicVolume"))
        {
            LoadVolume();
        }
        else
        {
            UpdateMusicVolume();
            UpdateSoundVolume();
        }
    }

    public void PlayGame()
    {
        SoundManager.Play("Button");
        SceneFader.FadeTo(gameplayScene);   // the GamePlay scene opens on a build phase, which starts its music
    }

    public void OpenSetting()
    {
        SoundManager.Play("Button");
        settingPanel.SetActive(true);
    }
    public void Close()
    {
        SoundManager.Play("Button");
        settingPanel.SetActive(false);
    }

    public void BackToMainMenu()
    {
        SoundManager.Play("Button");
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        SoundManager.Play("Button");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void UpdateMusicVolume()
    {
        float volume = musicSlider.value;
        audioMixer.SetFloat("music",MathF.Log10(Mathf.Max(volume, 0.0001f))*20);
        PlayerPrefs.SetFloat("musicVolume",volume);
    }

    public void UpdateSoundVolume()
    {
        float volume = sfxSlider.value;
        audioMixer.SetFloat("SFX",MathF.Log10(Mathf.Max(volume, 0.0001f))*20);
        PlayerPrefs.SetFloat("SFXVolume",volume);
    }


    public void LoadVolume()
    {
        musicSlider.value = PlayerPrefs.GetFloat("musicVolume");
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume");
        UpdateMusicVolume();
        UpdateSoundVolume();
    }
}
    
    