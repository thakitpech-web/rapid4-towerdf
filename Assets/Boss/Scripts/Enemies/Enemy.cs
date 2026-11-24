using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Melee,
    Ranged,
    Boss
}

/// A monster (GAME_DESIGN.md section 5). Convoy monsters walk their lane cell by cell, then into the base;
/// they pass through nothing solid because towers never stand on lanes. Swarm monsters move straight at the
/// character and slide along towers. Any monster that touches the base deals BaseDamage once and disappears
/// without dropping exp.
public abstract class Enemy : Unit
{
    private static readonly List<Enemy> active = new List<Enemy>();

    /// Every living enemy in the scene.
    public static IReadOnlyList<Enemy> Active => active;

    [Tooltip("Gem dropped on death. Leave empty for monsters that give no exp.")]
    [SerializeField] private ExpGem gemPrefab;
    [Tooltip("Animator on the monster or its sprite child. Needs Float parameters MoveX, MoveY and Speed.")]
    [SerializeField] private Animator animator;

    // Animator parameters: walk direction (kept while standing still) and speed as a fraction of full speed
    // (0 = stopped, so the walk clip freezes when Speed drives the state's speed multiplier).
    private static readonly int MoveXParam = Animator.StringToHash("MoveX");
    private static readonly int MoveYParam = Animator.StringToHash("MoveY");
    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    public EnemyGroup Group { get; private set; }

    /// Convoy only: the lane being walked and the index of the next centre-line point to walk to.
    public Lane Lane { get; private set; }
    public int LaneIndex { get; private set; }

    public float Speed { get; protected set; }
    public float Damage { get; protected set; }
    public int ExpReward { get; protected set; }

    /// Base HP lost when this monster walks into the base.
    public float BaseDamage { get; protected set; }

    /// Radius used for projectile hits and touching the character.
    public float BodyRadius { get; protected set; }

    /// Collision radius against towers and the base.
    public float Radius => Config.monsterWallRadius;

    /// Convoy: tiles still to walk along the lane (Bolt towers shoot the lowest first). Swarm: infinity.
    public float LaneDistanceLeft =>
        Group == EnemyGroup.Convoy && Lane != null ? Lane.DistanceLeft(Position, LaneIndex) : float.PositiveInfinity;

    // Fixed per monster, so convoy monsters on the same lane do not stack.
    private Vector2 laneOffset;

    /// Fraction of the enemy slow passive this monster receives (the boss overrides this).
    protected virtual float SlowFactor => 1f;

    protected static GridMap Grid => GameManager.Instance.Grid;

    /// True while within touching distance of the base's square.
    protected bool TouchingBase => Grid.DistanceToBase(Position) <= Radius + Config.monsterContactSlack;

    /// Called by EnemySpawner right after Instantiate. HP and damage are already scaled.
    /// <paramref name="lane"/> is null for swarm monsters.
    public void Init(float maxHP, float damage, float baseDamage, float speed, int expReward, float bodyRadius,
                     EnemyGroup group, Lane lane, Vector2 laneOffset)
    {
        RestoreFull(maxHP);
        Damage = damage;
        BaseDamage = baseDamage;
        Speed = speed;
        ExpReward = expReward;
        BodyRadius = bodyRadius;
        Group = group;
        Lane = lane;
        LaneIndex = 0;
        this.laneOffset = laneOffset;
    }

    protected virtual void OnEnable() => active.Add(this);
    protected virtual void OnDisable() => active.Remove(this);

    private void Update()
    {
        if (!IsSurvivalPhase || !IsAlive())
        {
            Animate(Vector2.zero, 0f);
            return;
        }

        Vector2 before = Position;
        Tick(Time.deltaTime);
        Animate(Position - before, Time.deltaTime);
    }

