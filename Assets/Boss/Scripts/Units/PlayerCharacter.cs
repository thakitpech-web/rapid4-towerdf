using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// The character: moves with the Move action and auto-attacks the nearest enemy in range.
/// Holds up to two active skills unlocked from level-up cards.
public class PlayerCharacter : Unit
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private Projectile projectilePrefab;

    private float attackCooldown;
    private readonly List<ActiveSkill> skills = new List<ActiveSkill>();

    public IReadOnlyList<ActiveSkill> Skills => skills;

    /// Set by the Rapid Fire skill while active.
    public float AttackRateMultiplier { get; set; } = 1f;

    public float MoveSpeed => Config.player.moveSpeed;
    public float AttackDamage => Config.player.damage * (1f + Modifier(UpgradeType.CharacterDamage));
    public float AttackRate => Config.player.attackRate * AttackRateMultiplier;
    public float AttackRange => Config.player.attackRange;
    public float PickupRadius => Config.player.pickupRadius * (1f + Modifier(UpgradeType.PickupRadius));
    public float BodyRadius => Config.player.bodyRadius;

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
    }

    /// Back to the start position (next to the base) with full HP and skills ready.
    public void ResetForLoop()
    {
        RestoreFull(Config.player.maxHP);
        transform.position = Config.map.playerStart;
        attackCooldown = 0f;
        foreach (ActiveSkill s in skills) s.ResetForLoop();
        AttackRateMultiplier = 1f;
    }

    public bool HasSkill(SkillType type)
    {
        foreach (ActiveSkill s in skills)
            if (s.Data.Skill == type) return true;
        return false;
    }

    /// Unlocks a skill from its level-up card.
    public void AddSkill(ActiveSkillUpgrade data)
    {
        if (HasSkill(data.Skill)) return;

        ActiveSkill skill;
        switch (data.Skill)
        {
            case SkillType.RapidFire: skill = gameObject.AddComponent<RapidFireSkill>(); break;
            default: skill = gameObject.AddComponent<BaseInvincibleSkill>(); break;
        }
        skill.Init(data);
        skills.Add(skill);
    }

    private void Update()
    {
        if (!IsSurvivalPhase || !IsAlive()) return;

        Move(moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero);
        AutoAttack();
    }

    public void Move(Vector2 dir)
    {
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        // Towers and the base are solid; ResolveCollision also keeps the character inside the map.
        Vector2 next = Position + dir * (MoveSpeed * Time.deltaTime);
        transform.position = GameManager.Instance.Grid.ResolveCollision(next, BodyRadius);
    }

    public void CollectExp(ExpGem gem)
    {
        GameManager.Instance.Levels.AddExp(gem.Value);
    }

    /// Used by the fallback card. The character is not IHealable: heal towers never heal it.
    public void Heal(float amount)
    {
        if (!IsAlive()) return;
        hp = Mathf.Min(maxHP, hp + amount);
    }

    private static float Modifier(UpgradeType t) => GameManager.Instance.Upgrades.GetModifier(t);

    public void AutoAttack()
    {
        attackCooldown = Mathf.Max(0f, attackCooldown - Time.deltaTime);
        if (attackCooldown > 0f) return;

        Enemy target = Enemy.FindNearest(Position, AttackRange);
        if (target == null) return;

        Projectile shot = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        shot.Launch(target, AttackDamage, Config.player.projectileSpeed);
        attackCooldown = 1f / AttackRate;
    }
}
