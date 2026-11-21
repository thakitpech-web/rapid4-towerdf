using UnityEngine;

/// Monster stat scaling per loop and difficulty preset (GAME_DESIGN.md section 7). Loops are 1-based.
public static class DifficultyScaling
{
    /// base HP × start multiplier × growth^(loop−1), then × the swarm multiplier for swarm monsters.
    public static float EnemyHP(float baseHp, int loop, DifficultyPreset d, EnemyGroup g)
    {
        float hp = baseHp * d.HpStart * Mathf.Pow(d.HpGrowth, loop - 1);
        return g == EnemyGroup.Swarm ? hp * d.SwarmHpMultiplier : hp;
    }

    public static float EnemyDamage(float baseDmg, int loop, DifficultyPreset d)
    {
        return baseDmg * d.DamageMultiplier * Mathf.Pow(d.DamageGrowth, loop - 1);
    }
}
