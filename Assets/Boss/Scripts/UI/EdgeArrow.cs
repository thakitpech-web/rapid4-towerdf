using UnityEngine;
using UnityEngine.UI;

/// An arrow at the screen edge pointing to an off-screen unit (the base, or the boss in loop 8),
/// with an optional HP bar. The base arrow turns red while the base is being hit.
/// Setup: put this on an empty UI object directly under the Canvas, anchored at the centre.
/// Give it a child arrow Image and, optionally, an HP bar (a child object with a Filled Image).
/// The script moves its own RectTransform to the screen edge and hides the content when the unit is on screen.
public class EdgeArrow : MonoBehaviour
{
    public enum TargetKind
    {
        Base,
        Boss
    }

    [SerializeField] private TargetKind target = TargetKind.Base;

    [Header("Parts")]
    [Tooltip("Shown/hidden as a whole (arrow + HP bar). Must be a child, not this object.")]
    [SerializeField] private GameObject content;
    [SerializeField] private Image arrow;
    [Tooltip("Direction the arrow sprite points when not rotated, in degrees (0 = right, -90 = down)")]
    [SerializeField] private float spriteAngle;
    [Tooltip("Optional HP bar; kept on the inward side of the arrow")]
    [SerializeField] private RectTransform hpBar;
    [Tooltip("Image Type = Filled, Horizontal")]
    [SerializeField] private Image hpFill;

    [Header("Look")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float hitFlashSeconds = 0.25f;
    [Tooltip("Distance from the screen edge (canvas units)")]
    [SerializeField] private float edgeMargin = 40f;
    [Tooltip("Distance of the HP bar from the arrow, toward the screen centre")]
    [SerializeField] private float hpOffset = 38f;

    private RectTransform rt;

    private void Awake() => rt = (RectTransform)transform;

    private Unit TargetUnit()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return null;
        return target == TargetKind.Base ? gm.Base : null;   // the boss is added in build step 11
    }

    private void LateUpdate()
    {
        GameManager gm = GameManager.Instance;
        Unit unit = TargetUnit();
        Camera cam = Camera.main;

        bool playing = gm != null && (gm.Phase == GamePhase.Survival || gm.Phase == GamePhase.LevelUp);
        Vector3 vp = unit != null && cam != null ? cam.WorldToViewportPoint(unit.Position) : Vector3.zero;
        bool onScreen = vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

        bool show = playing && unit != null && unit.IsAlive() && !onScreen;
        if (content != null) content.SetActive(show);
        if (!show) return;

        // Push the direction from the screen centre out to the edge, minus a margin.
        Vector2 canvasSize = ((RectTransform)rt.parent).rect.size;
        Vector2 dir = new Vector2((vp.x - 0.5f) * canvasSize.x, (vp.y - 0.5f) * canvasSize.y);
        Vector2 half = canvasSize * 0.5f - new Vector2(edgeMargin, edgeMargin);
        float scale = Mathf.Min(half.x / Mathf.Max(Mathf.Abs(dir.x), 1e-3f), half.y / Mathf.Max(Mathf.Abs(dir.y), 1e-3f));
        rt.anchoredPosition = dir * scale;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (arrow != null)
        {
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle - spriteAngle);
            bool hit = unit is Base b && Time.time - b.LastHitTime < hitFlashSeconds;
            arrow.color = hit ? hitColor : normalColor;
        }

        if (hpBar != null) hpBar.anchoredPosition = -dir.normalized * hpOffset;
        if (hpFill != null) hpFill.fillAmount = unit.MaxHP > 0f ? unit.HP / unit.MaxHP : 0f;
    }
}
