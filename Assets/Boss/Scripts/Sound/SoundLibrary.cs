using UnityEngine;

[System.Serializable]
public class SoundEffect
{
    public string groupID;
    [Tooltip("One is picked at random each time. Empty = the sound is silent (no error).")]
    public AudioClip[] clips;
    [Tooltip("Loudness of this sound (multiplied by the volume passed when it is played)")]
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Seconds before this sound can play again; plays asked for sooner are skipped (stops 30 hits at once sounding like a roar)")]
    public float minInterval = 0.05f;
}

public class SoundLibrary : MonoBehaviour
{
    public SoundEffect[] soundEffects;

    /// The entry with this groupID, or null.
    public SoundEffect Find(string name)
    {
        foreach (var soundEffect in soundEffects)
            if (soundEffect != null && soundEffect.groupID == name) return soundEffect;
        return null;
    }

    /// A random clip of this group, or null when the group is missing or has no clips yet.
    public AudioClip GetClipFromName(string name)
    {
        SoundEffect s = Find(name);
        if (s == null || s.clips == null || s.clips.Length == 0) return null;
        return s.clips[Random.Range(0, s.clips.Length)];
    }
}
