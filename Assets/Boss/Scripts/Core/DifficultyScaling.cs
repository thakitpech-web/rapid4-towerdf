using UnityEngine;

/// <summary>Monster stat scaling per loop and difficulty preset. Loops are 1-based.</summary>
public static class DifficultyScaling
{
    public static float EnemyHP(float baseHp, int loop, DifficultyPreset d)
    {
        return baseHp * d.HpStart * Mathf.Pow(d.HpGrowth, loop - 1);
    }

    public static float EnemyDamage(float baseDmg, int loop, DifficultyPreset d)
    {
        return baseDmg * d.DamageMultiplier * Mathf.Pow(d.DamageGrowth, loop - 1);
    }
}
