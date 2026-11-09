using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the spawn schedule for a loop from WaveData and creates monsters at the start of the path
/// with stats scaled for the current loop and difficulty. GameManager calls BuildSchedule and Tick.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public struct SpawnEvent
    {
        public float Time;
        public EnemyType Type;
    }

    [Tooltip("One WaveData per loop, loop 1 first")]
    [SerializeField] private WaveData[] waves;
    [SerializeField] private Path path;
    [SerializeField] private MeleeEnemy meleePrefab;
    [SerializeField] private RangedEnemy rangedPrefab;

    public WaveData[] Waves => waves;
    public Queue<SpawnEvent> Schedule { get; } = new Queue<SpawnEvent>();
    public Path Path => path;

    /// <summary>Number of loops in the game (one per WaveData).</summary>
    public int LoopCount => waves.Length;

    /// <summary>
    /// Spreads melee and ranged monsters over the spawn window, getting denser over time,
    /// with ranged monsters mixed in evenly (same as the prototype).
    /// </summary>
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
            Schedule.Enqueue(new SpawnEvent { Time = t, Type = isRanged ? EnemyType.Ranged : EnemyType.Melee });
        }
        // Boss (wave.HasBoss) is added in build step 9.
    }

    /// <summary>Spawns everything scheduled up to <paramref name="elapsed"/> seconds into the loop.</summary>
    public void Tick(float elapsed)
    {
        while (Schedule.Count > 0 && Schedule.Peek().Time <= elapsed)
            Spawn(Schedule.Dequeue().Type);
    }

    public Enemy Spawn(EnemyType type)
    {
        GameManager gm = GameManager.Instance;
        GameConfig config = gm.Config;

        switch (type)
        {
            case EnemyType.Melee:
                return SpawnFrom(meleePrefab, config.melee, gm.CurrentLoop, gm.Difficulty, config.pathSpread);
            case EnemyType.Ranged:
                return SpawnFrom(rangedPrefab, config.ranged, gm.CurrentLoop, gm.Difficulty, config.pathSpread);
            default:
                throw new System.NotSupportedException($"{type} enemies are added in a later build step.");
        }
    }

    /// <summary>Removes every monster from the map (loop reset).</summary>
    public void ClearAll()
    {
        Schedule.Clear();
        foreach (Enemy e in new List<Enemy>(Enemy.Active))
            Destroy(e.gameObject);
    }

    private Enemy SpawnFrom(Enemy prefab, GameConfig.MonsterStats stats, int loop, DifficultyPreset difficulty,
                            float spread)
    {
        Enemy enemy = Instantiate(prefab, path.PositionAt(0f), Quaternion.identity, transform);
        enemy.Init(
            path,
            DifficultyScaling.EnemyHP(stats.hp, loop, difficulty),
            DifficultyScaling.EnemyDamage(stats.damage, loop, difficulty),
            stats.speed,
            stats.exp,
            stats.bodyRadius,
            Random.Range(-spread * 0.5f, spread * 0.5f));
        return enemy;
    }
}
