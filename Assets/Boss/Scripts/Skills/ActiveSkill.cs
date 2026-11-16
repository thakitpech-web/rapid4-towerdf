using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// An active skill on the character: press its key to run the effect for Duration seconds,
/// then wait out the Cooldown (counted from activation). Only ticks during survival, so level-up
/// pauses it. Ready again at the start of every loop.
public abstract class ActiveSkill : MonoBehaviour
{
    public ActiveSkillUpgrade Data { get; private set; }

    public float Duration => Data.Duration;
    public float Cooldown => Data.Cooldown;
    public float CooldownLeft { get; private set; }
    public float ActiveLeft { get; private set; }
    public bool IsActive { get; private set; }

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
        if (!CanUse()) return;
        IsActive = true;
        ActiveLeft = Duration;
        CooldownLeft = Cooldown;
        OnStart();
    }

    /// Loop start: effect off, ready to use.
    public void ResetForLoop()
    {
        if (IsActive) OnEnd();
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
        CooldownLeft = Mathf.Max(0f, CooldownLeft - dt);

        if (IsActive)
        {
            ActiveLeft -= dt;
            if (ActiveLeft <= 0f)
            {
                IsActive = false;
                ActiveLeft = 0f;
                OnEnd();
            }
        }

        Keyboard kb = Keyboard.current;
        if (kb != null && key != Key.None && kb[key].wasPressedThisFrame) Activate();
    }
}
