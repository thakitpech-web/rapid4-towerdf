using System;
using UnityEngine;

/// The tile grid. Cell (x, y) spans x..x+1, y..y+1 in world units (y up).
/// Owns the base cells, the 4 lanes (from the scene layout, or GameConfig.map) and the tower on each cell,
/// and does circle-vs-cell collision for the character and swarm monsters. Towers may stand on any cell
/// except lane cells, base cells and the character's start cell. GameManager calls Init before anything
/// reads it, and RefreshVisuals when the loop shown on the map changes.
public class GridMap : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Parent of the LanePath objects drawn in the scene. A lane side without a LanePath falls back to " +
             "the default lanes in GameConfig.map.")]
    [SerializeField] private Transform layoutRoot;

    [Header("Lane overlay")]
    [Tooltip("Lanes convoys use in the shown loop")]
    [SerializeField] private Color laneOpenColor = new Color(1f, 0.6f, 0.15f, 0.55f);
    [Tooltip("Lanes that are closed in the shown loop")]
    [SerializeField] private Color laneClosedColor = new Color(0.55f, 0.55f, 0.55f, 0.35f);
    [SerializeField] private int overlaySortingOrder = 1;

    public int Width { get; private set; }
    public int Height { get; private set; }

    public RectInt BaseCells { get; private set; }
    public Vector2 BaseCenter => BaseCells.center;
    public Rect BaseRect => new Rect(BaseCells.position, BaseCells.size);

    /// Indexed by SpawnSide.
    public Lane[] Lanes { get; private set; }

    /// Character start position in world space.
    public Vector2 PlayerStart { get; private set; }

    /// Raised when a tower is placed, moved, removed, destroyed or revived.
    public event Action OnChanged;

    private Tower[] towerAt;        // per cell
    private int[] laneAt;           // per cell: SpawnSide + 1, or 0
    private Texture2D overlay;

    public void Init(GameConfig config)
    {
        GameConfig.MapSettings m = config.map;
        Width = m.width;
        Height = m.height;
        BaseCells = new RectInt(m.baseCell.x, m.baseCell.y, m.baseSize, m.baseSize);
        PlayerStart = m.playerStart;
        towerAt = new Tower[Width * Height];

        BuildLanes(m);
        BuildOverlay();
    }

    // ---------- Layout ----------

    /// The default lanes from GameConfig.map, indexed by SpawnSide.
    public static Vector2Int[][] DefaultLaneTurns(GameConfig.MapSettings m)
    {
        var laneTurns = new Vector2Int[4][];
        laneTurns[(int)SpawnSide.Left] = m.leftLane;
        laneTurns[(int)SpawnSide.Right] = m.rightLane;
        laneTurns[(int)SpawnSide.Top] = m.topLane;
        laneTurns[(int)SpawnSide.Bottom] = m.bottomLane;
        return laneTurns;
    }

    /// Lanes from the scene (LanePath under Layout Root) where present, else the default lanes (1 tile wide).
    private void BuildLanes(GameConfig.MapSettings m)
    {
        Vector2Int[][] laneTurns = DefaultLaneTurns(m);
        var lanePoints = new Vector2[4][];
        var laneWidths = new int[4];
        for (int s = 0; s < 4; s++)
        {
            lanePoints[s] = new Vector2[laneTurns[s].Length];
            for (int i = 0; i < laneTurns[s].Length; i++) lanePoints[s][i] = CellCenter(laneTurns[s][i]);
            laneWidths[s] = 1;
        }

        if (layoutRoot != null)
            foreach (LanePath path in layoutRoot.GetComponentsInChildren<LanePath>())
            {
                lanePoints[(int)path.Side] = path.Points();
                laneWidths[(int)path.Side] = path.Width;
            }

        Lanes = new Lane[4];
        laneAt = new int[Width * Height];
        foreach (SpawnSide side in (SpawnSide[])Enum.GetValues(typeof(SpawnSide)))
        {
            Lane lane = new Lane(side, lanePoints[(int)side], laneWidths[(int)side]);
            Lanes[(int)side] = lane;
            if (lane.Points.Length < 2) Debug.LogError($"Lane {side} needs at least 2 turn points.");
            foreach (Vector2Int c in lane.Cells)
                if (InBounds(c) && !IsBase(c)) laneAt[Index(c)] = (int)side + 1;
        }
    }

