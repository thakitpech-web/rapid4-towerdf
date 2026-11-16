using System.Collections.Generic;
using UnityEngine;

/// Weighted flow field toward the base (GAME_DESIGN.md section 4).
/// Dijkstra from the base cells over the whole grid with a binary heap. Each cell stores its distance
/// to the base and the next cell to step to. Entering an empty or base cell costs 1, entering a tower
/// cell costs Tower.PathCost(). 8-directional; diagonals cost more and may not cut a corner past a
/// tower or the base. Monsters break through a tower when going around costs more.
/// GameManager owns it and calls Rebuild.
public class FlowField
{
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
    };

    private readonly GridMap grid;
    private readonly GameConfig.FlowFieldSettings settings;
    private readonly MinHeap heap = new MinHeap();
    private bool[] done;

    /// Cost to reach the base from each cell (index = y * Width + x).
    public float[] Distance { get; private set; }

    /// Index of the next cell toward the base, or -1 for base cells and unreachable cells.
    public int[] Next { get; private set; }

    public FlowField(GridMap grid, GameConfig config)
    {
        this.grid = grid;
        settings = config.flowField;
    }

    /// Cost of entering this cell.
    public float CellCost(Vector2Int cell)
    {
        Tower t = grid.LiveTowerAt(cell);
        return t != null ? t.PathCost() : 1f;
    }

    public void Rebuild(GridMap map)
    {
        int count = map.Width * map.Height;
        if (Distance == null || Distance.Length != count)
        {
            Distance = new float[count];
            Next = new int[count];
            done = new bool[count];
        }

        for (int i = 0; i < count; i++)
        {
            Distance[i] = float.PositiveInfinity;
            Next[i] = -1;
            done[i] = false;
        }

        heap.Clear();
        RectInt b = map.BaseCells;
        for (int y = b.yMin; y < b.yMax; y++)
        for (int x = b.xMin; x < b.xMax; x++)
        {
            int i = map.Index(new Vector2Int(x, y));
            Distance[i] = 0f;
            heap.Push(i, 0f);
        }

        // Search outward from the base. Distance[from] = cost of stepping from -> into + Distance[into].
        while (heap.Count > 0)
        {
            int into = heap.Pop(out float d);
            if (done[into]) continue;
            done[into] = true;

            Vector2Int intoCell = map.CellAt(into);
            float enterCost = CellCost(intoCell);

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int fromCell = intoCell - dir;
                if (!map.InBounds(fromCell)) continue;
                int from = map.Index(fromCell);
                if (done[from] || map.IsBase(fromCell)) continue;

                bool diagonal = dir.x != 0 && dir.y != 0;
                if (diagonal && CutsCorner(map, fromCell, dir)) continue;

                float nd = d + enterCost * (diagonal ? settings.diagonalCost : 1f);
                if (nd < Distance[from])
                {
                    Distance[from] = nd;
                    Next[from] = into;
                    heap.Push(from, nd);
                }
            }
        }
    }

    /// A diagonal step is not allowed when either orthogonal neighbour is a tower or the base.
    private static bool CutsCorner(GridMap map, Vector2Int from, Vector2Int dir) =>
        map.IsBlocked(new Vector2Int(from.x + dir.x, from.y)) || map.IsBlocked(new Vector2Int(from.x, from.y + dir.y));

    /// The cell to step to from this cell. Returns the same cell at the base or if unreachable.
    public Vector2Int NextStep(Vector2Int cell)
    {
        if (Next == null || !grid.InBounds(cell)) return cell;
        int n = Next[grid.Index(cell)];
        return n < 0 ? cell : grid.CellAt(n);
    }

    /// Flow distance from a cell to the base (infinity if unreachable or not built yet).
    public float DistanceAt(Vector2Int cell)
    {
        if (Distance == null || !grid.InBounds(cell)) return float.PositiveInfinity;
        return Distance[grid.Index(cell)];
    }

    /// Cells a monster would walk from this cell to the base (for the build-phase preview).
    public List<Vector2Int> TraceRoute(Vector2Int from)
    {
        var route = new List<Vector2Int>();
        if (Next == null || !grid.InBounds(from)) return route;

        int i = grid.Index(from);
        route.Add(from);
        while (Next[i] >= 0 && route.Count <= Next.Length)
        {
            i = Next[i];
            route.Add(grid.CellAt(i));
        }
        return route;
    }

    /// Binary min-heap of (cell index, distance). Stale entries are skipped by the caller.
    private class MinHeap
    {
        private readonly List<int> items = new List<int>();
        private readonly List<float> keys = new List<float>();

        public int Count => items.Count;

        public void Clear()
        {
            items.Clear();
            keys.Clear();
        }

        public void Push(int item, float key)
        {
            items.Add(item);
            keys.Add(key);
            int i = items.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (keys[parent] <= keys[i]) break;
                Swap(i, parent);
                i = parent;
            }
        }

        public int Pop(out float key)
        {
            int top = items[0];
            key = keys[0];

            int last = items.Count - 1;
            items[0] = items[last];
            keys[0] = keys[last];
            items.RemoveAt(last);
            keys.RemoveAt(last);

            int i = 0;
            while (true)
            {
                int l = 2 * i + 1, r = l + 1, smallest = i;
                if (l < items.Count && keys[l] < keys[smallest]) smallest = l;
                if (r < items.Count && keys[r] < keys[smallest]) smallest = r;
                if (smallest == i) break;
                Swap(i, smallest);
                i = smallest;
            }
            return top;
        }

        private void Swap(int a, int b)
        {
            (items[a], items[b]) = (items[b], items[a]);
            (keys[a], keys[b]) = (keys[b], keys[a]);
        }
    }
}
