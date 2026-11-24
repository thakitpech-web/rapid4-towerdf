using UnityEngine;

/// Q: character attack speed × EffectMultiplier for Duration seconds (character only).
/// Shows an orange glow under the character while it runs.
public class RapidFireSkill : ActiveSkill
{
    protected override Transform EffectTarget => GameManager.Instance.Player.transform;
    protected override Color EffectColor => new Color(1f, 0.6f, 0.15f);
    protected override float EffectDiameter => 1.2f;
    protected override string StartSound => "Skill_RapidFire";

    protected override void OnStart() => GameManager.Instance.Player.AttackRateMultiplier = Data.EffectMultiplier;
    protected override void OnEnd() => GameManager.Instance.Player.AttackRateMultiplier = 1f;
}
