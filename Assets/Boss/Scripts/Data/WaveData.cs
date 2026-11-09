using UnityEngine;

/// <summary>Monsters for one loop (GAME_DESIGN.md section 7). Make one asset per loop, 8 in total.</summary>
[CreateAssetMenu(fileName = "Wave_01", menuName = "One Minute Siege/Wave Data")]
public class WaveData : ScriptableObject
{
    public int MeleeCount;
    public int RangedCount;
    public bool HasBoss;
    public float BossSpawnTime = 10f;
}
