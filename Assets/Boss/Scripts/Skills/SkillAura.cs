using UnityEngine;

/// Placeholder look for an active skill while it runs: a soft disc with a ring that pops in and pulses.
/// Flash() brightens it for a moment (the base shield blocking a hit). To use real art instead, give the
/// skill card an Effect Prefab; that prefab can have its own Flash() method.
public class SkillAura : MonoBehaviour
{
    private const float PopTime = 0.15f, PulseHz = 2.5f, PulseAmount = 0.06f, FlashTime = 0.15f;

    private SpriteRenderer fill, ring;
    private Color color;
    private float age, flash;

    /// A new aura centred on <paramref name="parent"/>, <paramref name="diameter"/> world units across.
    public static GameObject Create(Transform parent, Color color, float diameter, int sortingOrder)
    {
        var go = new GameObject("SkillAura");
        go.transform.SetParent(parent, false);

        // Undo the parent's scale so the diameter is in world units.
        float parentScale = Mathf.Max(0.0001f, Mathf.Abs(parent.lossyScale.x));
        float size = diameter / parentScale;

        var aura = go.AddComponent<SkillAura>();
        aura.color = color;
        aura.fill = PlaceholderArt.AddChild(go.transform, "Fill", PlaceholderArt.Circle, color, size, sortingOrder);
        aura.ring = PlaceholderArt.AddChild(go.transform, "Ring", PlaceholderArt.Ring, color, size, sortingOrder);
        aura.Apply();
        return go;
    }

    public void Flash() => flash = FlashTime;

    private void Update()
    {
        age += Time.deltaTime;
        flash = Mathf.Max(0f, flash - Time.deltaTime);
        Apply();
    }

    private void Apply()
    {
        float pop = Mathf.Clamp01(age / PopTime);
        float popScale = Mathf.Lerp(0.6f, 1f, 1f - (1f - pop) * (1f - pop));   // ease out
        float pulse = 1f + PulseAmount * Mathf.Sin(age * PulseHz * 2f * Mathf.PI);
        transform.localScale = Vector3.one * (popScale * pulse);

        float f = flash / FlashTime;
        fill.color = WithAlpha(Mathf.Lerp(0.18f, 0.5f, f) * pop);
        ring.color = WithAlpha(Mathf.Lerp(0.8f, 1f, f) * pop);
    }

    private Color WithAlpha(float a) => new Color(color.r, color.g, color.b, a);
}
