using System;
using UnityEngine;

/// <summary>
/// Player, base and monster stats (GAME_DESIGN.md sections 5 and 7).
/// Values marked "prototype" are not in the design doc and come from the reference prototype.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "One Minute Siege/Game Config")]
public class GameConfig : ScriptableObject
{
    [Serializable]
    public class PlayerStats
    {
        public float maxHP = 100f;
        public float damage = 10f;
        [Tooltip("Attacks per second")] public float attackRate = 1.2f;
        public float attackRange = 3f;
        public float moveSpeed = 3.6f;
        public float pickupRadius = 1f;
        [Tooltip("Prototype: contact radius used for melee touch damage")] public float bodyRadius = 0.28f;
        [Tooltip("Prototype")] public float projectileSpeed = 13f;
    }

    [Serializable]
    public class BaseStats
    {
        // Base max HP lives in DifficultyPreset.BaseHP.
        public float damage = 8f;
        [Tooltip("Attacks per second")] public float attackRate = 1f;
        public float attackRange = 3f;
        [Tooltip("Prototype")] public float projectileSpeed = 11f;
    }

    [Serializable]
    public class MonsterStats
    {
        [Tooltip("Loop 1 HP before the difficulty preset")] public float hp;
        [Tooltip("Tiles per second")] public float speed;
        public int exp;
        [Tooltip("Melee: damage per second. Ranged: damage per shot.")] public float damage;
        [Tooltip("Ranged only: seconds between shots")] public float attackInterval;
        [Tooltip("Ranged only: tower search range")] public float shootRange;
        [Tooltip("Prototype: body radius for hits and contact")] public float bodyRadius;
    }

    [Header("Loop")]
    [Tooltip("Seconds per survival loop")]
    public float loopDuration = 60f;
    [Tooltip("Total losses that end the game")]
    public int maxLosses = 3;

    [Header("Spawn timing (prototype)")]
    [Tooltip("Spawns are spread over roughly this many seconds")]
    public float spawnWindow = 52f;
    [Tooltip("Below 1 makes spawns denser toward the end of the window")]
    public float spawnCurve = 0.7f;
    [Tooltip("Seconds before the first spawn")]
    public float spawnStartDelay = 0.4f;

    [Header("Map")]
    [Tooltip("Map size in tiles. The player is kept inside this area.")]
    public Vector2 mapSize = new Vector2(16f, 9f);

    [Header("Units")]
    public PlayerStats player = new PlayerStats();
    public BaseStats baseStats = new BaseStats();

    [Header("Monsters")]
    public MonsterStats melee = new MonsterStats
    {
        hp = 30f, speed = 1.5f, exp = 1, damage = 10f, attackInterval = 0f, shootRange = 0f, bodyRadius = 0.26f
    };
    public MonsterStats ranged = new MonsterStats
    {
        hp = 20f, speed = 1.2f, exp = 2, damage = 6f, attackInterval = 1.5f, shootRange = 3f, bodyRadius = 0.24f
    };

    [Tooltip("Prototype: monsters walk up to this far apart sideways across the path (total width, tiles)")]
    public float pathSpread = 0.5f;
}
