using UnityEngine;

/// <summary>The base at the end of the path. Auto-attacks the nearest enemy in range.</summary>
public class Base : Unit, IHealable
{
    [SerializeField] private Projectile projectilePrefab;

    private float attackCooldown;

    public float AttackDamage => Config.baseStats.damage;   // tower damage passive is added in step 4
    public float AttackRange => Config.baseStats.attackRange;
    public bool IsInvincible { get; set; }                  // set by the Base Invincible skill (step 7)

    /// <summary>Full HP for a new loop. Max HP comes from the difficulty preset (plus passives later).</summary>
    public void ResetForLoop(float newMaxHP)
    {
        RestoreFull(newMaxHP);
        attackCooldown = 0f;
        IsInvincible = false;
    }

    private void Update()
    {
        if (!IsSurvivalPhase || !IsAlive()) return;
        AutoAttack();
    }

    public void AutoAttack()
    {
        attackCooldown = Mathf.Max(0f, attackCooldown - Time.deltaTime);
        if (attackCooldown > 0f) return;

        Enemy target = Enemy.FindNearest(Position, AttackRange);
        if (target == null) return;

        Projectile shot = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        shot.Launch(target, AttackDamage, Config.baseStats.projectileSpeed);
        attackCooldown = 1f / Config.baseStats.attackRate;
    }

    public override void TakeDamage(float amount)
    {
        if (IsInvincible) return;
        base.TakeDamage(amount);
    }

    public void Heal(float amount)
    {
        if (!IsAlive()) return;
        hp = Mathf.Min(maxHP, hp + amount);
    }

    public void AddShield(float amount, float cap)
    {
        if (!IsAlive()) return;
        shield = Mathf.Min(cap, shield + amount);
    }
}
