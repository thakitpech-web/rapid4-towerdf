using UnityEngine;

/// Hits the base for Damage per second while touching it (convoy at the end of its lane, swarm whenever it
/// touches the base). Damages the character on contact (Damage per second) in both groups.
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
