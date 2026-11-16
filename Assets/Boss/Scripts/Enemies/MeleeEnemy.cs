using UnityEngine;

/// Follows the flow field. If the next cell is a tower or the base, stops and hits it (Damage per second)
/// until it breaks. Damages the character on contact (Damage per second).
public class MeleeEnemy : Enemy
{
    protected override void Tick(float dt)
    {
        base.Tick(dt);
        HurtPlayerOnContact(dt);
    }

    public override void Attack(Unit target)
    {
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
