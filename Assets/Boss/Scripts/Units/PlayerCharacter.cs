using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// The character: moves with the Move action and auto-attacks the nearest enemy in range.
/// Holds up to two active skills unlocked from level-up cards.
public class PlayerCharacter : Unit
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private Projectile projectilePrefab;

    [Tooltip("Animator on the character or its sprite child. Needs Float parameters MoveX, MoveY and Speed.")]
    [SerializeField] private Animator animator;

    // Animator parameters: the walk direction (kept while standing still) and how fast the stick is pushed (0..1).
    private static readonly int MoveXParam = Animator.StringToHash("MoveX");
    private static readonly int MoveYParam = Animator.StringToHash("MoveY");
    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    private float attackCooldown;
    private readonly List<ActiveSkill> skills = new List<ActiveSkill>();

    public IReadOnlyList<ActiveSkill> Skills => skills;

    /// Set by the Rapid Fire skill while active.
    public float AttackRateMultiplier { get; set; } = 1f;

    public float MoveSpeed => Config.player.moveSpeed * (1f + Modifier(UpgradeType.MoveSpeed));
    public float AttackDamage => Config.player.damage * (1f + Modifier(UpgradeType.Damage));
    public float AttackRate => Config.player.attackRate * (1f + Modifier(UpgradeType.AttackSpeed)) * AttackRateMultiplier;
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
        transform.position = GameManager.Instance.Grid.PlayerStart;
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
        if (!IsSurvivalPhase || !IsAlive())
        {
            Animate(Vector2.zero);
            return;
        }

        Vector2 input = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Move(input);
        AutoAttack();
        Animate(input);
    }

    public override void TakeDamage(float amount)
    {
        if (amount > 0f && IsAlive()) SoundManager.Play("Player_GotHit");
        base.TakeDamage(amount);
    }

    // Tells the Animator where the character walks; the Animator Controller picks the clip.
    private void Animate(Vector2 input)
    {
        if (animator == null || !animator.isActiveAndEnabled) return;

        input = Vector2.ClampMagnitude(input, 1f);
        animator.SetFloat(SpeedParam, input.magnitude);
        if (input.sqrMagnitude > 0.0001f)
        {
            animator.SetFloat(MoveXParam, input.x);
            animator.SetFloat(MoveYParam, input.y);
        }
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
        SoundManager.Play("Player_GetExp");
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
        SoundManager.Play("Player_Shoot");
        attackCooldown = 1f / AttackRate;
    }
}
