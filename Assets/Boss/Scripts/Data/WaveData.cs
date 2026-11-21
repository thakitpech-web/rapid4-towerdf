using UnityEngine;
using UnityEngine.Serialization;

/// Monsters for one loop (GAME_DESIGN.md section 4). Make one asset per loop, 8 in total.
[CreateAssetMenu(fileName = "Wave_01", menuName = "One Minute Siege/Wave Data")]
public class WaveData : ScriptableObject
{
    public int MeleeCount;
    public int RangedCount;

    [Tooltip("Lanes convoys use this loop, taken in turn. Empty = every monster is swarm.")]
    [FormerlySerializedAs("Sides")]
    public SpawnSide[] ConvoySides = { SpawnSide.Left };

    [Tooltip("Share of the monsters that are swarm (0.25 = 25%)")]
    [Range(0f, 1f)] public float SwarmPercent = 0.25f;

    public SwarmSpawn SwarmFrom = SwarmSpawn.Edge;

    public bool HasBoss;
    public float BossSpawnTime = 10f;

    public int TotalCount => MeleeCount + RangedCount;

    /// round(total × (1 − swarm %)), or 0 when no lane is open.
    public int ConvoyCount() =>
        ConvoySides == null || ConvoySides.Length == 0 ? 0 : Mathf.RoundToInt(TotalCount * (1f - SwarmPercent));

    public int SwarmCount() => TotalCount - ConvoyCount();

    public bool HasConvoyOn(SpawnSide side) => ConvoySides != null && System.Array.IndexOf(ConvoySides, side) >= 0;

    /// Sides swarm monsters may spawn from this loop.
    public SpawnSide[] SwarmSides()
    {
        var all = (SpawnSide[])System.Enum.GetValues(typeof(SpawnSide));
        var result = new System.Collections.Generic.List<SpawnSide>();
        foreach (SpawnSide s in all)
        {
            bool convoy = HasConvoyOn(s);
            if (SwarmFrom == SwarmSpawn.Edge || (SwarmFrom == SwarmSpawn.FreeSides && !convoy) ||
                (SwarmFrom == SwarmSpawn.ConvoySides && convoy))
                result.Add(s);
        }
        return result.Count > 0 ? result.ToArray() : all;
    }
}
