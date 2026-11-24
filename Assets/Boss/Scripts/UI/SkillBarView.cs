using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Active skill slots during survival: key, name or seconds left, a cooldown overlay that shrinks as the
/// skill recharges, and a frame while the effect is running. Slot i shows the i-th skill the character
/// unlocked; slots without a skill stay hidden.
/// Setup: put this on a UI object that stays active and build one child object per slot.
public class SkillBarView : MonoBehaviour
{
    [Serializable]
    private class Slot
    {
        [Tooltip("The slot object; hidden until a skill fills it")] public GameObject root;
        [Tooltip("Optional: shows the skill card's Icon")] public Image icon;
        public TMP_Text keyText;
        [Tooltip("Skill name when ready, seconds left otherwise")] public TMP_Text infoText;
        [Tooltip("Image Type = Filled (e.g. Vertical, origin Bottom); full right after use")] public Image cooldownFill;
        [Tooltip("Shown while the effect is running")] public GameObject activeFrame;
    }

    [SerializeField] private Slot[] slots = new Slot[2];

    private void LateUpdate()
    {
        GameManager gm = GameManager.Instance;
        bool playing = gm != null && (gm.Phase == GamePhase.Survival || gm.Phase == GamePhase.LevelUp);

        for (int i = 0; i < slots.Length; i++)
        {
            Slot s = slots[i];
            if (s == null || s.root == null) continue;

            ActiveSkill skill = gm != null && i < gm.Player.Skills.Count ? gm.Player.Skills[i] : null;
            bool show = playing && skill != null;
            s.root.SetActive(show);
            if (!show) continue;

            if (s.icon != null)
            {
                s.icon.sprite = skill.Data.Icon;
                s.icon.enabled = skill.Data.Icon != null;
            }
            if (s.keyText != null) s.keyText.text = skill.Data.Key;
            if (s.activeFrame != null) s.activeFrame.SetActive(skill.IsActive);
            if (s.cooldownFill != null)
                s.cooldownFill.fillAmount = skill.Cooldown > 0f ? skill.CooldownLeft / skill.Cooldown : 0f;
            if (s.infoText != null)
                s.infoText.text = skill.IsActive ? $"{skill.ActiveLeft:0.0}s"
                                : skill.CooldownLeft > 0f ? $"{Mathf.CeilToInt(skill.CooldownLeft)}s"
                                : skill.Data.Name;
        }
    }
}
