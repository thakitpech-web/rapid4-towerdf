using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// Build phase. One tower per cell, on any cell except lanes, the base and the character's start cell.
/// Place: pick a shop item, tap an empty cell. Move: tap a tower, then an empty cell (free).
/// Merge: tap a tower, then a same-type same-tier tower. Sell: half the total cost, rounded down.
/// A tap places or selects; a drag pans. Mouse wheel or +/- zooms, WASD pans.
/// Preview: chevrons along the lanes convoys use next loop, and a pulsing "new" label on lanes opening
/// for the first time. Lane colours come from GridMap.
public class BuildManager : MonoBehaviour
{
    private static readonly Color RangeColor = new Color(1f, 1f, 1f, 0.6f);
    private static readonly Color GhostOk = new Color(0.4f, 1f, 0.4f, 0.45f);
    private static readonly Color GhostBad = new Color(1f, 0.3f, 0.3f, 0.45f);

    [Header("Lane preview (prefabs made in the editor)")]
    [Tooltip("A SpriteRenderer arrow placed along open lanes, rotated to the walking direction")]
    [SerializeField] private SpriteRenderer chevronPrefab;
    [Tooltip("Direction the chevron sprite points when not rotated, in degrees (0 = right, 90 = up)")]
    [SerializeField] private float chevronSpriteAngle;
    [Tooltip("A chevron every this many tiles along the lane")]
    [SerializeField, Min(1)] private int chevronSpacing = 3;
    [Tooltip("Any object (e.g. a world-space TextMeshPro \"NEW\") shown on lanes opening for the first time")]
    [SerializeField] private GameObject newLabelPrefab;
    [Tooltip("Tiles from the lane entrance, along the lane, where the label sits")]
    [FormerlySerializedAs("newLabelCell")]
    [SerializeField, Min(0f)] private float newLabelDistance = 2f;
    [Tooltip("Label offset from that point (tiles)")]
    [SerializeField] private Vector2 newLabelOffset = new Vector2(0f, 0.8f);
    [Tooltip("Pulses per second for the label and the chevrons of a new lane")]
    [SerializeField] private float newPulseSpeed = 2f;
    [Tooltip("Label scale change while pulsing (0.2 = ±20%)")]
    [SerializeField] private float newPulseScale = 0.2f;

    private readonly List<Tower> towers = new List<Tower>();
    private readonly List<SpriteRenderer> chevrons = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> newChevrons = new List<SpriteRenderer>();
    private readonly List<GameObject> labels = new List<GameObject>();
    private int chevronsUsed, labelsUsed;
    private SpriteRenderer rangeCircle, ghost;

    private bool pressing, dragging;
    private Vector2 pressPos, lastPointerPos;

    public IReadOnlyList<Tower> Towers => towers;
    public GridMap Grid => GameManager.Instance.Grid;

    /// Tower type picked in the shop, or null.
    public TowerType? SelectedShopItem { get; private set; }

    /// Tower tapped on the map, or null.
    public Tower SelectedTower { get; private set; }

    private static GameManager GM => GameManager.Instance;
    private static bool InBuild => GM != null && GM.Phase == GamePhase.Build;

    /// The loop the build phase prepares for (CurrentLoop is already the next one).
    private static int NextLoop => GM.CurrentLoop;

    private void Awake()
    {
        rangeCircle = PlaceholderArt.AddChild(transform, "RangeCircle", PlaceholderArt.Ring, RangeColor, 1f,
                                              PlaceholderArt.BuildCursorOrder);
        ghost = PlaceholderArt.AddChild(transform, "PlaceGhost", PlaceholderArt.Square, GhostOk, 0.9f,
                                        PlaceholderArt.BuildCursorOrder);
        rangeCircle.enabled = ghost.enabled = false;
    }

    public TowerData DataFor(TowerType type)
    {
        foreach (TowerData d in GM.Config.towerTypes)
            if (d != null && d.Type == type) return d;
        return null;
    }

    // ---------- Phase hooks (called by GameManager) ----------

    public void EnterBuild()
    {
        ClearSelection();
        DrawPreview(true);
    }

    public void ExitBuild()
    {
        ClearSelection();
        pressing = dragging = false;
        DrawPreview(false);
        rangeCircle.enabled = ghost.enabled = false;
    }

    /// Loop start: every tower, destroyed ones too, back to full HP.
    public void ResetTowersForLoop()
    {
        foreach (Tower t in towers) t.ResetForLoop();
        Grid.NotifyChanged();
    }

    // ---------- Selection ----------

    /// Shop button: picks a tower type (tapping the same one again cancels).
    public void SelectShopItem(TowerType type)
    {
        bool same = SelectedShopItem == type;
        ClearSelection();
        if (!same) SelectedShopItem = type;
    }

    public void ClearSelection()
    {
        SelectedShopItem = null;
        SelectTower(null);
    }

