using UnityEngine;
using UnityEngine.UI;

/// Fades the button's children (text, icons) while the button cannot be pressed. Unity's Button only
/// tints its own background (Target Graphic), so the label would stay fully visible otherwise.
/// Setup: add this to the button object; nothing to assign.
[RequireComponent(typeof(Button))]
public class ButtonContentFade : MonoBehaviour
{
    [Tooltip("Alpha multiplier for the children while the button is disabled")]
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.5f;

    private Button button;
    private Graphic[] parts;
    private float[] alphas;
    private bool faded;

    private void Awake()
    {
        button = GetComponent<Button>();
        Graphic[] all = GetComponentsInChildren<Graphic>(true);
        var list = new System.Collections.Generic.List<Graphic>();
        foreach (Graphic g in all)
            if (g != button.targetGraphic) list.Add(g);   // the background is tinted by the Button itself
        parts = list.ToArray();
        alphas = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++) alphas[i] = parts[i].color.a;
    }

    private void LateUpdate()
    {
        bool fade = !button.IsInteractable();
        if (fade == faded) return;
        faded = fade;

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == null) continue;
            Color c = parts[i].color;
            c.a = fade ? alphas[i] * disabledAlpha : alphas[i];
            parts[i].color = c;
        }
    }
}
