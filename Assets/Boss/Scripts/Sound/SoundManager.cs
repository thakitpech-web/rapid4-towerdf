using UnityEngine;
 
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
 
    [SerializeField]
    private SoundLibrary sfxLibrary;
    [SerializeField]
    private AudioSource sfx2DSource;
 
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
 
    public void PlaySound3D(AudioClip clip, Vector3 pos)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, pos);
        }
    }
 
    public void PlaySound3D(string soundName, Vector3 pos)
    {
        PlaySound3D(sfxLibrary.GetClipFromName(soundName), pos);
    }
 
    // Safe to call from any scene: does nothing when the SoundManager (lives in MainMenu) isn't loaded,
    // e.g. when pressing Play straight from the Gameplay scene.
    public static void Play(string soundName, float volume = 1f)
    {
        if (Instance != null && !string.IsNullOrEmpty(soundName)) Instance.PlaySound2D(soundName, volume);
    }

    // Real time each sound last played, for its Min Interval.
    private readonly System.Collections.Generic.Dictionary<string, float> lastPlayed =
        new System.Collections.Generic.Dictionary<string, float>();

    public void PlaySound2D(string soundName, float volume = 1f)
    {
        SoundEffect s = sfxLibrary.Find(soundName);
        if (s == null) return;

        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(soundName, out float last) && now - last < s.minInterval) return;

        AudioClip clip = sfxLibrary.GetClipFromName(soundName);
        if (clip == null) return;

        lastPlayed[soundName] = now;
        sfx2DSource.PlayOneShot(clip, volume * s.volume);
    }

    // Looping sound that lives on its owner: it stops by itself when the owner is destroyed.
    // Goes through the same mixer group as the other SFX, so the SFX slider still controls it.
    public AudioSource CreateLoop(string soundName, GameObject owner, float volume = 1f)
    {
        AudioSource source = owner.AddComponent<AudioSource>();
        source.clip = sfxLibrary.GetClipFromName(soundName);
        source.outputAudioMixerGroup = sfx2DSource.outputAudioMixerGroup;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = volume;
        return source;
    }}