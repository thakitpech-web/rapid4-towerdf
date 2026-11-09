using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Melee,
    Ranged,
    Boss
}

/// <summary>A monster walking the path toward the base.</summary>
public abstract class Enemy : Unit
{
    private static readonly List<Enemy> active = new List<Enemy>();

    /// <summary>Every living enemy in the scene.</summary>
    public static IReadOnlyList<Enemy> Active => active;

    public float Speed { get; protected set; }
    public float PathProgress { get; protected set; }
    public float Damage { get; protected set; }
    public int ExpReward { get; protected set; }
    public float BodyRadius { get; protected set; }

    protected Path path;
    private float laneOffset;

    /// <summary>Called by EnemySpawner right after Instantiate. HP and damage are already scaled.</summary>
    public void Init(Path path, float maxHP, float damage, float speed, int expReward, float bodyRadius,
                     float laneOffset, float startProgress = 0f)
    {
        this.path = path;
        RestoreFull(maxHP);
        Damage = damage;
        Speed = speed;
        ExpReward = expReward;
        BodyRadius = bodyRadius;
        this.laneOffset = laneOffset;
        PathProgress = startProgress;
        PlaceOnPath();
    }

    protected virtual void OnEnable() => active.Add(this);
    protected virtual void OnDisable() => active.Remove(this);

    private void Update()
    {
        if (!IsSurvivalPhase || !IsAlive() || path == null) return;
        Tick(Time.deltaTime);
    }

    /// <summary>Default behaviour: walk the path, then attack the base once at the end.</summary>
    protected virtual void Tick(float dt)
    {
        if (path.IsEnd(PathProgress)) Attack();
        else FollowPath(path, dt);
    }

    public void FollowPath(Path path, float dt)
    {
        PathProgress += Speed * dt;   // enemy slow passive is added in step 4
        PlaceOnPath();
    }

    public abstract void Attack();

    public override void Die()
    {
        base.Die();                   // exp gem drop is added in step 4
        Destroy(gameObject);
    }

    private void PlaceOnPath()
    {
        Vector2 dir = path.DirectionAt(PathProgress);
        Vector2 normal = new Vector2(-dir.y, dir.x);
        transform.position = path.PositionAt(PathProgress) + normal * laneOffset;
    }

    /// <summary>The closest living enemy within range of a point, or null.</summary>
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
