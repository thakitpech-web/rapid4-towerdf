using UnityEngine;

/// One lane drawn in the scene. Its children, in Hierarchy order, are the centre-line turn points from the
/// map edge to next to the base. Points snap to cell centres for an odd width and to cell corners for an
/// even width. Consecutive points must share a row or a column (a diagonal segment is drawn red and ignored).
/// GridMap reads this at play start.
/// Setup: an object with this script per side (Left, Top, Right, Bottom) under GridMap's Layout Root,
/// with one empty child per turn point.
public class LanePath : MonoBehaviour
{
    [SerializeField] private SpawnSide side;
    [Tooltip("Lane width in tiles")]
    [SerializeField, Min(1)] private int width = 1;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0.15f, 0.9f);

    public SpawnSide Side => side;
    public int Width => width;

    /// Used by GridMap's "Create Layout From GameConfig" menu.
    public void Setup(SpawnSide laneSide) => side = laneSide;

    /// Centre-line points (snapped), in Hierarchy order.
    public Vector2[] Points()
    {
        var points = new Vector2[transform.childCount];
        for (int i = 0; i < points.Length; i++)
            points[i] = Lane.Snap(transform.GetChild(i).position, width);
        return points;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            foreach (Transform child in transform)
            {
                Vector2 s = Lane.Snap(child.position, width);
                if (((Vector2)child.position - s).sqrMagnitude > 1e-8f) child.position = s;
            }

        Vector2[] points = Points();
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.25f);
        foreach (Vector2Int c in Lane.Footprint(points, width))
            Gizmos.DrawCube(new Vector3(c.x + 0.5f, c.y + 0.5f, 0f), new Vector3(1f, 1f, 0.01f));

        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.color = i == 0 ? Color.green : gizmoColor;
            Gizmos.DrawSphere(points[i], i == 0 ? 0.3f : 0.18f);
            if (i == 0) continue;

            Vector2 a = points[i - 1], b = points[i];
            bool diagonal = Mathf.Abs(a.x - b.x) > 1e-3f && Mathf.Abs(a.y - b.y) > 1e-3f;
            Gizmos.color = diagonal ? Color.red : gizmoColor;
            Gizmos.DrawLine(a, b);
        }
        UnityEditor.Handles.Label(points.Length > 0 ? (Vector3)(points[0] + new Vector2(0f, 0.8f)) : transform.position,
                                  $"Lane {side} (width {width})");
    }
#endif
}
