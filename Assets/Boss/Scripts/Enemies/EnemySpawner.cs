using System.Collections.Generic;
using UnityEngine;

/// Builds the spawn schedule for a loop from WaveData (GAME_DESIGN.md section 4) and creates monsters with
/// stats scaled for the current loop and difficulty. Convoy monsters spawn at lane entrances, taking the
/// open lanes in turn; swarm monsters spawn at a random point on an allowed map edge.
/// GameManager calls BuildSchedule and Tick.
public class EnemySpawner : MonoBehaviour
{
    public struct SpawnEvent
    {
        public float Time;
        public EnemyType Type;
        public EnemyGroup Group;
        public SpawnSide Side;
    }

    [Tooltip("One WaveData per loop, loop 1 first")]
    [SerializeField] private WaveData[] waves;
    [SerializeField] private MeleeEnemy meleePrefab;
    [SerializeField] private RangedEnemy rangedPrefab;

    public WaveData[] Waves => waves;
    public Queue<SpawnEvent> Schedule { get; } = new Queue<SpawnEvent>();

    /// Number of loops in the game (one per WaveData).
    public int LoopCount => waves.Length;

    /// WaveData for a loop (1-based, clamped).
    public WaveData WaveFor(int loop) => waves[Mathf.Clamp(loop, 1, waves.Length) - 1];

    /// True if convoys use this lane in any loop from 1 to <paramref name="loop"/> (pads unlock then).
    public bool LaneOpenedBy(SpawnSide side, int loop)
    {
        for (int l = 1; l <= Mathf.Min(loop, waves.Length); l++)
            if (waves[l - 1].HasConvoyOn(side)) return true;
        return false;
    }

    /// True if the lane is open in this loop and was never open before it.
    public bool LaneOpensFirstTime(SpawnSide side, int loop) =>
        WaveFor(loop).HasConvoyOn(side) && !LaneOpenedBy(side, loop - 1);

    /// Spawn i of n happens at spawnWindow × (i/n)^spawnCurve + spawnStartDelay seconds. Swarm monsters are
    /// spread evenly through the schedule, and ranged monsters evenly through both groups.
    public void BuildSchedule(int loop)
    {
        Schedule.Clear();

        GameConfig config = GameManager.Instance.Config;
        WaveData wave = WaveFor(loop);
        int total = wave.TotalCount;
        if (total <= 0) return;

        int convoy = wave.ConvoyCount();
        int swarm = total - convoy;
        int rangedConvoy = Mathf.RoundToInt((float)wave.RangedCount * convoy / total);
        int rangedSwarm = wave.RangedCount - rangedConvoy;
        SpawnSide[] swarmSides = wave.SwarmSides();
        int ci = 0, si = 0;

        for (int i = 0; i < total; i++)
        {
            var e = new SpawnEvent
            {
                Time = config.spawnWindow * Mathf.Pow((float)i / total, config.spawnCurve) + config.spawnStartDelay
            };

            if (EvenPick(i, swarm, total))
            {
                e.Group = EnemyGroup.Swarm;
                e.Type = EvenPick(si, rangedSwarm, swarm) ? EnemyType.Ranged : EnemyType.Melee;
                e.Side = swarmSides[Random.Range(0, swarmSides.Length)];
                si++;
            }
            else
            {
                e.Group = EnemyGroup.Convoy;
                e.Type = EvenPick(ci, rangedConvoy, convoy) ? EnemyType.Ranged : EnemyType.Melee;
                e.Side = wave.ConvoySides[ci % wave.ConvoySides.Length];
                ci++;
            }
            Schedule.Enqueue(e);
        }
        // Boss (wave.HasBoss) is added in build step 11.
    }

    /// True for k of every n indices, spread evenly.
    private static bool EvenPick(int i, int k, int n) => n > 0 && (i + 1) * k / n > i * k / n;

    /// Spawns everything scheduled up to <paramref name="elapsed"/> seconds into the loop.
    public void Tick(float elapsed)
    {
        while (Schedule.Count > 0 && Schedule.Peek().Time <= elapsed)
        {
            SpawnEvent e = Schedule.Dequeue();
            Spawn(e.Type, e.Group, e.Side);
        }
    }

    public Enemy Spawn(EnemyType type, EnemyGroup group, SpawnSide side)
    {
        GameConfig config = GameManager.Instance.Config;
        switch (type)
        {
            case EnemyType.Melee: return SpawnFrom(meleePrefab, config.melee, group, side);
            case EnemyType.Ranged: return SpawnFrom(rangedPrefab, config.ranged, group, side);
            default: throw new System.NotSupportedException($"{type} enemies are added in a later build step.");
        }
    }

    /// Removes every monster from the map (loop reset).
    public void ClearAll()
    {
        Schedule.Clear();
        foreach (Enemy e in new List<Enemy>(Enemy.Active))
            Destroy(e.gameObject);
    }

    private Enemy SpawnFrom(Enemy prefab, GameConfig.MonsterStats stats, EnemyGroup group, SpawnSide side)
    {
        GameManager gm = GameManager.Instance;
        GridMap grid = gm.Grid;

        Lane lane = null;
        Vector2 offset = Vector2.zero;
        Vector2 pos;
        if (group == EnemyGroup.Convoy)
        {
            lane = grid.LaneFor(side);
            float j = gm.Config.convoyLaneOffset + (lane.Width - 1) * 0.5f;   // wider lanes spread more
            offset = new Vector2(Random.Range(-j, j), Random.Range(-j, j));
            pos = lane.Entrance() + offset;
        }
        else
        {
            pos = RandomEdgePoint(side, stats.bodyRadius);
        }

        Enemy enemy = Instantiate(prefab, pos, Quaternion.identity, transform);
        enemy.Init(
            DifficultyScaling.EnemyHP(stats.hp, gm.CurrentLoop, gm.Difficulty, group),
            DifficultyScaling.EnemyDamage(stats.damage, gm.CurrentLoop, gm.Difficulty),
            DifficultyScaling.EnemyDamage(stats.baseDamage, gm.CurrentLoop, gm.Difficulty),
            stats.speed,
            stats.exp,
            stats.bodyRadius,
            group,
            lane,
            offset);
        return enemy;
    }

    /// A random point just inside the given map edge.
    private static Vector2 RandomEdgePoint(SpawnSide side, float inset)
    {
        GridMap grid = GameManager.Instance.Grid;
        float w = grid.Width, h = grid.Height;
        switch (side)
        {
            case SpawnSide.Left: return new Vector2(inset, Random.Range(inset, h - inset));
            case SpawnSide.Right: return new Vector2(w - inset, Random.Range(inset, h - inset));
            case SpawnSide.Top: return new Vector2(Random.Range(inset, w - inset), h - inset);
            default: return new Vector2(Random.Range(inset, w - inset), inset);
        }
    }
}
