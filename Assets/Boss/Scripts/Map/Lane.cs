using System.Collections.Generic;
using UnityEngine;

/// One lane (GAME_DESIGN.md section 3): a centre line from the map edge to the base, made of straight
/// horizontal/vertical segments, and a width in tiles. Odd widths have their centre line through cell
/// centres; even widths run along cell edges. Plain data built once by GridMap.
public class Lane
{
    public SpawnSide Side { get; }

    /// Centre-line points in world units, from the map edge to next to the base.
    public Vector2[] Points { get; }

    public int Width { get; }

    /// Every cell the lane covers (unordered). Towers cannot go here.
    public Vector2Int[] Cells { get; }

    /// Centre-line length from each point to the last one.
    private readonly float[] remaining;

    public Lane(SpawnSide side, Vector2[] points, int width)
    {
        Side = side;
        Points = points;
        Width = Mathf.Max(1, width);
        Cells = Footprint(points, Width);

        remaining = new float[points.Length];
        for (int i = points.Length - 2; i >= 0; i--)
            remaining[i] = remaining[i + 1] + Vector2.Distance(points[i], points[i + 1]);
    }

    public float TotalLength => Points.Length > 0 ? remaining[0] : 0f;

    /// Where convoy monsters of this lane spawn.
    public Vector2 Entrance() => Points.Length > 0 ? Points[0] : Vector2.zero;

    /// Centre-line distance from a position heading to Points[nextIndex] to the end of the lane.
    public float DistanceLeft(Vector2 pos, int nextIndex)
    {
        if (nextIndex >= Points.Length) return 0f;
        return Vector2.Distance(pos, Points[nextIndex]) + remaining[nextIndex];
    }

    /// Position and walking direction at a distance along the centre line.
    public void Sample(float distance, out Vector2 pos, out Vector2 dir)
    {
        pos = Entrance();
        dir = Vector2.right;
        for (int i = 0; i < Points.Length - 1; i++)
        {
            Vector2 a = Points[i], b = Points[i + 1];
            float len = Vector2.Distance(a, b);
            if (len < 1e-5f) continue;
            dir = (b - a) / len;
            if (distance <= len || i == Points.Length - 2)
            {
                pos = a + dir * Mathf.Min(distance, len);
                return;
            }
            distance -= len;
        }
    }

    /// Cells whose centre lies inside the lane: each segment widened by Width / 2 on both sides.
    /// Diagonal segments are skipped.
    public static Vector2Int[] Footprint(Vector2[] points, int width)
    {
        var cells = new HashSet<Vector2Int>();
        float half = width * 0.5f - 1e-3f;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = points[i];
            Vector2 b = i + 1 < points.Length ? points[i + 1] : a;
            if (Mathf.Abs(a.x - b.x) > 1e-3f && Mathf.Abs(a.y - b.y) > 1e-3f) continue;   // diagonal

            float minX = Mathf.Min(a.x, b.x) - half, maxX = Mathf.Max(a.x, b.x) + half;
            float minY = Mathf.Min(a.y, b.y) - half, maxY = Mathf.Max(a.y, b.y) + half;
            for (int y = Mathf.FloorToInt(minY); y <= Mathf.FloorToInt(maxY); y++)
            for (int x = Mathf.FloorToInt(minX); x <= Mathf.FloorToInt(maxX); x++)
            {
                float cx = x + 0.5f, cy = y + 0.5f;
                if (cx > minX && cx < maxX && cy > minY && cy < maxY) cells.Add(new Vector2Int(x, y));
            }
        }
        var result = new Vector2Int[cells.Count];
        cells.CopyTo(result);
        return result;
    }

    /// Where a centre-line point snaps for this width: cell centres for odd widths, cell corners for even.
    public static Vector2 Snap(Vector2 p, int width) =>
        width % 2 == 1
            ? new Vector2(Mathf.Floor(p.x) + 0.5f, Mathf.Floor(p.y) + 0.5f)
            : new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
}
