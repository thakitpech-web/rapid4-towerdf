using System.Collections;
using UnityEngine;
 
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
 
    [SerializeField]
    private MusicLibrary musicLibrary;
    [SerializeField]
    private AudioSource musicSource;
 
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
 
    // The track playing or being faded in, so asking for it again does not restart it.
    private AudioClip targetTrack;

    /// Safe to call from any scene: does nothing when the MusicManager (lives in MainMenu) isn't loaded.
    public static void Play(string trackName)
    {
        if (Instance != null) Instance.PlayMusic(trackName);
    }

    public void PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        AudioClip next = musicLibrary.GetClipFromName(trackName);
        if (next == null || next == targetTrack) return;   // missing track, or already playing it

        targetTrack = next;
        StopAllCoroutines();                                // a newer request replaces a fade in progress
        StartCoroutine(AnimateMusicCrossfade(next, fadeDuration));
    }

    // Unscaled time so the fade also works while the game is paused (timeScale 0).
    IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float fadeDuration = 0.5f)
    {
        float from = musicSource.isPlaying ? musicSource.volume : 0f;
        float percent = 0;
        while (from > 0f && percent < 1)
        {
            percent += Time.unscaledDeltaTime / fadeDuration;
            musicSource.volume = Mathf.Lerp(from, 0, percent);
            yield return null;
        }

        musicSource.clip = nextTrack;
        musicSource.loop = true;
        musicSource.Play();

        percent = 0;
        while (percent < 1)
        {
            percent += Time.unscaledDeltaTime / fadeDuration;
            musicSource.volume = Mathf.Lerp(0, 1f, percent);
            yield return null;
        }
    }
}