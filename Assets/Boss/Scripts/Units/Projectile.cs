using System.Collections.Generic;
using UnityEngine;

/// A shot. Two kinds:
/// - Homing (Launch): follows one unit and always hits it unless it dies first. Used by the character,
///   the base and Bolt towers against monsters.
/// - Straight (LaunchStraight): flies in a fixed direction and hits the first character, tower or base it
///   touches, so it can be dodged. Used by ranged monsters. Vanishes after its max distance.
/// Damage is dealt on impact.
public class Projectile : MonoBehaviour
{
    private static readonly List<Projectile> active = new List<Projectile>();

    [Tooltip("Turn the shot to face where it flies")]
    [SerializeField] private bool faceDirection;
    [Tooltip("Direction the sprite points when not rotated, in degrees (0 = right, 90 = up)")]
    [SerializeField] private float spriteAngle;
    [Tooltip("Straight shots: hit radius against the character")]
    [SerializeField] private float radius = 0.1f;
    [Tooltip("Optional effect spawned where the shot hits")]
    [SerializeField] private TimedEffect hitEffect;
    [Tooltip("Radius passed to the hit effect (used when its Scale To Radius is on)")]
    [SerializeField] private float hitEffectRadius = 0.2f;

    private bool straight;
    private Unit target;
    private Vector2 direction;
    private float damage, speed, distanceLeft;

    /// Homing shot at one unit.
    public void Launch(Unit target, float damage, float speed)
    {
        straight = false;
        this.target = target;
        this.damage = damage;
        this.speed = speed;
    }

    /// Straight shot toward a point; flies up to maxDistance.
    public void LaunchStraight(Vector2 toward, float damage, float speed, float maxDistance)
    {
        straight = true;
        Vector2 d = toward - (Vector2)transform.position;
        direction = d.sqrMagnitude > 1e-6f ? d.normalized : Vector2.right;
        this.damage = damage;
        this.speed = speed;
        distanceLeft = maxDistance;
        Face(direction);
    }

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    /// Removes every shot from the map (loop reset).
    public static void ClearAll()
    {
        foreach (Projectile p in new List<Projectile>(active))
            Destroy(p.gameObject);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Survival) return;

        if (straight) FlyStraight(speed * Time.deltaTime);
        else FlyHoming(speed * Time.deltaTime);
    }

    private void FlyHoming(float step)
    {
        if (target == null || !target.IsAlive())
        {
            Destroy(gameObject);
            return;
        }

        Vector2 toTarget = target.Position - (Vector2)transform.position;
        float hitDistance = target is Enemy e ? e.BodyRadius * 0.5f : 0.1f;
        if (toTarget.magnitude <= step + hitDistance)
        {
            Hit(target);
            return;
        }

        transform.position += (Vector3)(toTarget.normalized * step);
        Face(toTarget);
    }

    private void FlyStraight(float step)
    {
        // Small sub-steps so a fast shot cannot skip over a target.
        const float maxSub = 0.1f;
        GameManager gm = GameManager.Instance;
        GridMap grid = gm.Grid;

        while (step > 0f)
        {
            float s = Mathf.Min(step, maxSub, distanceLeft);
            step -= s;
            distanceLeft -= s;
            transform.position += (Vector3)(direction * s);
            Vector2 pos = transform.position;

            PlayerCharacter player = gm.Player;
            if (player != null && player.IsAlive() && Vector2.Distance(pos, player.Position) <= player.BodyRadius + radius)
            {
                Hit(player);
                return;
            }

            Vector2Int cell = grid.WorldToCell(pos);
            if (grid.IsBase(cell))
            {
                Hit(gm.Base);
                return;
            }
            Tower tower = grid.LiveTowerAt(cell);
            if (tower != null)
            {
                Hit(tower);
                return;
            }

            if (distanceLeft <= 0f || !grid.InBounds(cell))
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    private void Hit(Unit u)
    {
        u.TakeDamage(damage);
        TimedEffect.Spawn(hitEffect, transform.position, hitEffectRadius);
        Destroy(gameObject);
    }

    private void Face(Vector2 dir)
    {
        if (faceDirection && dir.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - spriteAngle);
    }
}
