using UnityEngine;

/// <summary>
/// Walks the path. Damages the character on contact (Damage per second).
/// At the end of the path it hits the base continuously (Damage per second).
/// </summary>
public class MeleeEnemy : Enemy
{
    protected override void Tick(float dt)
    {
        base.Tick(dt);
        HurtPlayerOnContact(dt);
    }

    public override void Attack()
    {
        Base target = GameManager.Instance.Base;
        if (target != null && target.IsAlive()) target.TakeDamage(Damage * Time.deltaTime);
    }

    private void HurtPlayerOnContact(float dt)
    {
        PlayerCharacter player = GameManager.Instance.Player;
        if (player == null || !player.IsAlive()) return;

        if (Vector2.Distance(Position, player.Position) < BodyRadius + player.BodyRadius)
            player.TakeDamage(Damage * dt);
    }
}
