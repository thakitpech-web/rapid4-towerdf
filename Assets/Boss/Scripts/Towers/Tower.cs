using UnityEngine;

/// A tower on one grid cell (never a lane or the base). It blocks the character and swarm monsters;
/// convoy monsters never meet it because towers are off the lanes. A destroyed tower stops working and stops blocking
/// until the loop ends, then revives at full HP. BuildManager creates, moves, merges and sells towers.
/// Visuals come from the prefab (TowerData.Prefab): every part below is optional.
public abstract class Tower : Unit, IHealable
{
    [Header("Visuals (all optional)")]
    [Tooltip("Main sprite of the tower")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("Body sprite per tier (element 0 = T1). Empty entries keep the sprite set on Body.")]
    [SerializeField] private Sprite[] tierSprites = new Sprite[3];
    [Tooltip("Marks shown up to the tier (e.g. 3 stars: T2 shows the first two)")]
    [SerializeField] private GameObject[] tierMarks;
    [Tooltip("Shown while the tower is selected in the build phase")]
    [SerializeField] private GameObject selectedMark;
    [Tooltip("HP bar root, shown while the tower is alive")]
    [SerializeField] private GameObject hpBar;
    [Tooltip("Fill sprite inside the HP bar; shrinks on X, keeping its left edge in place")]
    [SerializeField] private SpriteRenderer hpFill;
    [Tooltip("Shield bar root (Heal tier-3 overheal), shown only while the tower has shield")]
    [SerializeField] private GameObject shieldBar;
    [Tooltip("Fill sprite inside the shield bar; shrinks on X, keeping its left edge in place")]
    [SerializeField] private SpriteRenderer shieldFill;
    [Tooltip("Shield (as a share of max HP) that fills the shield bar; 0.3 = the Heal tower's cap")]
    [SerializeField] private float shieldFullAt = 0.3f;
    [Tooltip("Multiplied into the body colour while destroyed")]
    [SerializeField] private Color deadTint = new Color(0.5f, 0.5f, 0.5f, 0.4f);

    public TowerData Data { get; private set; }
    public int Tier { get; private set; }
    public Vector2Int Cell { get; set; }

    /// Seconds left disabled (boss shockwave, build step 11).
    public float DisabledTime { get; set; }

    public TowerStats Stats => Data.GetStats(Tier);

    private bool selected;
    private Color bodyColor = Color.white;
    private SpriteBar hpSprite, shieldSprite;

    /// Called by BuildManager right after the prefab is instantiated.
    public void Init(TowerData data, int tier, Vector2Int cell)
    {
        Data = data;
        Cell = cell;

        if (body != null) bodyColor = body.color;
        hpSprite = new SpriteBar(hpFill);
        shieldSprite = new SpriteBar(shieldFill);
        SetTier(tier);
    }

    /// HP bar always (while alive), shield bar while shielded.
    private void LateUpdate()
    {
        bool showHp = IsAlive();
        if (hpBar != null && hpBar.activeSelf != showHp) hpBar.SetActive(showHp);
        if (showHp) hpSprite.Set(HP / MaxHP);

        bool showShield = IsAlive() && Shield > 0f;
        if (shieldBar != null && shieldBar.activeSelf != showShield) shieldBar.SetActive(showShield);
        if (showShield) shieldSprite.Set(shieldFullAt > 0f ? Shield / (MaxHP * shieldFullAt) : 1f);
    }

    /// A fill sprite that shrinks on X while keeping its left edge in place.
    private struct SpriteBar
    {
        private readonly SpriteRenderer fill;
        private readonly Vector3 scale, pos;
        private readonly float width, pivot;

        public SpriteBar(SpriteRenderer fill)
        {
            this.fill = fill;
            scale = pos = Vector3.zero;
            width = 0f;
            pivot = 0.5f;
            if (fill == null) return;

            scale = fill.transform.localScale;
            pos = fill.transform.localPosition;
            Sprite s = fill.sprite;
            width = s != null ? s.bounds.size.x * scale.x : scale.x;
            pivot = s != null && s.rect.width > 0f ? s.pivot.x / s.rect.width : 0.5f;
        }

        public void Set(float frac)
        {
            if (fill == null) return;
            frac = Mathf.Clamp01(frac);
            fill.transform.localScale = new Vector3(scale.x * frac, scale.y, scale.z);
            fill.transform.localPosition = pos - new Vector3(pivot * width * (1f - frac), 0f, 0f);
        }
    }

    /// Changes tier (merge) and restores full HP for the new tier.
    public void SetTier(int tier)
    {
        Tier = Mathf.Clamp(tier, 1, Data.MaxTier);
        RestoreFull(Stats.MaxHP);
        DisabledTime = 0f;

        if (body != null && tierSprites != null && Tier - 1 < tierSprites.Length && tierSprites[Tier - 1] != null)
            body.sprite = tierSprites[Tier - 1];
        if (tierMarks != null)
            for (int i = 0; i < tierMarks.Length; i++)
                if (tierMarks[i] != null) tierMarks[i].SetActive(i < Tier);

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
        GameManager.Instance.Grid.NotifyChanged();   // stops blocking the character and swarm
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

    /// Selected in the build phase (shows selectedMark).
    public void SetSelected(bool on)
    {
        selected = on;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (body != null) body.color = IsAlive() ? bodyColor : bodyColor * deadTint;
        if (selectedMark != null) selectedMark.SetActive(selected);
    }
}
