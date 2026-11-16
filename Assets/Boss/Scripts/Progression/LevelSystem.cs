using System;
using UnityEngine;

/// Level and exp. Never resets between loops. Levels gained in a loop become orbs.
public class LevelSystem : MonoBehaviour
{
    public int Level { get; private set; } = 1;
    public int Exp { get; private set; }
    public int LevelsThisLoop { get; private set; }
    public int PendingLevelUps { get; private set; }

    /// Raised once per level gained.
    public event Action OnLevelUp;

    public void AddExp(int amount)
    {
        Exp += amount;
        while (Exp >= ExpToNext())
        {
            Exp -= ExpToNext();
            Level++;
            LevelsThisLoop++;
            PendingLevelUps++;
            OnLevelUp?.Invoke();
        }
    }

    /// Exp needed to reach the next level.
    public int ExpToNext() => GameManager.Instance.Config.ExpToReach(Level + 1);

    /// Called after the player picked a card for one pending level-up.
    public void ResolveLevelUp()
    {
        if (PendingLevelUps > 0) PendingLevelUps--;
    }

    /// Levels gained this loop, then resets the count for the next loop.
    public int ConvertToOrbs()
    {
        int orbs = LevelsThisLoop;
        LevelsThisLoop = 0;
        return orbs;
    }
}
