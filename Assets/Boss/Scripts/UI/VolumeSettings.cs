using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// Music and SFX volume sliders. Uses the same mixer parameters and saved values as the main menu, so a
/// change made in either place shows in the other. The sliders are hooked up in code (no OnValueChanged
/// wiring needed); give them Min Value 0.0001 and Max Value 1.
public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    // Same keys and mixer parameter names as MainMenu.
    private const string MusicKey = "musicVolume", SfxKey = "SFXVolume";
    private const string MusicParam = "music", SfxParam = "SFX";

    private void Awake()
    {
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(v => Apply(MusicParam, MusicKey, v));
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(v => Apply(SfxParam, SfxKey, v));
    }

    // Each time the panel opens, show the saved values.
    private void OnEnable()
    {
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicKey, 1f));
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SfxKey, 1f));
    }

    private void Apply(string param, string key, float value)
    {
        if (audioMixer != null) audioMixer.SetFloat(param, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat(key, value);
    }
}
