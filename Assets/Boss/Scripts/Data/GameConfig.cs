using System;
using UnityEngine;

/// Player, base, monster, map and camera values (GAME_DESIGN.md sections 3 to 7).
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
        [Tooltip("Convoy HP at loop 1 before the difficulty preset (swarm gets a share of it)")] public float hp;
        [Tooltip("Tiles per second")] public float speed;
        public int exp;
        [Tooltip("Melee: damage per second. Ranged: damage per shot.")] public float damage;
        [Tooltip("Ranged only: seconds between shots")] public float attackInterval;
        [Tooltip("Ranged only: convoy shoots towers, swarm shoots the character within this range")] public float shootRange;
        [Tooltip("Ranged convoy only: shoots the base when this close to it (no tower in range)")] public float baseShootRange;
        [Tooltip("Prototype: body radius for hits and contact")] public float bodyRadius;
        [Tooltip("Ranged only. Not in the doc: shot speed in tiles per second")] public float projectileSpeed;
        [Tooltip("Ranged only. Not in the doc: a straight shot vanishes after flying this far (tiles)")] public float projectileRange;
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

    /// Map layout in Unity cells (x right, y up; cell (0,0) is the bottom-left corner).
    /// Lanes drawn in the scene (LanePath) replace the default lanes below.
    [Serializable]
    public class MapSettings
    {
        public int width = 34;
        public int height = 20;
        [Tooltip("Bottom-left cell of the base")] public Vector2Int baseCell = new Vector2Int(16, 9);
        [Tooltip("The base is baseSize x baseSize cells")] public int baseSize = 2;
        [Tooltip("Character start position (tiles)")] public Vector2 playerStart = new Vector2(18.5f, 8.3f);

        [Header("Default lanes: turn points from the map edge to the cell next to the base")]
        public Vector2Int[] leftLane =
        {
            new Vector2Int(0, 10), new Vector2Int(5, 10), new Vector2Int(5, 15),
            new Vector2Int(10, 15), new Vector2Int(10, 10), new Vector2Int(15, 10)
        };
        public Vector2Int[] rightLane =
        {
            new Vector2Int(33, 9), new Vector2Int(28, 9), new Vector2Int(28, 4),
            new Vector2Int(23, 4), new Vector2Int(23, 9), new Vector2Int(18, 9)
        };
        public Vector2Int[] topLane =
        {
            new Vector2Int(16, 19), new Vector2Int(16, 17), new Vector2Int(12, 17),
            new Vector2Int(12, 13), new Vector2Int(16, 13), new Vector2Int(16, 11)
        };
        public Vector2Int[] bottomLane =
        {
            new Vector2Int(17, 0), new Vector2Int(17, 2), new Vector2Int(21, 2),
            new Vector2Int(21, 6), new Vector2Int(17, 6), new Vector2Int(17, 8)
        };
    }

    [Serializable]
    public class CameraSettings
    {
        [Tooltip("Build phase: the closest zoom shows at least this many tiles (width, height)")]
        public Vector2 buildMinView = new Vector2(16f, 9f);
        [Tooltip("Empty tiles shown around the map when the camera fits the whole map")]
        public float fitPadding = 0f;
        [Tooltip("Off: the whole map is always visible (thin empty bars where the screen shape differs). " +
                 "On: the map fills the screen and the edges that do not fit are cut off.")]
        public bool fillScreen;
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

    [Header("Map and camera")]
    public MapSettings map = new MapSettings();
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
        hp = 24f, speed = 1.5f, exp = 1, damage = 10f, attackInterval = 0f, shootRange = 0f, bodyRadius = 0.26f
    };
    public MonsterStats ranged = new MonsterStats
    {
        hp = 16f, speed = 1.2f, exp = 2, damage = 6f, attackInterval = 1.5f, shootRange = 3f, baseShootRange = 2.5f,
        bodyRadius = 0.24f, projectileSpeed = 8f, projectileRange = 4.5f
    };

    [Tooltip("Monster collision radius against towers and the base")]
    public float monsterWallRadius = 0.2f;
    [Tooltip("Each convoy monster walks this far (random, +/-) from lane cell centres so they do not stack")]
    public float convoyLaneOffset = 0.25f;
    [Tooltip("Not in the doc: a monster counts as touching the base when its edge is this close to it")]
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
