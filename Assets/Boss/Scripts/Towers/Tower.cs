using UnityEngine;

/// A tower on one grid cell. Towers are walls: monsters path around them or break through
/// (flow-field cost 1 + HP / divisor). A destroyed tower stops working and stops blocking until
/// the loop ends, then revives at full HP. BuildManager creates, moves, merges and sells towers.
public abstract class Tower : Unit, IHealable
{
    private static readonly Color BreachColor = new Color(1f, 0.2f, 0.2f, 1f);
    private static readonly Color SelectColor = new Color(1f, 0.95f, 0.3f, 1f);
    private const float BodyScale = 0.8f;
    private const float DeadAlpha = 0.3f;

    public TowerData Data { get; private set; }
    public int Tier { get; private set; }
    public Vector2Int Cell { get; set; }

    /// Seconds left disabled (boss shockwave, build step 11).
    public float DisabledTime { get; set; }

    public TowerStats Stats => Data.GetStats(Tier);

    private static readonly Color HpBackColor = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Color HpFillColor = new Color(0.4f, 1f, 0.4f, 1f);
    private const float HpBarWidth = 0.8f, HpBarHeight = 0.1f, HpBarY = 0.55f;

    private SpriteRenderer body;
    private SpriteRenderer outline;
    private SpriteRenderer hpBack, hpFill;
    private Transform pips;
    private bool breach, selected;

    /// Called by BuildManager right after the component is added.
    public void Init(TowerData data, int tier, Vector2Int cell)
    {
        Data = data;
        Cell = cell;
        body = PlaceholderArt.AddChild(transform, "Body", PlaceholderArt.Square, data.Color, BodyScale,
                                       PlaceholderArt.TowerOrder);
        outline = PlaceholderArt.AddChild(transform, "Outline", PlaceholderArt.SquareOutline, Color.white, 1f,
                                          PlaceholderArt.TowerDetailOrder);
        hpBack = PlaceholderArt.AddChild(transform, "HpBack", PlaceholderArt.Square, HpBackColor, 1f,
                                         PlaceholderArt.TowerDetailOrder);
        hpFill = PlaceholderArt.AddChild(transform, "HpFill", PlaceholderArt.Square, HpFillColor, 1f,
                                         PlaceholderArt.TowerDetailOrder + 1);
        hpBack.transform.localPosition = new Vector3(0f, HpBarY, 0f);
        hpBack.transform.localScale = new Vector3(HpBarWidth, HpBarHeight, 1f);
        SetTier(tier);
    }

    /// HP bar above the tower, shown only while damaged.
    private void LateUpdate()
    {
        if (hpBack == null) return;

        bool show = IsAlive() && HP < MaxHP;
        hpBack.enabled = hpFill.enabled = show;
        if (!show) return;

        float frac = HP / MaxHP;
        hpFill.transform.localScale = new Vector3(HpBarWidth * frac, HpBarHeight, 1f);
        hpFill.transform.localPosition = new Vector3(-HpBarWidth * (1f - frac) * 0.5f, HpBarY, 0f);
    }

    /// Changes tier (merge) and restores full HP for the new tier.
    public void SetTier(int tier)
    {
        Tier = Mathf.Clamp(tier, 1, Data.MaxTier);
        RestoreFull(Stats.MaxHP);
        DisabledTime = 0f;
        BuildPips();
        RefreshVisuals();
    }

    /// Loop start: full HP, working again even if destroyed last loop.
    public void ResetForLoop()
    {
        RestoreFull(Stats.MaxHP);
        DisabledTime = 0f;
        RefreshVisuals();
    }

    public bool CanMergeWith(Tower other) =>
        other != null && other != this && other.Data == Data && other.Tier == Tier && Tier < Data.MaxTier;

    public int TotalCost() => Data.TotalCost(Tier);

    /// Half the total cost, rounded down.
    public int SellValue() => TotalCost() / 2;

    /// Flow-field cost of entering this tower's cell.
    public float PathCost() => 1f + HP / Config.flowField.towerHpDivisor;

    /// Bolt and AoE damage including the tower damage passive (not healing).
    protected float ModifiedDamage =>
        Stats.Damage * (1f + GameManager.Instance.Upgrades.GetModifier(UpgradeType.TowerDamage));

    private void Update()
    {
        if (!IsSurvivalPhase || !IsAlive()) return;

        float dt = Time.deltaTime;
        if (DisabledTime > 0f)
        {
            DisabledTime = Mathf.Max(0f, DisabledTime - dt);
            return;
        }
        Act(dt);
    }

    public abstract void Act(float dt);

    public override void Die()
    {
        base.Die();
        RefreshVisuals();
        GameManager.Instance.Grid.NotifyChanged();   // stops blocking: monsters re-route through here
    }

    // ---------- IHealable ----------

    public void Heal(float amount)
    {
        if (!IsAlive()) return;
        hp = Mathf.Min(maxHP, hp + amount);
    }

    public void AddShield(float amount, float cap)
    {
        if (!IsAlive()) return;
        shield = Mathf.Min(cap, shield + amount);
    }

    // ---------- Build-phase marks ----------

    /// Red outline: the previewed monster route breaks through this tower.
    public void SetBreachMark(bool on)
    {
        breach = on;
        RefreshVisuals();
    }

    /// Yellow outline: selected in the build phase.
    public void SetSelected(bool on)
    {
        selected = on;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (body == null) return;

        Color c = Data.Color;
        c.a = IsAlive() ? 1f : DeadAlpha;
        body.color = c;

        outline.enabled = selected || breach;
        outline.color = selected ? SelectColor : BreachColor;
    }

    /// One small dot per tier along the bottom of the tower.
    private void BuildPips()
    {
        if (pips != null) Destroy(pips.gameObject);
        pips = new GameObject("TierPips").transform;
        pips.SetParent(transform, false);

        const float size = 0.14f, gap = 0.2f;
        for (int i = 0; i < Tier; i++)
        {
            SpriteRenderer pip = PlaceholderArt.AddChild(pips, "Pip", PlaceholderArt.Circle, Color.white, size,
                                                         PlaceholderArt.TowerDetailOrder);
            pip.transform.localPosition = new Vector3((i - (Tier - 1) * 0.5f) * gap, -0.25f, 0f);
        }
    }
}
