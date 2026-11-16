using System.Collections.Generic;
using UnityEngine;

/// Heals the lowest-%-HP targets in range: the base or other towers, never the character.
public class HealTower : Tower
{
    /// Tier 3 special (build step 10).
    public bool OverhealShield => false;

    private readonly List<Unit> candidates = new List<Unit>();

    public override void Act(float dt)
    {
        float amount = Stats.Damage * dt;   // heal per second; the tower damage passive does not apply
        foreach (IHealable target in LowestHealthTargets())
            target.Heal(amount);
    }

    public List<IHealable> LowestHealthTargets()
    {
        float range = Stats.Range;
        candidates.Clear();

        Base b = GameManager.Instance.Base;
        if (IsHurtAndInRange(b, range)) candidates.Add(b);

        foreach (Tower t in GameManager.Instance.Builder.Towers)
            if (t != this && IsHurtAndInRange(t, range)) candidates.Add(t);

        candidates.Sort((a, c) => (a.HP / a.MaxHP).CompareTo(c.HP / c.MaxHP));

        var result = new List<IHealable>();
        for (int i = 0; i < candidates.Count && i < Stats.Targets; i++)
            result.Add((IHealable)candidates[i]);
        return result;
    }

    private bool IsHurtAndInRange(Unit u, float range) =>
        u.IsAlive() && u.HP < u.MaxHP && Vector2.Distance(Position, u.Position) <= range;
}
