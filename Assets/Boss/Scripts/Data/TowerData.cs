using UnityEngine;

public enum TowerType
{
    Bolt,
    Heal,
    AoE
}

/// Stats of one tower at one tier.
public struct TowerStats
{
    public float Damage;
    public float Rate;
    public float Range;
    public float Radius;
    public float MaxHP;
    public int Targets;
}

/// One tower type (GAME_DESIGN.md section 10). Arrays hold tier 1, 2, 3.
/// Total cost doubles per tier because a tier is made by merging two of the tier below.
[CreateAssetMenu(fileName = "Tower_Bolt", menuName = "One Minute Siege/Tower Data")]
public class TowerData : ScriptableObject
{
    public TowerType Type;
    public string DisplayName = "Bolt";
    [Tooltip("Prefab with the BoltTower / HealTower / AoETower component that matches Type, plus its art")]
    public Tower Prefab;
    [Tooltip("Colour of this tower on the minimap")] public Color Color = Color.white;

    [Tooltip("Orbs for tier 1")] public int BaseCost = 1;

    [Tooltip("Bolt/AoE: damage per hit. Heal: heal per second.")]
    public float[] Damage = { 10f, 20f, 40f };
    [Tooltip("Bolt/AoE: attacks per second. Unused by Heal.")]
    public float[] Rate = { 1f, 1.25f, 1.5f };
    public float[] Range = { 3.5f, 4f, 4.5f };
    [Tooltip("AoE only: blast radius")]
    public float[] Radius = { 0f, 0f, 0f };
    public float[] MaxHP = { 250f, 500f, 1000f };
    [Tooltip("Heal only: targets healed at once")]
    public int[] Targets = { 1, 1, 1 };

    [Tooltip("Bolt only")] public Projectile ProjectilePrefab;
    [Tooltip("Bolt only. Not in the doc.")] public float ProjectileSpeed = 12f;

    public int MaxTier => MaxHP.Length;

    /// Total orbs spent on a tower of this tier (1 / 2 / 4 × BaseCost).
    public int TotalCost(int tier) => BaseCost << (tier - 1);

    public TowerStats GetStats(int tier)
    {
        int i = Mathf.Clamp(tier, 1, MaxTier) - 1;
        return new TowerStats
        {
            Damage = Damage[i],
            Rate = Rate[i],
            Range = Range[i],
            Radius = Radius[i],
            MaxHP = MaxHP[i],
            Targets = Targets[i]
        };
    }
}
