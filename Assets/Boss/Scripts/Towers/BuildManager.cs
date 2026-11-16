using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// Build phase (GAME_DESIGN.md sections 3 and 10). A tower fills one buildable GridMap cell.
/// Place: pick a shop item, tap an empty buildable cell. Move: tap a tower, then an empty cell (free).
/// Merge: tap a tower, then a same-type same-tier tower. Sell: half the total cost, rounded down.
/// A tap places or selects; a drag pans. Mouse wheel or +/- zooms, WASD pans.
/// Shows the monster route from the middle of each open side and marks towers it breaks through.
public class BuildManager : MonoBehaviour
{
    private static readonly Color DashColor = new Color(1f, 1f, 1f, 0.8f);
    private static readonly Color RangeColor = new Color(1f, 1f, 1f, 0.6f);
    private static readonly Color GhostOk = new Color(0.4f, 1f, 0.4f, 0.45f);
    private static readonly Color GhostBad = new Color(1f, 0.3f, 0.3f, 0.45f);
    private const float DashLength = 0.45f;
    private const float DashWidth = 0.09f;

    private readonly List<Tower> towers = new List<Tower>();
    private readonly HashSet<Tower> breached = new HashSet<Tower>();
    private readonly List<SpriteRenderer> dashes = new List<SpriteRenderer>();
    private int dashesUsed;
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
        RefreshPreview();
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
        if (!GM.TrySpendOrbs(data.TotalCost(1))) return false;

        Tower t = CreateTower(data, cell);
        towers.Add(t);
        Grid.SetOccupant(cell, t);
        RefreshPreview();
        return true;
    }

    public void MoveTower(Tower t, Vector2Int cell)
    {
        if (!InBuild || t == null || !Grid.CanBuild(cell)) return;

        Grid.SetOccupant(t.Cell, null);
        t.Cell = cell;
        t.transform.position = Grid.CellCenter(cell);
        Grid.SetOccupant(cell, t);
        RefreshPreview();
    }

    /// Merges <paramref name="from"/> into <paramref name="into"/>; the first cell becomes empty.
    public bool MergeTowers(Tower from, Tower into)
    {
        if (!InBuild || from == null || !from.CanMergeWith(into)) return false;

        RemoveTower(from);
        into.SetTier(into.Tier + 1);
        Grid.NotifyChanged();                   // more HP: higher path cost
        SelectTower(into);
        RefreshPreview();
        return true;
    }

    public void SellTower(Tower t)
    {
        if (!InBuild || t == null) return;

        GM.AddOrbs(t.SellValue());
        RemoveTower(t);
        RefreshPreview();
    }

    /// Sell button.
    public void SellSelected() => SellTower(SelectedTower);

    /// Cells monsters from the middle of this side would walk to the base.
    public List<Vector2Int> PreviewRoute(SpawnSide side)
    {
        int w = Grid.Width, h = Grid.Height;
        Vector2Int start;
        switch (side)
        {
            case SpawnSide.Left: start = new Vector2Int(0, h / 2); break;
            case SpawnSide.Right: start = new Vector2Int(w - 1, h / 2); break;
            case SpawnSide.Top: start = new Vector2Int(w / 2, h - 1); break;
            default: start = new Vector2Int(w / 2, 0); break;
        }
        return GM.Flow.TraceRoute(start);
    }

    private Tower CreateTower(TowerData data, Vector2Int cell)
    {
        var go = new GameObject($"{data.DisplayName} Tower");
        go.transform.SetParent(transform, false);
        go.transform.position = Grid.CellCenter(cell);

        Tower t;
        switch (data.Type)
        {
            case TowerType.Bolt: t = go.AddComponent<BoltTower>(); break;
            case TowerType.Heal: t = go.AddComponent<HealTower>(); break;
            default: t = go.AddComponent<AoETower>(); break;
        }
        t.Init(data, 1, cell);
        return t;
    }

    private void RemoveTower(Tower t)
    {
        if (SelectedTower == t) SelectTower(null);
        breached.Remove(t);
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
            else PlaceTower(SelectedShopItem.Value, cell);      // stays picked for placing more
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
            ShowRange(at, data.GetStats(1).Range);
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

    /// Redraws the dashed routes for the next loop's open sides and the breach marks.
    private void RefreshPreview() => DrawPreview(InBuild);

    private void DrawPreview(bool show)
    {
        foreach (Tower t in breached) t.SetBreachMark(false);
        breached.Clear();
        dashesUsed = 0;

        if (show)
        {
            foreach (SpawnSide side in GM.Spawner.SidesFor(GM.CurrentLoop))
            {
                List<Vector2Int> route = PreviewRoute(side);
                for (int i = 1; i < route.Count; i++)
                {
                    PlaceDash(Grid.CellCenter(route[i - 1]), Grid.CellCenter(route[i]));
                    Tower t = Grid.LiveTowerAt(route[i]);
                    if (t != null && breached.Add(t)) t.SetBreachMark(true);
                }
            }
        }

        for (int i = dashesUsed; i < dashes.Count; i++) dashes[i].enabled = false;
    }

    private void PlaceDash(Vector2 a, Vector2 b)
    {
        if (dashesUsed == dashes.Count)
            dashes.Add(PlaceholderArt.AddChild(transform, "RouteDash", PlaceholderArt.Square, DashColor, 1f,
                                               PlaceholderArt.RouteOrder));

        SpriteRenderer dash = dashes[dashesUsed++];
        Vector2 d = b - a;
        dash.transform.position = (a + b) * 0.5f;
        dash.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        dash.transform.localScale = new Vector3(DashLength, DashWidth, 1f);
        dash.enabled = true;
    }
}