#if UNITY_EDITOR
    /// Inspector menu (⋮ on the Grid Map component): creates LanePath objects for the default lanes under
    /// Layout Root, to edit by dragging. Replaces any layout objects already there.
    [ContextMenu("Create Layout From GameConfig")]
    private void CreateLayoutFromConfig()
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null || gm.Config == null)
        {
            Debug.LogError("No GameManager with a GameConfig in the scene.");
            return;
        }

        if (layoutRoot == null)
        {
            layoutRoot = new GameObject("MapLayout").transform;
            layoutRoot.SetParent(transform, false);
            UnityEditor.Undo.RegisterCreatedObjectUndo(layoutRoot.gameObject, "Create map layout");
        }
        UnityEditor.Undo.RecordObject(this, "Create map layout");
        for (int i = layoutRoot.childCount - 1; i >= 0; i--)
            UnityEditor.Undo.DestroyObjectImmediate(layoutRoot.GetChild(i).gameObject);

        Vector2Int[][] laneTurns = DefaultLaneTurns(gm.Config.map);
        foreach (SpawnSide side in (SpawnSide[])Enum.GetValues(typeof(SpawnSide)))
        {
            var laneGo = new GameObject($"Lane_{side}");
            laneGo.transform.SetParent(layoutRoot, false);
            laneGo.AddComponent<LanePath>().Setup(side);
            Vector2Int[] turns = laneTurns[(int)side];
            for (int i = 0; i < turns.Length; i++)
            {
                var p = new GameObject($"P{i}");
                p.transform.SetParent(laneGo.transform, false);
                p.transform.position = new Vector3(turns[i].x + 0.5f, turns[i].y + 0.5f, 0f);
            }
            UnityEditor.Undo.RegisterCreatedObjectUndo(laneGo, "Create map layout");
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// Draws the map border, the base and the character start so the layout can be edited against them.
    private void OnDrawGizmos()
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null || gm.Config == null) return;
        GameConfig.MapSettings m = gm.Config.map;

        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireCube(new Vector3(m.width * 0.5f, m.height * 0.5f, 0f), new Vector3(m.width, m.height, 0f));
        var baseRect = new Rect(m.baseCell.x, m.baseCell.y, m.baseSize, m.baseSize);
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(baseRect.center, new Vector3(m.baseSize, m.baseSize, 0f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(m.playerStart.x, m.playerStart.y, 0f), 0.3f);
    }
