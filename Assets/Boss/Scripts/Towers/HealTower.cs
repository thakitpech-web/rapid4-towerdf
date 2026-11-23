using System.Collections.Generic;
using UnityEngine;

/// Heals the lowest-%-HP targets in range: the base or other towers, never the character.
/// Tier 3 overheal shield: healing beyond full HP becomes shield, up to TowerData.ShieldCap × max HP,
/// so full-HP targets whose shield is not full are still healed.
public class HealTower : Tower
{
    public bool OverhealShield => Data.HasSpecial(Tier);

    private readonly List<Unit> candidates = new List<Unit>();
    private readonly List<Unit> chosen = new List<Unit>();

    public override void Act(float dt)
    {
        float amount = Stats.Damage * dt;   // heal per second; the Damage passive does not apply
        foreach (Unit target in LowestHealthTargets())
        {
            float missing = target.MaxHP - target.HP;
            ((IHealable)target).Heal(amount);
            float overflow = amount - missing;
            if (OverhealShield && overflow > 0f)
                ((IHealable)target).AddShield(overflow, Data.ShieldCap * target.MaxHP);
        }
    }

    /// Up to Targets units in range that can take healing, lowest (HP + shield) share of max HP first.
    public List<Unit> LowestHealthTargets()
    {
        float range = Stats.Range;
        candidates.Clear();

        Base b = GameManager.Instance.Base;
        if (CanTake(b, range)) candidates.Add(b);

        foreach (Tower t in GameManager.Instance.Builder.Towers)
            if (t != this && CanTake(t, range)) candidates.Add(t);

        candidates.Sort((a, c) => Fill(a).CompareTo(Fill(c)));

        chosen.Clear();
        for (int i = 0; i < candidates.Count && i < Stats.Targets; i++) chosen.Add(candidates[i]);
        return chosen;
    }

    private static float Fill(Unit u) => (u.HP + u.Shield) / u.MaxHP;

    private bool CanTake(Unit u, float range)
    {
        if (!u.IsAlive() || Vector2.Distance(Position, u.Position) > range) return false;
        if (u.HP < u.MaxHP) return true;
        return OverhealShield && u.Shield < Data.ShieldCap * u.MaxHP;
    }
}
