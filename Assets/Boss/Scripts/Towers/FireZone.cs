using System.Collections.Generic;
using UnityEngine;

/// AoE tier-3 fire: burns every enemy inside the radius for damage per second until it runs out.
/// Zones do not stack: an enemy inside several zones takes the damage once.
/// Created by AoETower; its look is the optional effect prefab, sized to the radius and removed with the zone.
public class FireZone : MonoBehaviour
{
    private static readonly List<FireZone> active = new List<FireZone>();
    private static readonly HashSet<Enemy> burnedThisFrame = new HashSet<Enemy>();
    private static readonly List<Enemy> targets = new List<Enemy>();
    private static int burnFrame = -1;

    private float radius, damagePerSecond, timeLeft;

    public static FireZone Spawn(Vector2 at, float radius, float damagePerSecond, float duration, GameObject effectPrefab)
    {
        var go = new GameObject("FireZone");
        go.transform.position = at;
        FireZone zone = go.AddComponent<FireZone>();
        zone.radius = radius;
        zone.damagePerSecond = damagePerSecond;
        zone.timeLeft = duration;

        if (effectPrefab != null)
        {
            GameObject fx = Instantiate(effectPrefab, go.transform);
            fx.transform.localPosition = Vector3.zero;
            Vector3 s = effectPrefab.transform.localScale;
            fx.transform.localScale = new Vector3(s.x * radius * 2f, s.y * radius * 2f, s.z);
        }
        return zone;
    }

    /// Removes every zone (loop reset).
    public static void ClearAll()
    {
        foreach (FireZone z in new List<FireZone>(active)) Destroy(z.gameObject);
    }

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Phase != GamePhase.Survival) return;

        float dt = Time.deltaTime;
        timeLeft -= dt;

        if (burnFrame != Time.frameCount)
        {
            burnFrame = Time.frameCount;
            burnedThisFrame.Clear();
        }

        targets.Clear();
        foreach (Enemy e in Enemy.Active)
            if (e.IsAlive() && !burnedThisFrame.Contains(e) && Vector2.Distance(transform.position, e.Position) <= radius)
                targets.Add(e);
        foreach (Enemy e in targets)
        {
            burnedThisFrame.Add(e);
            e.TakeDamage(damagePerSecond * dt);   // may destroy the enemy, so iterate a copy
        }

        if (timeLeft <= 0f) Destroy(gameObject);
    }
}
