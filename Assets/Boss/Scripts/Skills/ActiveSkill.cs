using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// An active skill on the character: press its key to run the effect for Duration seconds,
/// then wait out the Cooldown (counted from activation). Only ticks during survival, so level-up
/// pauses it. Ready again at the start of every loop.
/// Feedback: a sound on start, end, ready again and on a press that is not ready yet, and a look shown on
/// EffectTarget while it runs (the card's Effect Prefab, or a placeholder SkillAura).
public abstract class ActiveSkill : MonoBehaviour
{
    public ActiveSkillUpgrade Data { get; private set; }

    public float Duration => Data.Duration;
    public float Cooldown => Data.Cooldown;
    public float CooldownLeft { get; private set; }
    public float ActiveLeft { get; private set; }
    public bool IsActive { get; private set; }

    /// The look shown while the skill runs (null when not running).
    protected GameObject Effect { get; private set; }

    /// Where the look is shown, its placeholder colour and size (world units across), and the start sound.
    protected abstract Transform EffectTarget { get; }
    protected abstract Color EffectColor { get; }
    protected abstract float EffectDiameter { get; }
    protected abstract string StartSound { get; }
    protected virtual int EffectSortingOrder => 4;

    private Key key;

    public void Init(ActiveSkillUpgrade data)
    {
        Data = data;
        if (!Enum.TryParse(data.Key, true, out key))
            Debug.LogWarning($"{data.name}: unknown key '{data.Key}'", data);
    }

    public bool CanUse() => !IsActive && CooldownLeft <= 0f;

    public void Activate()
    {
        if (!CanUse())
        {
            SoundManager.Play("Skill_NotReady");
            return;
        }
        IsActive = true;
        ActiveLeft = Duration;
        CooldownLeft = Cooldown;
        OnStart();
        ShowEffect();
        SoundManager.Play(StartSound);
    }

    /// Loop start: effect off, ready to use.
    public void ResetForLoop()
    {
        if (IsActive) OnEnd();
        HideEffect();
        IsActive = false;
        ActiveLeft = 0f;
        CooldownLeft = 0f;
    }

    protected abstract void OnStart();
    protected abstract void OnEnd();

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Phase != GamePhase.Survival || Data == null) return;

        float dt = Time.deltaTime;
        bool wasCooling = CooldownLeft > 0f;
        CooldownLeft = Mathf.Max(0f, CooldownLeft - dt);
        if (wasCooling && CooldownLeft <= 0f) SoundManager.Play("Skill_Ready");

        if (IsActive)
        {
            ActiveLeft -= dt;
            if (ActiveLeft <= 0f)
            {
                IsActive = false;
                ActiveLeft = 0f;
                OnEnd();
                HideEffect();
                SoundManager.Play("Skill_End");
            }
        }

        Keyboard kb = Keyboard.current;
        if (kb != null && !PauseMenu.IsPaused && key != Key.None && kb[key].wasPressedThisFrame) Activate();
    }

    private void ShowEffect()
    {
        HideEffect();
        Transform target = EffectTarget;
        if (target == null) return;

        Effect = Data.EffectPrefab != null
            ? Instantiate(Data.EffectPrefab, target)
            : SkillAura.Create(target, EffectColor, EffectDiameter, EffectSortingOrder);
    }

    private void HideEffect()
    {
        if (Effect != null) Destroy(Effect);
        Effect = null;
    }

    private void OnDestroy() => HideEffect();
}