    private void SelectTower(Tower t)
    {
        if (SelectedTower != null) SelectedTower.SetSelected(false);
        SelectedTower = t;
        if (t != null)
        {
            SelectedShopItem = null;
            t.SetSelected(true);
        }
    }

    // ---------- Build actions ----------

    public bool PlaceTower(TowerType type, Vector2Int cell)
    {
        TowerData data = DataFor(type);
        if (!InBuild || data == null || !Grid.CanBuild(cell)) return false;
        if (data.Prefab == null)
        {
            Debug.LogError($"{data.name} has no Prefab: assign the tower prefab in the TowerData asset.");
            return false;
        }
        if (!GM.TrySpendOrbs(data.TotalCost(1))) return false;

        Tower t = CreateTower(data, cell);
        towers.Add(t);
        Grid.SetOccupant(cell, t);
        return true;
    }

    public void MoveTower(Tower t, Vector2Int cell)
    {
        if (!InBuild || t == null || !Grid.CanBuild(cell)) return;

        Grid.SetOccupant(t.Cell, null);
        t.Cell = cell;
        t.transform.position = Grid.CellCenter(cell);
        Grid.SetOccupant(cell, t);
    }

    /// Merges <paramref name="from"/> into <paramref name="into"/>; the first cell becomes empty.
    public bool MergeTowers(Tower from, Tower into)
    {
        if (!InBuild || from == null || !from.CanMergeWith(into)) return false;

        RemoveTower(from);
        into.SetTier(into.Tier + 1);
        SelectTower(into);
        return true;
    }

    public void SellTower(Tower t)
    {
        if (!InBuild || t == null) return;

        GM.AddOrbs(t.SellValue());
        RemoveTower(t);
    }

    /// Sell button.
    public void SellSelected() => SellTower(SelectedTower);

    /// Lanes convoys use in a loop (highlighted in the build phase).
    public List<SpawnSide> PreviewLanes(int loop) => new List<SpawnSide>(GM.Spawner.WaveFor(loop).ConvoySides);

    private Tower CreateTower(TowerData data, Vector2Int cell)
    {
        Tower t = Instantiate(data.Prefab, Grid.CellCenter(cell), Quaternion.identity, transform);
        t.name = $"{data.DisplayName} Tower";
        t.Init(data, 1, cell);
        return t;
    }

    private void RemoveTower(Tower t)
    {
        if (SelectedTower == t) SelectTower(null);
        towers.Remove(t);
        Grid.SetOccupant(t.Cell, null);
        Destroy(t.gameObject);
    }

    // ---------- Input ----------

    private void Update()
    {
        if (!InBuild) return;

        HandleKeys();
        HandlePointer();
        UpdateCursor();
        PulseNewLanes();
    }

    private void HandleKeys()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        CameraController cam = GM.CameraView;
        GameConfig.BuildInputSettings input = GM.Config.buildInput;
        float dt = Time.unscaledDeltaTime;

