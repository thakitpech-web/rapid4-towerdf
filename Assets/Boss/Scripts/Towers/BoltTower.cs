using UnityEngine;

/// Single target: shoots the enemy in range with the smallest flow-field distance to the base.
public class BoltTower : Tower
{
    /// Tier 3 special (build step 10).
    public bool Pierce => false;

    private float cooldown;

    public override void Act(float dt)
    {
        cooldown = Mathf.Max(0f, cooldown - dt);
        if (cooldown > 0f) return;

        Enemy target = SelectClosestToBase();
        if (target == null) return;

        Projectile shot = Instantiate(Data.ProjectilePrefab, transform.position, Quaternion.identity);
        shot.Launch(target, ModifiedDamage, Data.ProjectileSpeed);
        cooldown = 1f / Stats.Rate;
    }

    public Enemy SelectClosestToBase()
    {
        float range = Stats.Range;
        Enemy best = null;
        float bestFlow = float.PositiveInfinity;
        foreach (Enemy e in Enemy.Active)
        {
            if (!e.IsAlive() || Vector2.Distance(Position, e.Position) > range) continue;
            float flow = e.FlowDistance;
            if (flow < bestFlow)
            {
                bestFlow = flow;
                best = e;
            }
        }
        return best;
    }
}
