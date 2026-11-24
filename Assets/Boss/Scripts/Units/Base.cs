using UnityEngine;

/// The 2×2 base in the centre of the map. Auto-attacks the nearest enemy in range.
public class Base : Unit, IHealable
{
    [SerializeField] private Projectile projectilePrefab;

    [Header("Damage look")]
    [Tooltip("Main sprite of the base")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("Body sprite by HP, from full to ruined. With 4 sprites: >75%, >50%, >25%, 25% and below. Empty keeps the Body sprite.")]
    [SerializeField] private Sprite[] damageStages;

    private float attackCooldown;

    public float AttackDamage =>
        Config.baseStats.damage * (1f + GameManager.Instance.Upgrades.GetModifier(UpgradeType.Damage));
    public float AttackRange => Config.baseStats.attackRange;
    public bool IsInvincible { get; set; }                  // set by the Base Invincible skill (step 9)

    /// Grid cells the base fills (2×2 at the map centre).
    public Vector2Int[] Cells { get; private set; }

    /// Moves the base onto its grid cells. Called once by GameManager after the grid is ready.
    public void PlaceOn(GridMap grid)
    {
        RectInt r = grid.BaseCells;
        Cells = new Vector2Int[r.width * r.height];
        int i = 0;
        foreach (Vector2Int c in r.allPositionsWithin) Cells[i++] = c;

        transform.position = grid.BaseCenter;
    }

    /// Full HP for a new loop. Max HP comes from the difficulty preset (plus passives later).
    public void ResetForLoop(float newMaxHP)
    {
        RestoreFull(newMaxHP);
        attackCooldown = 0f;
        IsInvincible = false;
    }

    /// Base max HP passive picked mid-loop: the increase is also added to current HP.
    public void RaiseMaxHP(float newMaxHP)
    {
        if (IsAlive()) hp += newMaxHP - maxHP;
        maxHP = newMaxHP;
    }

    private void Update()
    {
        RefreshDamageLook();
        if (!IsSurvivalPhase || !IsAlive()) return;
        AutoAttack();
    }

    // Picks the damage stage sprite for the current HP (runs every frame so heals and loop resets show too).
    private void RefreshDamageLook()
    {
        if (body == null || damageStages == null || damageStages.Length == 0) return;

        int n = damageStages.Length;
        float lost = maxHP > 0f ? 1f - hp / maxHP : 0f;
        int stage = Mathf.Clamp(Mathf.FloorToInt(lost * n), 0, n - 1);

        Sprite s = damageStages[stage];
        if (s != null && body.sprite != s) body.sprite = s;
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

    /// Time.time of the last damage taken (the base arrow turns red while it is recent).
    public float LastHitTime { get; private set; } = float.NegativeInfinity;

    public override void TakeDamage(float amount)
    {
        if (IsInvincible || amount <= 0f || !IsAlive()) return;
        LastHitTime = Time.time;
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
