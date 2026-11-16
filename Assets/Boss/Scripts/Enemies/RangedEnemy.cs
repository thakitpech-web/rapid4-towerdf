using UnityEngine;

/// Stops and shoots the nearest tower within ShootRange (Damage every attack interval). With no tower in
/// range, shoots the base once within BaseShootRange of it; otherwise follows the flow field.
public class RangedEnemy : Enemy
{
    private static readonly Color ShotColor = new Color(1f, 0.3f, 0.6f, 0.8f);
    private const float ShotFlashRadius = 0.2f, ShotFlashSeconds = 0.15f;

    private float attackCooldown;

    public float ShootRange => Config.ranged.shootRange;
    public float BaseShootRange => Config.ranged.baseShootRange;

    protected override void Tick(float dt)
    {
        attackCooldown = Mathf.Max(0f, attackCooldown - dt);

        Tower tower = FindNearestTower();
        if (tower != null)
        {
            Attack(tower);
            return;
        }

        GameManager gm = GameManager.Instance;
        if (gm.Grid.DistanceToBase(Position) <= BaseShootRange)
            Attack(gm.Base);
        else
            base.Tick(dt);
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

        target.TakeDamage(Damage);
        BlastFlash.Spawn(target.Position, ShotFlashRadius, ShotColor, ShotFlashSeconds);
        attackCooldown = Config.ranged.attackInterval;
    }
}
