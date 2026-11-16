using UnityEngine;

/// A level-up card. Upgrades are ScriptableObject assets so their numbers live in data.
/// They hold no runtime state: stacks are kept in UpgradeManager.
public abstract class Upgrade : ScriptableObject
{
    public string Name;
    [TextArea] public string Description;

    /// Small label on the card, e.g. "Rank 2/5".
    public virtual string Tag => "";

    public abstract bool IsAvailable();
    public abstract void Apply();
}
