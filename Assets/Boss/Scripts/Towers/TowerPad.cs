using UnityEngine;

/// <summary>
/// A spot where a tower can be built. Locked pads are dark and show a lock icon.
/// Occupant (the Tower on this pad) is added in build step 5.
/// </summary>
public class TowerPad : MonoBehaviour
{
    [Tooltip("First loop in which this pad is open (see GAME_DESIGN.md section 9).")]
    [SerializeField, Min(1)] private int unlockLoop = 1;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Color unlockedColor = new Color(0.95f, 0.89f, 0.76f, 0.35f);
    [SerializeField] private Color lockedColor = new Color(0f, 0f, 0f, 0.45f);

    public int Index { get; private set; }
    public int UnlockLoop => unlockLoop;
    public Vector2 Position => transform.position;
    public bool IsUnlocked { get; private set; }

    /// <summary>Called by BuildManager so the pad knows its place in the unlock order.</summary>
    public void SetIndex(int index) => Index = index;

    public void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
        if (body != null) body.color = unlocked ? unlockedColor : lockedColor;
        if (lockIcon != null) lockIcon.SetActive(!unlocked);
    }
}