        if (kb.escapeKey.wasPressedThisFrame) ClearSelection();

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed) cam.ZoomAt(Mathf.Pow(input.keyZoomPerSecond, -dt), center);
        if (kb.minusKey.isPressed || kb.numpadMinusKey.isPressed) cam.ZoomAt(Mathf.Pow(input.keyZoomPerSecond, dt), center);

        Vector2 pan = Vector2.zero;
        if (kb.wKey.isPressed) pan.y += 1f;
        if (kb.sKey.isPressed) pan.y -= 1f;
        if (kb.dKey.isPressed) pan.x += 1f;
        if (kb.aKey.isPressed) pan.x -= 1f;
        if (pan != Vector2.zero)
            cam.Pan(pan.normalized * (input.keyPanSpeed * Screen.height * cam.WorldPerPixel * dt));
    }

    private void HandlePointer()
    {
        Pointer p = Pointer.current;
        if (p == null) return;

        CameraController cam = GM.CameraView;
        Vector2 pos = p.position.ReadValue();

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.wasPressedThisFrame) ClearSelection();

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f && !IsOverUI())
                cam.ZoomAt(Mathf.Pow(GM.Config.buildInput.wheelZoomStep, -Mathf.Sign(scroll)), pos);
        }

        if (p.press.wasPressedThisFrame)
        {
            pressing = !IsOverUI();
            dragging = false;
            pressPos = lastPointerPos = pos;
        }

        if (!pressing) return;

        if (p.press.isPressed)
        {
            if (!dragging && (pos - pressPos).magnitude > GM.Config.buildInput.dragThresholdPixels) dragging = true;
            if (dragging) cam.Pan((lastPointerPos - pos) * cam.WorldPerPixel);
            lastPointerPos = pos;
        }

        if (p.press.wasReleasedThisFrame)
        {
            pressing = false;
            if (!dragging) Tap(Grid.WorldToCell(cam.ScreenToWorld(pos)));
        }
    }

    private void Tap(Vector2Int cell)
    {
        Tower tapped = Grid.TowerAt(cell);

        if (SelectedShopItem.HasValue)
        {
            if (tapped != null) SelectTower(tapped);
            else PlaceTower(SelectedShopItem.Value, cell);     // stays picked for placing more
            return;
        }

        if (SelectedTower != null)
        {
            if (tapped == SelectedTower) SelectTower(null);
            else if (tapped != null)
            {
                if (!MergeTowers(SelectedTower, tapped)) SelectTower(tapped);
            }
            else if (Grid.CanBuild(cell)) MoveTower(SelectedTower, cell);
            else SelectTower(null);
            return;
        }

        if (tapped != null) SelectTower(tapped);
    }

    private static bool IsOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    // ---------- Visuals ----------

    /// Range circle for the picked shop item or selected tower, and a ghost on the hovered cell.
    private void UpdateCursor()
    {
        rangeCircle.enabled = ghost.enabled = false;
        Pointer p = Pointer.current;

        if (SelectedShopItem.HasValue && p != null && !IsOverUI())
        {
            TowerData data = DataFor(SelectedShopItem.Value);
            Vector2Int cell = Grid.WorldToCell(GM.CameraView.ScreenToWorld(p.position.ReadValue()));
            if (data == null || !Grid.InBounds(cell)) return;

            Vector2 at = Grid.CellCenter(cell);
            bool ok = Grid.CanBuild(cell) && GM.Orbs >= data.TotalCost(1);
            ghost.transform.position = at;
            ghost.color = ok ? GhostOk : GhostBad;
            ghost.enabled = true;
            ShowRange(at, Tower.WithRangeBonus(data.GetStats(1)).Range);
        }
        else if (SelectedTower != null)
        {
            ShowRange(SelectedTower.Position, SelectedTower.Stats.Range);
        }
    }

    private void ShowRange(Vector2 at, float range)
    {
        rangeCircle.transform.position = at;
        rangeCircle.transform.localScale = new Vector3(range * 2f, range * 2f, 1f);
        rangeCircle.enabled = true;
    }

    /// Chevrons along next loop's convoy lanes and a "new" label on lanes opening for the first time.
    private void DrawPreview(bool show)
    {
        chevronsUsed = labelsUsed = 0;
        newChevrons.Clear();

        if (show)
        {
            foreach (SpawnSide side in PreviewLanes(NextLoop))
            {
                Lane lane = Grid.LaneFor(side);
                bool isNew = GM.Spawner.LaneOpensFirstTime(side, NextLoop);

                for (float d = 1f; d < lane.TotalLength; d += chevronSpacing)
                {
                    SpriteRenderer c = PlaceChevron(lane, d);
                    if (c != null && isNew) newChevrons.Add(c);
                }
                if (isNew) PlaceLabel(lane);
            }
        }

        for (int i = chevronsUsed; i < chevrons.Count; i++) chevrons[i].gameObject.SetActive(false);
        for (int i = labelsUsed; i < labels.Count; i++) labels[i].SetActive(false);
    }

    private SpriteRenderer PlaceChevron(Lane lane, float distance)
    {
        if (chevronPrefab == null) return null;
        if (chevronsUsed == chevrons.Count) chevrons.Add(Instantiate(chevronPrefab, transform));

        SpriteRenderer c = chevrons[chevronsUsed++];
        lane.Sample(distance, out Vector2 pos, out Vector2 dir);
        c.transform.position = pos;
        c.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - chevronSpriteAngle);
        c.color = chevronPrefab.color;
        c.gameObject.SetActive(true);
        return c;
    }

    private void PlaceLabel(Lane lane)
    {
        if (newLabelPrefab == null) return;
        if (labelsUsed == labels.Count) labels.Add(Instantiate(newLabelPrefab, transform));

        GameObject label = labels[labelsUsed++];
        lane.Sample(newLabelDistance, out Vector2 pos, out _);
        label.transform.position = pos + newLabelOffset;
        label.SetActive(true);
    }

    private void PulseNewLanes()
    {
        float wave = Mathf.Sin(Time.unscaledTime * newPulseSpeed * 2f * Mathf.PI) * 0.5f + 0.5f;   // 0..1

        if (newLabelPrefab != null)
        {
            Vector3 baseScale = newLabelPrefab.transform.localScale;
            for (int i = 0; i < labelsUsed; i++)
                labels[i].transform.localScale = baseScale * (1f + newPulseScale * (wave * 2f - 1f));
        }

        if (chevronPrefab != null)
        {
            Color c = chevronPrefab.color;
            c.a *= Mathf.Lerp(0.35f, 1f, wave);
            foreach (SpriteRenderer sr in newChevrons) sr.color = c;
        }
    }
}
