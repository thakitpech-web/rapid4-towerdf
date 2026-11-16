using System.Collections.Generic;
using UnityEngine;

/// Rolls level-up cards and keeps passive stacks. Other systems read passives with GetModifier.
public class UpgradeManager : MonoBehaviour
{
    [Tooltip("Every card that can be rolled: the 5 passives and the 2 active skills")]
    [SerializeField] private List<Upgrade> pool = new List<Upgrade>();
    [Tooltip("Added when fewer cards than needed remain")]
    [SerializeField] private Upgrade fallback;

    private readonly Dictionary<UpgradeType, PassiveUpgrade> passives = new Dictionary<UpgradeType, PassiveUpgrade>();

    public List<Upgrade> Pool => pool;
    public Dictionary<UpgradeType, int> Stacks { get; } = new Dictionary<UpgradeType, int>();

    private void Awake()
    {
        foreach (Upgrade u in pool)
            if (u is PassiveUpgrade p) passives[p.Type] = p;
    }

    /// Up to <paramref name="count"/> different available cards, plus the fallback if short.
    public List<Upgrade> RollChoices(int count)
    {
        List<Upgrade> available = new List<Upgrade>();
        foreach (Upgrade u in pool)
            if (u.IsAvailable()) available.Add(u);

        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }

        List<Upgrade> choices = available.GetRange(0, Mathf.Min(count, available.Count));
        if (choices.Count < count && fallback != null) choices.Add(fallback);
        return choices;
    }

    public void Apply(Upgrade u) => u.Apply();

    public int GetStacks(UpgradeType t) => Stacks.TryGetValue(t, out int n) ? n : 0;

    public void AddStack(UpgradeType t) => Stacks[t] = GetStacks(t) + 1;

    /// Total bonus of a passive, e.g. 0.3 for three +10% picks. 0 if never picked.
    public float GetModifier(UpgradeType t)
    {
        return passives.TryGetValue(t, out PassiveUpgrade p) ? GetStacks(t) * p.ValuePerStack : 0f;
    }
}
