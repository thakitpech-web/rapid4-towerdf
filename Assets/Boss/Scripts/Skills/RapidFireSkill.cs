/// Q: character attack speed × EffectMultiplier for Duration seconds (character only).
public class RapidFireSkill : ActiveSkill
{
    protected override void OnStart() => GameManager.Instance.Player.AttackRateMultiplier = Data.EffectMultiplier;
    protected override void OnEnd() => GameManager.Instance.Player.AttackRateMultiplier = 1f;
}
