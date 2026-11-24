using UnityEngine;

/// Damages the character on contact (Damage per second) in both groups. Walking into the base is handled
/// by Enemy.EnterBase.
public class MeleeEnemy : Enemy
{
    protected override string HitSound => "Mon_MeleeGotHit";
    protected override string DeathSound => "Mon_MeleeDeath";

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
