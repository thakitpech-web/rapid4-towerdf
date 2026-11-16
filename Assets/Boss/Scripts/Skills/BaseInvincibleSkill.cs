/// E: the base takes no damage for Duration seconds.
public class BaseInvincibleSkill : ActiveSkill
{
    protected override void OnStart() => GameManager.Instance.Base.IsInvincible = true;
    protected override void OnEnd() => GameManager.Instance.Base.IsInvincible = false;
}
