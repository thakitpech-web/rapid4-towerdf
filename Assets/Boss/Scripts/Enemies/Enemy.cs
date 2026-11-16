using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Melee,
    Ranged,
    Boss
}

/// A monster that spawns on a map edge and walks to the base along the flow field.
/// When the next cell is a tower or the base, it stops and attacks it instead of moving.
public abstract class Enemy : Unit
{
    private static readonly List<Enemy> active = new List<Enemy>();

    /// Every living enemy in the scene.
    public static IReadOnlyList<Enemy> Active => active;

    public float Speed { get; protected set; }
    public float Damage { get; protected set; }
    public int ExpReward { get; protected set; }

    /// Radius used for projectile hits and touching the character.
    public float BodyRadius { get; protected set; }

    /// Collision radius against towers and the base.
    public float Radius => Config.monsterWallRadius;

    /// Flow-field distance to the base from this enemy's cell (lower = further ahead).
    public float FlowDistance => GameManager.Instance.Flow.DistanceAt(GameManager.Instance.Grid.WorldToCell(Position));

    [Tooltip("Gem dropped on death. Leave empty for monsters that give no exp.")]
    [SerializeField] private ExpGem gemPrefab;

    // Fixed per monster, so monsters walking the same cells do not stack.
    private Vector2 cellOffset;

    /// Fraction of the enemy slow passive this monster receives (the boss overrides this).
    protected virtual float SlowFactor => 1f;

    /// Called by EnemySpawner right after Instantiate. HP and damage are already scaled.
    public void Init(float maxHP, float damage, float speed, int expReward, float bodyRadius, Vector2 cellOffset)
    {
        RestoreFull(maxHP);
        Damage = damage;
        Speed = speed;
        ExpReward = expReward;
        BodyRadius = bodyRadius;
        this.cellOffset = cellOffset;
    }

    protected virtual void OnEnable() => active.Add(this);
    protected virtual void OnDisable() => active.Remove(this);

    private void Update()
    {
        if (!IsSurvivalPhase || !IsAlive()) return;
        Tick(Time.deltaTime);
    }

    /// Default behaviour: follow the flow field, attacking whatever blocks the next cell.
    protected virtual void Tick(float dt) => Move(GameManager.Instance.Flow, dt);

    /// Steps toward the next flow-field cell. If that cell is a tower or the base, walks up to it and
    /// calls Attack(target) once touching it. Slowed by the enemy slow passive.
    public void Move(FlowField field, float dt)
    {
        GridMap grid = GameManager.Instance.Grid;
        Vector2Int cell = grid.ClampCell(grid.WorldToCell(Position));
        Vector2Int next = field.NextStep(cell);

        Unit blocker = grid.BlockerAt(next);
        if (blocker != null && GridMap.DistanceToCell(Position, next) <= Radius + Config.monsterContactSlack)
        {
            Attack(blocker);
            return;
        }

        Vector2 target = grid.CellCenter(next) + (blocker == null ? cellOffset : Vector2.zero);
        float slow = GameManager.Instance.Upgrades.GetModifier(UpgradeType.EnemySlow) * SlowFactor;
        Vector2 pos = Vector2.MoveTowards(Position, target, Speed * (1f - slow) * dt);
        transform.position = grid.ResolveCollision(pos, Radius);
    }

    /// Hits a tower or the base that blocks the way.
    public abstract void Attack(Unit target);

    public override void Die()
    {
        base.Die();
        if (ExpReward > 0 && gemPrefab != null)
            Instantiate(gemPrefab, transform.position, Quaternion.identity).Init(ExpReward);
        Destroy(gameObject);
    }

    /// The closest living enemy within range of a point, or null.
    public static Enemy FindNearest(Vector2 from, float range)
    {
        Enemy best = null;
        float bestDist = range;
        foreach (Enemy e in active)
        {
            if (!e.IsAlive()) continue;
            float d = Vector2.Distance(from, e.Position);
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }
}
