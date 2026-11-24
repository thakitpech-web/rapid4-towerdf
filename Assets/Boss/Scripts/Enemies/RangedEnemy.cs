using UnityEngine;

/// Stops and fires a straight shot (Damage on impact, every attack interval) when PickTarget finds something;
/// otherwise moves. The shot flies toward where the target was, so the character can dodge it.
/// Convoy: the nearer of a tower or the character within ShootRange. Swarm: the character within ShootRange.
/// It never shoots the base: like melee, it walks into it (Enemy.EnterBase).
public class RangedEnemy : Enemy
{
    [Tooltip("Shot fired at the target; its hit effect is set on the projectile prefab")]
    [SerializeField] private Projectile projectilePrefab;

    private float attackCooldown;

    public float ShootRange => Config.ranged.shootRange;

    protected override void Tick(float dt)
    {
        attackCooldown = Mathf.Max(0f, attackCooldown - dt);

        Unit target = PickTarget();
        if (target != null) Attack(target);
        else base.Tick(dt);
    }

    public Unit PickTarget()
    {
        GameManager gm = GameManager.Instance;
        PlayerCharacter p = gm.Player;
        bool playerInRange = p != null && p.IsAlive() && Vector2.Distance(Position, p.Position) <= ShootRange;
        if (Group == EnemyGroup.Swarm) return playerInRange ? p : null;

        Tower tower = FindNearestTower();
        if (playerInRange && (tower == null ||
                              Vector2.Distance(Position, p.Position) < Vector2.Distance(Position, tower.Position)))
            return p;
        return tower;
    }

    /// The closest living tower within ShootRange, or null.
    public Tower FindNearestTower()
    {
        Tower best = null;
        float bestDist = ShootRange;
        foreach (Tower t in GameManager.Instance.Builder.Towers)
        {
            if (!t.IsAlive()) continue;
            float d = Vector2.Distance(Position, t.Position);
            if (d <= bestDist)
            {
                bestDist = d;
                best = t;
            }
        }
        return best;
    }

    public override void Attack(Unit target)
    {
        if (attackCooldown > 0f || target == null || !target.IsAlive()) return;

        if (projectilePrefab != null)
        {
            Projectile shot = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            shot.LaunchStraight(target.Position, Damage, Config.ranged.projectileSpeed, Config.ranged.projectileRange);
        }
        else
        {
            target.TakeDamage(Damage);   // no prefab assigned: instant hit
        }
        attackCooldown = Config.ranged.attackInterval;
    }
}
