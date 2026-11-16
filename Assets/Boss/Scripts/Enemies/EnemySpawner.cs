using System.Collections.Generic;
using UnityEngine;

/// Builds the spawn schedule for a loop from WaveData and creates monsters on a random cell along a
/// random open side (WaveData.Sides), with stats scaled for the current loop and difficulty.
/// GameManager calls BuildSchedule and Tick.
public class EnemySpawner : MonoBehaviour
{
    public struct SpawnEvent
    {
        public float Time;
        public EnemyType Type;
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

    /// Open spawn sides for a loop (1-based).
    public SpawnSide[] SidesFor(int loop) => waves[Mathf.Clamp(loop, 1, waves.Length) - 1].Sides;

    /// Spawn i of n happens at spawnWindow × (i/n)^spawnCurve + spawnStartDelay seconds, with ranged
    /// monsters mixed in evenly and a random open side for each.
    public void BuildSchedule(int loop)
    {
        Schedule.Clear();

        GameConfig config = GameManager.Instance.Config;
        WaveData wave = waves[loop - 1];
        int ranged = wave.RangedCount;
        int total = wave.MeleeCount + ranged;

        for (int i = 0; i < total; i++)
        {
            bool isRanged = (i + 1) * ranged / total > i * ranged / total;
            float t = config.spawnWindow * Mathf.Pow((float)i / total, config.spawnCurve) + config.spawnStartDelay;
            Schedule.Enqueue(new SpawnEvent
            {
                Time = t,
                Type = isRanged ? EnemyType.Ranged : EnemyType.Melee,
                Side = wave.Sides[Random.Range(0, wave.Sides.Length)]
            });
        }
        // Boss (wave.HasBoss) is added in build step 11.
    }

    /// Spawns everything scheduled up to <paramref name="elapsed"/> seconds into the loop.
    public void Tick(float elapsed)
    {
        while (Schedule.Count > 0 && Schedule.Peek().Time <= elapsed)
        {
            SpawnEvent e = Schedule.Dequeue();
            Spawn(e.Type, e.Side);
        }
    }

    public Enemy Spawn(EnemyType type, SpawnSide side)
    {
        GameManager gm = GameManager.Instance;
        GameConfig config = gm.Config;

        switch (type)
        {
            case EnemyType.Melee:
                return SpawnFrom(meleePrefab, config.melee, side, gm.CurrentLoop, gm.Difficulty);
            case EnemyType.Ranged:
                return SpawnFrom(rangedPrefab, config.ranged, side, gm.CurrentLoop, gm.Difficulty);
            default:
                throw new System.NotSupportedException($"{type} enemies are added in a later build step.");
        }
    }

    /// Removes every monster from the map (loop reset).
    public void ClearAll()
    {
        Schedule.Clear();
        foreach (Enemy e in new List<Enemy>(Enemy.Active))
            Destroy(e.gameObject);
    }

    private Enemy SpawnFrom(Enemy prefab, GameConfig.MonsterStats stats, SpawnSide side, int loop,
                            DifficultyPreset difficulty)
    {
        GameConfig config = GameManager.Instance.Config;
        float j = config.monsterCellJitter;
        Vector2 offset = new Vector2(Random.Range(-j, j), Random.Range(-j, j));
        Vector2 pos = GameManager.Instance.Grid.CellCenter(RandomCellOn(side)) + offset;

        Enemy enemy = Instantiate(prefab, pos, Quaternion.identity, transform);
        enemy.Init(
            DifficultyScaling.EnemyHP(stats.hp, loop, difficulty),
            DifficultyScaling.EnemyDamage(stats.damage, loop, difficulty),
            stats.speed,
            stats.exp,
            stats.bodyRadius,
            offset);
        return enemy;
    }

    /// A random cell in the outermost row or column of the given side (corners excluded).
    private static Vector2Int RandomCellOn(SpawnSide side)
    {
        GridMap grid = GameManager.Instance.Grid;
        int w = grid.Width, h = grid.Height;
        switch (side)
        {
            case SpawnSide.Left: return new Vector2Int(0, Random.Range(1, h - 1));
            case SpawnSide.Right: return new Vector2Int(w - 1, Random.Range(1, h - 1));
            case SpawnSide.Top: return new Vector2Int(Random.Range(1, w - 1), h - 1);
            default: return new Vector2Int(Random.Range(1, w - 1), 0);
        }
    }
}
