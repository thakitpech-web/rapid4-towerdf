using UnityEngine;

/// Single target: the convoy monster in range with the fewest lane cells left; with no convoy in range,
/// the nearest swarm monster.
public class BoltTower : Tower
{
    /// Tier 3 special (build step 10).
    public bool Pierce => false;

    private float cooldown;

    public override void Act(float dt)
    {
        cooldown = Mathf.Max(0f, cooldown - dt);
        if (cooldown > 0f) return;

        Enemy target = SelectTarget();
        if (target == null) return;

        Projectile shot = Instantiate(Data.ProjectilePrefab, transform.position, Quaternion.identity);
        shot.Launch(target, ModifiedDamage, Data.ProjectileSpeed);
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
}
