using System;

/// <summary>
/// Counts one survival loop. GameManager owns it and calls Tick only during the Survival phase,
/// so the timer stops during level-up and build.
/// </summary>
public class LoopTimer
{
    public float Duration { get; }
    public float Elapsed { get; private set; }
    public bool IsFinished { get; private set; }

    public event Action OnTimeUp;

    public LoopTimer(float duration)
    {
        Duration = duration;
    }

    public void Tick(float dt)
    {
        if (IsFinished) return;

        Elapsed += dt;
        if (Elapsed >= Duration)
        {
            Elapsed = Duration;
            IsFinished = true;
            OnTimeUp?.Invoke();
        }
    }

    public float TimeLeft() => Duration - Elapsed;

    public void Reset()
    {
        Elapsed = 0f;
        IsFinished = false;
    }
}
