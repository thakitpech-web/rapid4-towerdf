using System;
using UnityEngine;

/// The tile grid (GAME_DESIGN.md section 3). Cell (x, y) spans x..x+1, y..y+1 in world units.
/// Holds the base cells, the spawn ring along the map edge, the reserved ring around the base,
/// and the tower on each cell. Also does circle-vs-cell collision for the character and monsters.
/// GameManager calls Init before anything reads it.
public class GridMap : MonoBehaviour
{
    [Header("Visuals (placeholder overlay)")]
    [SerializeField] private Color spawnRingColor = new Color(0.9f, 0.25f, 0.2f, 0.25f);
    [SerializeField] private Color reservedRingColor = new Color(1f, 0.85f, 0.3f, 0.2f);
    [SerializeField] private int overlaySortingOrder = 1;

    private GameConfig.MapSettings settings;

    public int Width { get; private set; }
    public int Height { get; private set; }

    /// Tower on each cell (index = y * Width + x), or null.
    public Tower[] Occupant { get; private set; }

    public RectInt BaseCells { get; private set; }
    public Vector2 BaseCenter => BaseCells.center;
    public Rect BaseRect => new Rect(BaseCells.position, BaseCells.size);

    /// Raised when a tower is placed, moved, removed or destroyed.
    public event Action OnChanged;

    public void Init(GameConfig config)
    {
        settings = config.map;
        Width = settings.width;
        Height = settings.height;
        Occupant = new Tower[Width * Height];
        BaseCells = new RectInt(settings.baseCell, new Vector2Int(settings.baseSize, settings.baseSize));
        BuildOverlay();
    }

    // ---------- Cells ----------

    public int Index(Vector2Int cell) => cell.y * Width + cell.x;
    public Vector2Int CellAt(int index) => new Vector2Int(index % Width, index / Width);

    public bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;

    public Vector2Int WorldToCell(Vector2 pos) => new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));

    public Vector2Int ClampCell(Vector2Int cell) =>
        new Vector2Int(Mathf.Clamp(cell.x, 0, Width - 1), Mathf.Clamp(cell.y, 0, Height - 1));

    public Vector2 CellCenter(Vector2Int cell) => new Vector2(cell.x + 0.5f, cell.y + 0.5f);

    public bool IsBase(Vector2Int cell) => BaseCells.Contains(cell);

    /// The border ring where monsters spawn.
    public bool IsSpawnRing(Vector2Int cell)
    {
        int r = settings.spawnRing;
        return cell.x < r || cell.y < r || cell.x >= Width - r || cell.y >= Height - r;
    }

    /// The ring around the base where the character starts and monsters reach the base.
    public bool IsReserved(Vector2Int cell)
    {
        int r = settings.reservedRing;
        RectInt grown = new RectInt(BaseCells.xMin - r, BaseCells.yMin - r, BaseCells.width + 2 * r, BaseCells.height + 2 * r);
        return grown.Contains(cell) && !IsBase(cell);
    }

    public Tower TowerAt(Vector2Int cell) => InBounds(cell) ? Occupant[Index(cell)] : null;

    /// A living tower on this cell (destroyed towers stop blocking until reset).
    public Tower LiveTowerAt(Vector2Int cell)
    {
        Tower t = TowerAt(cell);
        return t != null && t.IsAlive() ? t : null;
    }

    /// True for an empty cell where a tower may be built.
    public bool CanBuild(Vector2Int cell) =>
        InBounds(cell) && !IsSpawnRing(cell) && !IsReserved(cell) && !IsBase(cell) && TowerAt(cell) == null;

    /// True if the base or a living tower fills this cell.
    public bool IsBlocked(Vector2Int cell) => InBounds(cell) && (IsBase(cell) || LiveTowerAt(cell) != null);

    /// The unit filling a blocked cell (the base or a living tower), or null.
    public Unit BlockerAt(Vector2Int cell)
    {
        if (!InBounds(cell)) return null;
        if (IsBase(cell)) return GameManager.Instance.Base;
        return LiveTowerAt(cell);
    }

    /// Puts a tower on a cell (or clears it with null) and raises OnChanged.
    public void SetOccupant(Vector2Int cell, Tower tower)
    {
        Occupant[Index(cell)] = tower;
        NotifyChanged();
    }

    /// Call when a tower's blocking state changes without moving (destroyed or revived).
    public void NotifyChanged() => OnChanged?.Invoke();

    // ---------- Collision ----------

    /// Shortest distance from a point to a cell's square (0 if inside).
    public static float DistanceToCell(Vector2 pos, Vector2Int cell)
    {
        float dx = Mathf.Max(cell.x - pos.x, 0f, pos.x - (cell.x + 1));
        float dy = Mathf.Max(cell.y - pos.y, 0f, pos.y - (cell.y + 1));
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// Shortest distance from a point to the base's square.
    public float DistanceToBase(Vector2 pos)
    {
        Rect r = BaseRect;
        float dx = Mathf.Max(r.xMin - pos.x, 0f, pos.x - r.xMax);
        float dy = Mathf.Max(r.yMin - pos.y, 0f, pos.y - r.yMax);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// Pushes a circle out of every blocked cell it overlaps (circle-vs-cell),
    /// then keeps it inside the map.
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
                    float m = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
                    if (m == left) pos.x = x - radius;
                    else if (m == right) pos.x = x + 1 + radius;
                    else if (m == down) pos.y = y - radius;
                    else pos.y = y + 1 + radius;
                }
            }
        }

        pos.x = Mathf.Clamp(pos.x, radius, Width - radius);
        pos.y = Mathf.Clamp(pos.y, radius, Height - radius);
        return pos;
    }

    // ---------- Visuals ----------

    /// One pixel per cell, tinted for the spawn ring and the reserved ring.
    private void BuildOverlay()
    {
        var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[Width * Height];
        for (int i = 0; i < pixels.Length; i++)
        {
            Vector2Int cell = CellAt(i);
            pixels[i] = IsSpawnRing(cell) ? spawnRingColor
                      : IsReserved(cell) ? reservedRingColor
                      : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();

        var overlay = new GameObject("GridOverlay");
        overlay.transform.SetParent(transform, false);
        overlay.transform.position = Vector3.zero;
        var sr = overlay.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, Width, Height), Vector2.zero, 1f);
        sr.sortingOrder = overlaySortingOrder;
    }
}