#endif

    // ---------- Cells ----------

    public int Index(Vector2Int cell) => cell.y * Width + cell.x;

    public bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;

    public Vector2Int WorldToCell(Vector2 pos) => new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));

    public Vector2Int ClampCell(Vector2Int cell) =>
        new Vector2Int(Mathf.Clamp(cell.x, 0, Width - 1), Mathf.Clamp(cell.y, 0, Height - 1));

    public Vector2 CellCenter(Vector2Int cell) => new Vector2(cell.x + 0.5f, cell.y + 0.5f);

    public bool IsBase(Vector2Int cell) => BaseCells.Contains(cell);

    public Lane LaneFor(SpawnSide side) => Lanes[(int)side];

    /// The lane running through this cell, or null.
    public Lane LaneAt(Vector2Int cell) => InBounds(cell) && laneAt[Index(cell)] > 0 ? Lanes[laneAt[Index(cell)] - 1] : null;

    /// True where a tower may stand: inside the map, not on a lane, the base or the character's start cell.
    public bool IsBuildable(Vector2Int cell) =>
        InBounds(cell) && laneAt[Index(cell)] == 0 && !IsBase(cell) && cell != WorldToCell(PlayerStart);

    /// True for an empty buildable cell.
    public bool CanBuild(Vector2Int cell) => IsBuildable(cell) && towerAt[Index(cell)] == null;

    public Tower TowerAt(Vector2Int cell) => InBounds(cell) ? towerAt[Index(cell)] : null;

    /// A living tower on this cell (destroyed towers stop blocking until reset).
    public Tower LiveTowerAt(Vector2Int cell)
    {
        Tower t = TowerAt(cell);
        return t != null && t.IsAlive() ? t : null;
    }

    /// True for base cells and cells with a living tower. Only the character and swarm monsters collide.
    public bool IsBlocked(Vector2Int cell) => InBounds(cell) && (IsBase(cell) || LiveTowerAt(cell) != null);

    /// Puts a tower on a cell (or clears it with null) and raises OnChanged.
    public void SetOccupant(Vector2Int cell, Tower tower)
    {
        towerAt[Index(cell)] = tower;
        NotifyChanged();
    }

    /// Call when a tower's blocking state changes without moving (destroyed or revived).
    public void NotifyChanged() => OnChanged?.Invoke();

    // ---------- Collision ----------

    /// Shortest distance from a point to the base's square.
    public float DistanceToBase(Vector2 pos)
    {
        Rect r = BaseRect;
        float dx = Mathf.Max(r.xMin - pos.x, 0f, pos.x - r.xMax);
        float dy = Mathf.Max(r.yMin - pos.y, 0f, pos.y - r.yMax);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// Pushes a circle out of every blocked cell it overlaps (circle-vs-cell), so it slides along
    /// towers and the base, then keeps it inside the map.
    public Vector2 ResolveCollision(Vector2 pos, float radius)
    {
        for (int pass = 0; pass < 2; pass++)
        {
            int minX = Mathf.FloorToInt(pos.x - radius), maxX = Mathf.FloorToInt(pos.x + radius);
            int minY = Mathf.FloorToInt(pos.y - radius), maxY = Mathf.FloorToInt(pos.y + radius);

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!IsBlocked(cell)) continue;

                Vector2 closest = new Vector2(Mathf.Clamp(pos.x, x, x + 1), Mathf.Clamp(pos.y, y, y + 1));
                Vector2 away = pos - closest;
                float dist = away.magnitude;
                if (dist >= radius) continue;

                if (dist > 1e-5f)
                {
                    pos = closest + away / dist * radius;
                }
                else
                {
                    // Centre is inside the cell: leave through the nearest edge.
                    float left = pos.x - x, right = x + 1 - pos.x, down = pos.y - y, up = y + 1 - pos.y;
                    float min = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
                    if (min == left) pos.x = x - radius;
                    else if (min == right) pos.x = x + 1 + radius;
                    else if (min == down) pos.y = y - radius;
                    else pos.y = y + 1 + radius;
                }
            }
        }

        pos.x = Mathf.Clamp(pos.x, radius, Width - radius);
        pos.y = Mathf.Clamp(pos.y, radius, Height - radius);
        return pos;
    }

    // ---------- Visuals ----------

    /// Recolours lanes (open/closed) for a loop. Called at loop start and when the build phase opens
    /// (showing the loop about to be played).
    public void RefreshVisuals(int loop)
    {
        WaveData wave = GameManager.Instance.Spawner.WaveFor(loop);

        var pixels = new Color[Width * Height];
        for (int i = 0; i < pixels.Length; i++)
        {
            int lane = laneAt[i];
            pixels[i] = lane == 0 ? Color.clear
                      : wave.HasConvoyOn((SpawnSide)(lane - 1)) ? laneOpenColor
                      : laneClosedColor;
        }
        overlay.SetPixels(pixels);
        overlay.Apply();
    }

    private void BuildOverlay()
    {
        overlay = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var go = new GameObject("LaneOverlay");
        go.transform.SetParent(transform, false);
        go.transform.position = Vector3.zero;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(overlay, new Rect(0, 0, Width, Height), Vector2.zero, 1f);
        sr.sortingOrder = overlaySortingOrder;
    }
}
