using UnityEngine;

/// Monsters for one loop (GAME_DESIGN.md section 6). Make one asset per loop, 8 in total.
[CreateAssetMenu(fileName = "Wave_01", menuName = "One Minute Siege/Wave Data")]
public class WaveData : ScriptableObject
{
    public int MeleeCount;
    public int RangedCount;

    [Tooltip("Open map edges this loop. Each monster picks one at random (GAME_DESIGN.md section 3).")]
    public SpawnSide[] Sides = { SpawnSide.Left, SpawnSide.Right };

    public bool HasBoss;
    public float BossSpawnTime = 10f;
}
