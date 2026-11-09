using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>The character: moves with the Move action and auto-attacks the nearest enemy in range.</summary>
public class PlayerCharacter : Unit
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private Projectile projectilePrefab;

    private Vector2 startPosition;
    private float attackCooldown;

    public float MoveSpeed => Config.player.moveSpeed;
    public float AttackDamage => Config.player.damage;     // character damage passive is added in step 4
    public float AttackRate => Config.player.attackRate;   // rapid fire skill is added in step 7
    public float AttackRange => Config.player.attackRange;
    public float PickupRadius => Config.player.pickupRadius;
    public float BodyRadius => Config.player.bodyRadius;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
    }

    /// <summary>Back to the start position with full HP.</summary>
    public void ResetForLoop()
    {
        RestoreFull(Config.player.maxHP);
        transform.position = startPosition;
        attackCooldown = 0f;
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

        Vector2 next = Position + dir * (MoveSpeed * Time.deltaTime);
        Vector2 map = Config.mapSize;
        next.x = Mathf.Clamp(next.x, BodyRadius, map.x - BodyRadius);
        next.y = Mathf.Clamp(next.y, BodyRadius, map.y - BodyRadius);
        transform.position = next;
    }

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
