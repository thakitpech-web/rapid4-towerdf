using System;
using UnityEngine;

/// <summary>Anything with HP: player, base, monsters, towers.</summary>
public abstract class Unit : MonoBehaviour
{
    // Serialized only so they can be watched in the Inspector while playing.
    [Header("Runtime (read only)")]
    [SerializeField] protected float hp;
    [SerializeField] protected float maxHP;
    [SerializeField] protected float shield;

    public float HP => hp;
    public float MaxHP => maxHP;
    public float Shield => shield;
    public Vector2 Position => transform.position;

    public event Action<Unit> OnDied;

    public bool IsAlive() => hp > 0f;

    public virtual void TakeDamage(float amount)
    {
        if (!IsAlive() || amount <= 0f) return;

        float absorbed = Mathf.Min(shield, amount);
        shield -= absorbed;
        hp -= amount - absorbed;

        if (hp <= 0f)
        {
            hp = 0f;
            Die();
        }
    }

    public virtual void Die()
    {
        OnDied?.Invoke(this);
    }

    /// <summary>Full HP, no shield.</summary>
    protected void RestoreFull(float newMaxHP)
    {
        maxHP = newMaxHP;
        hp = maxHP;
        shield = 0f;
    }

    protected static bool IsSurvivalPhase =>
        GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Survival;

    protected static GameConfig Config => GameManager.Instance.Config;
}