    // Feeds the Animator from how far the monster actually moved this frame, so lane walking, chasing,
    // sliding along towers and stopping to shoot all animate without each behaviour reporting it.
    private void Animate(Vector2 moved, float dt)
    {
        if (animator == null || !animator.isActiveAndEnabled) return;

        float full = Speed * dt;
        float speed01 = full > 0f ? Mathf.Clamp01(moved.magnitude / full) : 0f;
        animator.SetFloat(SpeedParam, speed01);
        if (moved.sqrMagnitude > 1e-8f)
        {
            Vector2 dir = moved.normalized;
            animator.SetFloat(MoveXParam, dir.x);
            animator.SetFloat(MoveYParam, dir.y);
        }
    }

    /// Default behaviour: move for this monster's group.
    protected virtual void Tick(float dt) => Move(dt);

    public virtual void Move(float dt)
    {
        if (Group == EnemyGroup.Convoy) FollowLane(dt);
        else ChasePlayer(dt);
    }

    /// Walks the lane's centre line point by point (plus this monster's offset). After the last point,
    /// walks into the base and enters it once touching.
    public void FollowLane(float dt)
    {
        if (Lane == null) return;
        float step = CurrentSpeed * dt;

        while (LaneIndex < Lane.Points.Length && step > 0f)
        {
            Vector2 target = Lane.Points[LaneIndex] + laneOffset;
            float dist = Vector2.Distance(Position, target);
            if (dist > step)
            {
                transform.position = Vector2.MoveTowards(Position, target, step);
                return;
            }
            transform.position = target;
            step -= dist;
            LaneIndex++;
        }

        if (LaneIndex < Lane.Points.Length) return;

        if (TouchingBase)
        {
            EnterBase();
            return;
        }
        Vector2 pos = Vector2.MoveTowards(Position, Grid.BaseCenter, step);
        transform.position = Grid.ResolveCollision(pos, Radius);
    }

    /// Moves straight at the character, sliding along towers. Enters the base if it touches it.
    public void ChasePlayer(float dt)
    {
        if (TouchingBase)
        {
            EnterBase();
            return;
        }

        PlayerCharacter player = GameManager.Instance.Player;
        if (player == null || !player.IsAlive()) return;

        Vector2 pos = Vector2.MoveTowards(Position, player.Position, CurrentSpeed * dt);
        transform.position = Grid.ResolveCollision(pos, Radius);
    }

    /// Speed after the enemy slow passive.
    protected float CurrentSpeed =>
        Speed * (1f - GameManager.Instance.Upgrades.GetModifier(UpgradeType.EnemySlow) * SlowFactor);

    /// Hits a tower or the character.
    public abstract void Attack(Unit target);

    /// Deals BaseDamage to the base once and disappears (no exp gem).
    protected void EnterBase()
    {
        Base b = GameManager.Instance.Base;
        if (b != null && b.IsAlive()) b.TakeDamage(BaseDamage);
        Destroy(gameObject);
    }

    /// Sound names in the SoundLibrary for being hit and dying (null = silent).
    protected virtual string HitSound => null;
    protected virtual string DeathSound => null;

    public override void TakeDamage(float amount)
    {
        if (amount <= 0f || !IsAlive()) return;
        base.TakeDamage(amount);
        if (IsAlive()) SoundManager.Play(HitSound);    // the killing hit plays DeathSound instead
    }

    public override void Die()
    {
        SoundManager.Play(DeathSound);
        base.Die();
        if (ExpReward > 0 && gemPrefab != null)
            Instantiate(gemPrefab, transform.position, Quaternion.identity).Init(ExpReward);
        Destroy(gameObject);
    }

    /// The closest living enemy within range of a point, or null.
    public static Enemy FindNearest(Vector2 from, float range) => FindNearest(from, range, null);

    /// The closest living enemy of a group (or any group when null) within range of a point, or null.
    public static Enemy FindNearest(Vector2 from, float range, EnemyGroup? group)
    {
        Enemy best = null;
        float bestDist = range;
        foreach (Enemy e in active)
        {
            if (!e.IsAlive() || (group.HasValue && e.Group != group.Value)) continue;
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
