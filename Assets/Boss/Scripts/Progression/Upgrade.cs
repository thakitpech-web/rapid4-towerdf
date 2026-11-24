using UnityEngine;

/// A level-up card. Upgrades are ScriptableObject assets so their numbers live in data.
/// They hold no runtime state: stacks are kept in UpgradeManager.
public abstract class Upgrade : ScriptableObject
{
    public string Name;
    [TextArea] public string Description;
    [Tooltip("Picture in the coloured area of the card. Empty hides the icon.")]
    public Sprite Icon;
    [Tooltip("Card background (the coloured frame). Empty keeps the frame set on the card in the scene.")]
    public Sprite CardFrame;

    /// Small label on the card, e.g. "Rank 2/5".
    public virtual string Tag => "";

    public abstract bool IsAvailable();
    public abstract void Apply();
}
