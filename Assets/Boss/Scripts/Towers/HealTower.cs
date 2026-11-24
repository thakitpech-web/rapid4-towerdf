using System.Collections.Generic;
using UnityEngine;

/// Heals the lowest-%-HP targets in range: the base or other towers, never the character.
/// Heals in pulses: TowerData.Rate pulses per second, each worth Damage (heal per second) × the pulse
/// interval, so the heal per second stays the same. A pulse waits until something needs healing.
/// Rate 0 = heal a little every frame instead.
/// Tier 3 overheal shield: healing beyond full HP becomes shield, up to TowerData.ShieldCap × max HP,
/// so full-HP targets whose shield is not full are still healed.
public class HealTower : Tower
{
    public bool OverhealShield => Data.HasSpecial(Tier);

    [Header("Range circle (survival only)")]
    [Tooltip("Show the heal range during survival: faint while idle, flashes bright on each heal pulse")]
    [SerializeField] private bool showRange = true;
    [SerializeField] private Color rangeColor = new Color(0.35f, 0.9f, 0.45f);
    [Tooltip("Ring opacity while nothing needs healing")]
    [SerializeField] private float idleAlpha = 0.15f;
    [Tooltip("Ring opacity right after a heal pulse")]
    [SerializeField] private float healingAlpha = 0.75f;
    [Tooltip("Seconds the ring takes to fade back after a heal pulse")]
    [SerializeField] private float flashFade = 0.6f;
    [Tooltip("Sorting order of the circle (towers are 3, so 2 draws it under them)")]
    [SerializeField] private int rangeSortingOrder = 2;

    private readonly List<Unit> candidates = new List<Unit>();
    private readonly List<Unit> chosen = new List<Unit>();

    private SpriteRenderer rangeRing, rangeFill;
    private bool healedThisFrame;
    private float glow;      // 1 right after a heal pulse, fading to 0
    private float cooldown;  // seconds until the next pulse may happen

    public override void Act(float dt)
    {
        cooldown = Mathf.Max(0f, cooldown - dt);
        if (cooldown > 0f) return;

        List<Unit> targets = LowestHealthTargets();
        if (targets.Count == 0) return;   // stay ready: heal as soon as something is hurt

        // Damage is heal per second (the Damage passive does not apply); a pulse heals one interval's worth.
        float interval = Stats.Rate > 0f ? 1f / Stats.Rate : 0f;
        float amount = Stats.Damage * (interval > 0f ? interval : dt);
        cooldown = interval;
        healedThisFrame = true;

        foreach (Unit target in targets)
        {
            float missing = target.MaxHP - target.HP;
            ((IHealable)target).Heal(amount);
            float overflow = amount - missing;
            if (OverhealShield && overflow > 0f)
                ((IHealable)target).AddShield(overflow, Data.ShieldCap * target.MaxHP);
        }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        UpdateRangeCircle();
    }

    // Faint ring while idle; on each heal pulse it flashes bright with a soft fill, then fades back.
    // Hidden outside survival (the build phase shows the range on selection instead) and while destroyed.
    private void UpdateRangeCircle()
    {
        bool healed = healedThisFrame;
        healedThisFrame = false;

        GameManager gm = GameManager.Instance;
        bool show = showRange && IsAlive() && gm != null &&
                    (gm.Phase == GamePhase.Survival || gm.Phase == GamePhase.LevelUp);

        if (rangeRing == null)
        {
            if (!show) return;
            rangeFill = PlaceholderArt.AddChild(transform, "HealRangeFill", PlaceholderArt.Circle, rangeColor, 1f, rangeSortingOrder);
            rangeRing = PlaceholderArt.AddChild(transform, "HealRangeRing", PlaceholderArt.Ring, rangeColor, 1f, rangeSortingOrder);
        }

        if (rangeRing.gameObject.activeSelf != show)
        {
            rangeRing.gameObject.SetActive(show);
            rangeFill.gameObject.SetActive(show);
        }
        if (!show)
        {
            glow = 0f;
            return;
        }

        // Diameter = 2 × range in world units, whatever the tower's own scale.
        float size = Stats.Range * 2f / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        rangeRing.transform.localScale = new Vector3(size, size, 1f);
        rangeFill.transform.localScale = new Vector3(size, size, 1f);

        if (healed) glow = 1f;
        else if (gm.Phase == GamePhase.Survival)   // frozen while the level-up cards are open
            glow = Mathf.MoveTowards(glow, 0f, Time.deltaTime / Mathf.Max(0.01f, flashFade));

        rangeRing.color = WithAlpha(rangeColor, Mathf.Lerp(idleAlpha, healingAlpha, glow));
        rangeFill.color = WithAlpha(rangeColor, 0.15f * glow);
    }

    private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    /// Up to Targets units in range that can take healing, lowest (HP + shield) share of max HP first.
    public List<Unit> LowestHealthTargets()
    {
        float range = Stats.Range;
        candidates.Clear();

        Base b = GameManager.Instance.Base;
        if (CanTake(b, range)) candidates.Add(b);

        foreach (Tower t in GameManager.Instance.Builder.Towers)
            if (t != this && CanTake(t, range)) candidates.Add(t);

        candidates.Sort((a, c) => Fill(a).CompareTo(Fill(c)));

        chosen.Clear();
        for (int i = 0; i < candidates.Count && i < Stats.Targets; i++) chosen.Add(candidates[i]);
        return chosen;
    }

    private static float Fill(Unit u) => (u.HP + u.Shield) / u.MaxHP;

    private bool CanTake(Unit u, float range)
    {
        if (!u.IsAlive() || Vector2.Distance(Position, u.Position) > range) return false;
        if (u.HP < u.MaxHP) return true;
        return OverhealShield && u.Shield < Data.ShieldCap * u.MaxHP;
    }
}
