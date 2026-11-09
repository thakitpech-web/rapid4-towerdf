using UnityEngine;

/// <summary>
/// Walks the path. At the end of the path it shoots the base (Damage every attack interval).
/// Stopping to shoot the nearest tower within ShootRange is added in build step 6, once towers exist.
/// </summary>
public class RangedEnemy : Enemy
{
    private float attackCooldown;

    public float ShootRange => Config.ranged.shootRange;

    protected override void Tick(float dt)
    {
        attackCooldown = Mathf.Max(0f, attackCooldown - dt);
        base.Tick(dt);
    }

    public override void Attack()
    {
        if (attackCooldown > 0f) return;

        Base target = GameManager.Instance.Base;
        if (target == null || !target.IsAlive()) return;

        target.TakeDamage(Damage);
        attackCooldown = Config.ranged.attackInterval;
    }
}
