using System;
using UnityEngine;

/// Player, base, monster, map, flow field and camera values (GAME_DESIGN.md sections 3 to 6).
/// Values marked "prototype" are not in the design doc and come from the reference prototype.
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
        public float moveSpeed = 4.5f;
        public float pickupRadius = 1f;
        [Tooltip("Collision radius against towers and the base; also melee touch range")] public float bodyRadius = 0.26f;
        [Tooltip("Prototype")] public float projectileSpeed = 13f;
    }

    [Serializable]
    public class BaseStats
    {
        // Base max HP lives in DifficultyPreset.BaseHP.
        public float damage = 8f;
        [Tooltip("Attacks per second")] public float attackRate = 1f;
        public float attackRange = 3.5f;
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
        [Tooltip("Ranged only: shoots the base when this close to it (no tower in range)")] public float baseShootRange;
        [Tooltip("Prototype: body radius for hits and contact")] public float bodyRadius;
    }

    [Header("Loop")]
    [Tooltip("Seconds per survival loop")]
    public float loopDuration = 60f;
    [Tooltip("Total losses that end the game")]
    public int maxLosses = 3;
    [Tooltip("Free orbs added at the end of every loop (win or loss), on top of the levels gained")]
    public int bonusOrbsPerLoop = 2;

    [Header("Spawn timing (prototype)")]
    [Tooltip("Spawns are spread over roughly this many seconds")]
    public float spawnWindow = 50f;
    [Tooltip("Below 1 makes spawns denser toward the end of the window")]
    public float spawnCurve = 0.7f;
    [Tooltip("Seconds before the first spawn")]
    public float spawnStartDelay = 0.4f;

    [Serializable]
    public class MapSettings
    {
        public int width = 36;
        public int height = 36;
        [Tooltip("Bottom-left cell of the base")] public Vector2Int baseCell = new Vector2Int(17, 17);
        [Tooltip("The base is baseSize x baseSize cells")] public int baseSize = 2;
        [Tooltip("Width of the spawn ring along the map edge (no towers)")] public int spawnRing = 1;
        [Tooltip("Width of the reserved ring around the base (no towers)")] public int reservedRing = 1;
        [Tooltip("Character start position (tiles)")] public Vector2 playerStart = new Vector2(18f, 19.6f);
    }

    [Serializable]
    public class FlowFieldSettings
    {
        [Tooltip("Seconds between rebuilds during survival (also rebuilt when a tower dies)")]
        public float rebuildInterval = 0.5f;
        [Tooltip("Entering a tower cell costs 1 + tower current HP / this")]
        public float towerHpDivisor = 50f;
        [Tooltip("Cost multiplier for a diagonal step")]
        public float diagonalCost = 1.414f;
    }

    [Serializable]
    public class CameraSettings
    {
        [Tooltip("Tiles visible during survival (at least this wide and this tall)")]
        public Vector2 viewSize = new Vector2(24f, 13f);
        [Tooltip("Follow lerp factor per second")]
        public float followSharpness = 8f;
        [Tooltip("Not in the doc: empty tiles shown around the map when the build phase fits the whole map")]
        public float fitPadding = 0.5f;
    }

    [Serializable]
    public class BuildInputSettings
    {
        [Tooltip("A press that moves more than this many pixels pans instead of tapping")]
        public float dragThresholdPixels = 10f;
        [Tooltip("Not in the doc: zoom factor per mouse wheel notch")]
        public float wheelZoomStep = 1.15f;
        [Tooltip("Not in the doc: zoom factor per second while holding + or -")]
        public float keyZoomPerSecond = 2.5f;
        [Tooltip("Not in the doc: WASD pan speed in screen heights per second")]
        public float keyPanSpeed = 1.2f;
    }

    [Header("Map, flow field, camera")]
    public MapSettings map = new MapSettings();
    public FlowFieldSettings flowField = new FlowFieldSettings();
    public CameraSettings cameraView = new CameraSettings();

    [Header("Build phase")]
    [Tooltip("Shop order: Bolt, Heal, AoE")]
    public TowerData[] towerTypes;
    public BuildInputSettings buildInput = new BuildInputSettings();

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
        hp = 20f, speed = 1.2f, exp = 2, damage = 6f, attackInterval = 1.5f, shootRange = 3f, baseShootRange = 2.5f,
        bodyRadius = 0.24f
    };

    [Tooltip("Monster collision radius against towers and the base")]
    public float monsterWallRadius = 0.2f;
    [Tooltip("Each monster aims this far (random, +/-) from cell centres so they do not stack")]
    public float monsterCellJitter = 0.2f;
    [Tooltip("Not in the doc: a monster attacks a blocking tower or the base when its edge is this close to it")]
    public float monsterContactSlack = 0.05f;

    [Tooltip("Boss receives this fraction of the enemy slow passive")]
    public float bossSlowFactor = 0.5f;

    [Serializable]
    public struct ExpStep
    {
        [Tooltip("This step applies when levelling up to this level or lower")] public int upToLevel;
        public int expPerLevel;
    }

    [Header("Level and exp")]
    [Tooltip("Stepped exp curve. The last step applies to every level above it.")]
    public ExpStep[] expCurve =
    {
        new ExpStep { upToLevel = 10, expPerLevel = 6 },
        new ExpStep { upToLevel = 20, expPerLevel = 10 },
        new ExpStep { upToLevel = 999, expPerLevel = 14 },
    };
    [Tooltip("Cards offered on each level-up")]
    public int upgradeChoices = 3;

    [Header("Exp gems (prototype)")]
    [Tooltip("Tiles per second while flying to the character")]
    public float gemMagnetSpeed = 10f;
    [Tooltip("Collected when this close to the character")]
    public float gemCollectDistance = 0.3f;

    /// Exp needed to go from level-1 to <paramref name="level"/>.
    public int ExpToReach(int level)
    {
        foreach (ExpStep step in expCurve)
            if (level <= step.upToLevel) return step.expPerLevel;
        return expCurve[expCurve.Length - 1].expPerLevel;
    }
}
