using UnityEngine;

/// Single target: the convoy monster in range with the fewest lane cells left; with no convoy in range,
/// the nearest swarm monster. Tier 3 pierce: each hit also strikes the nearest other enemy within
/// TowerData.PierceRadius of the target, for the same damage.
public class BoltTower : Tower
{
    [Tooltip("Optional effect on the enemy struck by pierce")]
    [SerializeField] private TimedEffect pierceEffect;
    [Tooltip("Radius passed to the pierce effect (used when its Scale To Radius is on)")]
    [SerializeField] private float pierceEffectRadius = 0.3f;

    public bool Pierce => Data.HasSpecial(Tier);

    private float cooldown;

    public override void Act(float dt)
    {
        cooldown = Mathf.Max(0f, cooldown - dt);
        if (cooldown > 0f) return;

        Enemy target = SelectTarget();
        if (target == null) return;

        float damage = ModifiedDamage;
        Projectile shot = Instantiate(Data.ProjectilePrefab, transform.position, Quaternion.identity);
        if (Pierce) shot.Launch(target, damage, Data.ProjectileSpeed, (hit, at) => PierceFrom(hit, at, damage));
        else shot.Launch(target, damage, Data.ProjectileSpeed);
        cooldown = 1f / Stats.Rate;
    }

    public Enemy SelectTarget()
    {
        float range = Stats.Range;
        Enemy best = null;
        float bestLeft = float.PositiveInfinity;
        foreach (Enemy e in Enemy.Active)
        {
            if (!e.IsAlive() || e.Group != EnemyGroup.Convoy || Vector2.Distance(Position, e.Position) > range)
                continue;
            float left = e.LaneDistanceLeft;
            if (left < bestLeft)
            {
                bestLeft = left;
                best = e;
            }
        }
        return best != null ? best : Enemy.FindNearest(Position, range, EnemyGroup.Swarm);
    }

    /// Strikes the nearest other living enemy within PierceRadius of where the first one was hit.
    private void PierceFrom(Unit first, Vector2 at, float damage)
    {
        Enemy second = null;
        float bestDist = Data.PierceRadius;
        foreach (Enemy e in Enemy.Active)
        {
            if (e == first || !e.IsAlive()) continue;
            float d = Vector2.Distance(at, e.Position);
            if (d <= bestDist)
            {
                bestDist = d;
                second = e;
            }
        }
        if (second == null) return;

        Vector2 pos = second.Position;
        second.TakeDamage(damage);
        TimedEffect.Spawn(pierceEffect, pos, pierceEffectRadius);
    }
}
