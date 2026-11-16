using UnityEngine;

/// Fallback card, offered when fewer cards than needed remain in the pool. Always available.
[CreateAssetMenu(fileName = "Fallback_HealCharacter", menuName = "One Minute Siege/Fallback Heal Upgrade")]
public class HealCharacterUpgrade : Upgrade
{
    [Tooltip("Fraction of the character's max HP to heal")]
    public float HealFraction = 0.3f;

    public override string Tag => "Fallback";

    public override bool IsAvailable() => true;

    public override void Apply()
    {
        PlayerCharacter player = GameManager.Instance.Player;
        player.Heal(player.MaxHP * HealFraction);
    }
}
