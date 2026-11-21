using UnityEngine;

/// A short visual effect made as a prefab (blast circle, hit spark, particles, an animated sprite...).
/// Removes itself after Lifetime seconds, optionally fading its sprites and sizing itself to a radius.
/// Setup: put this on the prefab root; build the visuals as children or on the root.
public class TimedEffect : MonoBehaviour
{
    [Tooltip("Seconds before the effect is removed")]
    [SerializeField, Min(0.01f)] private float lifetime = 0.25f;
    [Tooltip("Fade every SpriteRenderer in the effect to transparent over the lifetime")]
    [SerializeField] private bool fadeOut = true;
    [Tooltip("Scale the effect so 1 unit of art covers the given radius × 2 (e.g. art 1 unit across = blast diameter). " +
             "Off: the prefab keeps its own size.")]
    [SerializeField] private bool scaleToRadius = true;

    private SpriteRenderer[] sprites;
    private Color[] colors;
    private float left;

    /// Creates the effect at a point. Does nothing when the prefab is not assigned.
    public static TimedEffect Spawn(TimedEffect prefab, Vector2 at, float radius)
    {
        if (prefab == null) return null;

        TimedEffect fx = Instantiate(prefab, at, Quaternion.identity);
        if (fx.scaleToRadius)
        {
            Vector3 s = prefab.transform.localScale;
            fx.transform.localScale = new Vector3(s.x * radius * 2f, s.y * radius * 2f, s.z);
        }
        return fx;
    }

    private void Awake()
    {
        left = lifetime;
        sprites = GetComponentsInChildren<SpriteRenderer>();
        colors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) colors[i] = sprites[i].color;
    }

    private void Update()
    {
        left -= Time.deltaTime;
        if (left <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        if (!fadeOut) return;

        float k = left / lifetime;
        for (int i = 0; i < sprites.Length; i++)
        {
            Color c = colors[i];
            c.a *= k;
            sprites[i].color = c;
        }
    }
}
