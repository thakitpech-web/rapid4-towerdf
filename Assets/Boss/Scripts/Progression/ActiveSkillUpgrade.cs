using UnityEngine;

public enum SkillType
{
    RapidFire,
    BaseInvincible
}

/// Level-up card that unlocks an active skill (GAME_DESIGN.md section 9). Leaves the pool once taken.
/// Holds the skill's numbers; the skill itself is added to the character as an ActiveSkill component.
[CreateAssetMenu(fileName = "ActiveSkill_", menuName = "One Minute Siege/Active Skill Upgrade")]
public class ActiveSkillUpgrade : Upgrade
{
    public SkillType Skill;
    [Tooltip("Keyboard key name from UnityEngine.InputSystem.Key, e.g. Q or E")]
    public string Key = "Q";
    [Tooltip("Seconds the effect lasts")] public float Duration = 5f;
    [Tooltip("Seconds from activation until it can be used again")] public float Cooldown = 15f;
    [Tooltip("Rapid Fire: character attack speed multiplier")] public float EffectMultiplier = 2f;
    [Tooltip("Optional look shown while the skill runs (on the character for Rapid Fire, on the base for Base Invincible). Empty = a placeholder glow.")]
    public GameObject EffectPrefab;

    public override string Tag => $"Active skill · {Key}";

    public override bool IsAvailable() => !GameManager.Instance.Player.HasSkill(Skill);

    public override void Apply() => GameManager.Instance.Player.AddSkill(this);
}
