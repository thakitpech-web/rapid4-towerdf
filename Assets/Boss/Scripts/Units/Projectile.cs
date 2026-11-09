using System.Collections.Generic;
using UnityEngine;

/// <summary>A homing shot that hits one enemy. Used by the character and the base.</summary>
public class Projectile : MonoBehaviour
{
    private static readonly List<Projectile> active = new List<Projectile>();

    private Enemy target;
    private float damage;
    private float speed;

    public void Launch(Enemy target, float damage, float speed)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
    }

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    /// <summary>Removes every shot from the map (loop reset).</summary>
    public static void ClearAll()
    {
        foreach (Projectile p in new List<Projectile>(active))
            Destroy(p.gameObject);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Survival) return;

        if (target == null || !target.IsAlive())
        {
            Destroy(gameObject);
            return;
        }

        Vector2 toTarget = target.Position - (Vector2)transform.position;
        float step = speed * Time.deltaTime;

        if (toTarget.magnitude <= step + target.BodyRadius * 0.5f)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(toTarget.normalized * step);
    }
}
