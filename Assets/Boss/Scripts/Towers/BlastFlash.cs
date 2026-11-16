using UnityEngine;

/// A short-lived fading circle showing an AoE blast.
public class BlastFlash : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color color;
    private float duration, left;

    public static void Spawn(Vector2 at, float radius, Color color, float duration)
    {
        var go = new GameObject("Blast");
        go.transform.position = at;
        go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        var flash = go.AddComponent<BlastFlash>();
        flash.sr = go.AddComponent<SpriteRenderer>();
        flash.sr.sprite = PlaceholderArt.Circle;
        flash.sr.sortingOrder = PlaceholderArt.BlastOrder;
        flash.sr.color = color;
        flash.color = color;
        flash.duration = flash.left = duration;
    }

    private void Update()
    {
        left -= Time.deltaTime;
        if (left <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        Color c = color;
        c.a *= left / duration;
        sr.color = c;
    }
}
