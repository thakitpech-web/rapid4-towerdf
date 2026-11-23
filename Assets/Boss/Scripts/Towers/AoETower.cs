using System.Collections.Generic;
using UnityEngine;

/// Area damage: fires at the densest cluster in range, equal damage to every enemy in the blast radius.
public class AoETower : Tower
{
    [Tooltip("Effect shown at the blast point, sized to the blast radius")]
    [SerializeField] private TimedEffect blastEffect;

    [Tooltip("Tier 3: look of the fire zone, sized so 1 unit of art = the blast diameter; removed when the fire ends")]
    [SerializeField] private GameObject fireEffect;

    /// Tier 3: each blast leaves a fire zone (TowerData.FireDamagePerSecond for FireDuration seconds).
    public bool LeavesFire => Data.HasSpecial(Tier);

    private float cooldown;
    private readonly List<Enemy> hits = new List<Enemy>();

    public override void Act(float dt)
    {
        cooldown = Mathf.Max(0f, cooldown - dt);
        if (cooldown > 0f) return;

        if (!FindDensestPoint(out Vector2 point)) return;

        float radius = Stats.Radius;
        hits.Clear();
        foreach (Enemy e in Enemy.Active)
            if (e.IsAlive() && Vector2.Distance(point, e.Position) <= radius) hits.Add(e);

        float damage = ModifiedDamage;
        foreach (Enemy e in hits) e.TakeDamage(damage);   // TakeDamage may destroy, so hit a copy

        TimedEffect.Spawn(blastEffect, point, radius);
        if (LeavesFire)
        {
            // The Damage passive also boosts the fire.
            float fireDps = Data.FireDamagePerSecond *
                            (1f + GameManager.Instance.Upgrades.GetModifier(UpgradeType.Damage));
            FireZone.Spawn(point, radius, fireDps, Data.FireDuration, fireEffect);
        }
        cooldown = 1f / Stats.Rate;
    }

    /// The enemy position in range with the most enemies within the blast radius around it.
    public bool FindDensestPoint(out Vector2 point)
    {
        float range = Stats.Range, radius = Stats.Radius;
        point = default;
        int bestCount = 0;

        foreach (Enemy candidate in Enemy.Active)
        {
            if (!candidate.IsAlive() || Vector2.Distance(Position, candidate.Position) > range) continue;

            int count = 0;
            foreach (Enemy e in Enemy.Active)
                if (e.IsAlive() && Vector2.Distance(candidate.Position, e.Position) <= radius) count++;

            if (count > bestCount)
            {
                bestCount = count;
                point = candidate.Position;
            }
        }
        return bestCount > 0;
    }
}
