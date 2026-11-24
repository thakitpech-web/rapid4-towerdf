using UnityEngine;

/// E: the base takes no damage for Duration seconds.
/// Shows a blue dome over the base while it runs; each blocked hit flashes the dome and plays a sound.
public class BaseInvincibleSkill : ActiveSkill
{
    protected override Transform EffectTarget => GameManager.Instance.Base.transform;
    protected override Color EffectColor => new Color(0.45f, 0.8f, 1f);
    protected override float EffectDiameter => 3.2f;
    protected override string StartSound => "Skill_Shield";

    protected override void OnStart()
    {
        Base b = GameManager.Instance.Base;
        b.IsInvincible = true;
        b.Blocked += OnBlocked;
    }

    protected override void OnEnd()
    {
        Base b = GameManager.Instance.Base;
        b.IsInvincible = false;
        b.Blocked -= OnBlocked;
    }

    private void OnBlocked()
    {
        SoundManager.Play("Skill_Block");
        if (Effect != null) Effect.SendMessage("Flash", SendMessageOptions.DontRequireReceiver);
    }
}
