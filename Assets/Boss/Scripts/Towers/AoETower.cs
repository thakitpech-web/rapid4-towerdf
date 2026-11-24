using System.Collections.Generic;
using UnityEngine;

/// Area damage: fires at the densest cluster in range, equal damage to every enemy in the blast radius.
public class AoETower : Tower
{
    [Tooltip("Effect shown at the blast point, sized to the blast radius")]
    [SerializeField] private TimedEffect blastEffect;

    [Tooltip("Tier 3: look of the fire zone, sized so 1 unit of art = the blast diameter; removed when the fire ends")]
    [SerializeField] private GameObject fireEffect;

    [Header("Bomb (used when TowerData has a Projectile Prefab)")]
    [Tooltip("Seconds the bomb flies before it lands and blasts")]
    [SerializeField] private float bombFlightTime = 0.6f;
    [Tooltip("Height of the arc at its middle, in tiles")]
    [SerializeField] private float bombArcHeight = 1.5f;

    /// Tier 3: each blast leaves a fire zone (TowerData.FireDamagePerSecond for FireDuration seconds).
    public bool LeavesFire => Data.HasSpecial(Tier);

    private float cooldown;
    private static readonly List<Enemy> hits = new List<Enemy>();

    public override void Act(float dt)
    {
        cooldown = Mathf.Max(0f, cooldown - dt);
        if (cooldown > 0f) return;

        if (!FindDensest(out Enemy center)) return;

        // Read the numbers now, so a bomb still in the air works if the tower is destroyed meanwhile.
        float radius = Stats.Radius;
        float damage = ModifiedDamage;
        bool leavesFire = LeavesFire;
        // The Damage passive also boosts the fire.
        float fireDps = Data.FireDamagePerSecond * (1f + GameManager.Instance.Upgrades.GetModifier(UpgradeType.Damage));
        float fireDuration = Data.FireDuration;
        TimedEffect blast = blastEffect;
        GameObject fire = fireEffect;

        void Blast(Vector2 point) => Explode(point, radius, damage, leavesFire, fireDps, fireDuration, blast, fire);

        if (Data.ProjectilePrefab != null)
        {
            Projectile bomb = Instantiate(Data.ProjectilePrefab, transform.position, Quaternion.identity);
            bomb.LaunchArc(center, bombFlightTime, bombArcHeight, Blast);
        }
        else Blast(center.Position);

        cooldown = 1f / Stats.Rate;
    }

    // Equal damage to every enemy in the radius, the blast effect, and the tier 3 fire zone.
    private static void Explode(Vector2 point, float radius, float damage, bool leavesFire, float fireDps, float fireDuration,
                                TimedEffect blastEffect, GameObject fireEffect)
    {
        hits.Clear();
        foreach (Enemy e in Enemy.Active)
            if (e.IsAlive() && Vector2.Distance(point, e.Position) <= radius) hits.Add(e);

        foreach (Enemy e in hits) e.TakeDamage(damage);   // TakeDamage may destroy, so hit a copy

        TimedEffect.Spawn(blastEffect, point, radius);
        if (leavesFire) FireZone.Spawn(point, radius, fireDps, fireDuration, fireEffect);
    }

    /// The enemy in range with the most enemies within the blast radius around it.
    public bool FindDensest(out Enemy center)
    {
        float range = Stats.Range, radius = Stats.Radius;
        center = null;
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
                center = candidate;
            }
        }
        return bestCount > 0;
    }
}
