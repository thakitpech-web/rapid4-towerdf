using UnityEngine;

/// A stacking passive (GAME_DESIGN.md section 4). Leaves the pool at MaxStacks.
[CreateAssetMenu(fileName = "Passive_", menuName = "One Minute Siege/Passive Upgrade")]
public class PassiveUpgrade : Upgrade
{
    public UpgradeType Type;
    [Tooltip("0.1 = +10% per pick")] public float ValuePerStack = 0.1f;
    public int MaxStacks = 10;

    private static UpgradeManager Manager => GameManager.Instance.Upgrades;

    public override string Tag => $"Rank {Manager.GetStacks(Type)}/{MaxStacks}";

    public override bool IsAvailable() => Manager.GetStacks(Type) < MaxStacks;

    public override void Apply()
    {
        Manager.AddStack(Type);

        // Raising base max HP mid-loop also adds the difference to current HP.
        if (Type == UpgradeType.BaseMaxHP)
        {
            GameManager gm = GameManager.Instance;
            gm.Base.RaiseMaxHP(gm.BaseMaxHP);
        }
    }
}
