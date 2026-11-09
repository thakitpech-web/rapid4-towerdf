using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The single enemy path, as a polyline of waypoints in tile units (1 tile = 1 Unity unit).
/// Enemies store a distance travelled along it and ask for their position with PositionAt.
/// </summary>
public class Path : MonoBehaviour
{
    [Tooltip("Waypoints in world tile units, from the spawn to the end next to the base.")]
    [SerializeField] private List<Vector2> waypoints = new List<Vector2>();

    [Header("Scene view gizmo")]
    [SerializeField] private Color gizmoColor = Color.yellow;

    // cumulative[i] = distance from the first waypoint to waypoint i
    private float[] cumulative;

    public IReadOnlyList<Vector2> Waypoints => waypoints;

    public float Length
    {
        get
        {
            EnsureCache();
            return cumulative.Length == 0 ? 0f : cumulative[cumulative.Length - 1];
        }
    }

    private void Awake() => RebuildCache();

    private void OnValidate() => RebuildCache();

    /// <summary>World position after travelling <paramref name="distance"/> tiles along the path.</summary>
    public Vector2 PositionAt(float distance)
    {
        EnsureCache();
        if (waypoints.Count == 0) return transform.position;
        if (distance <= 0f || waypoints.Count == 1) return waypoints[0];
        if (distance >= Length) return waypoints[waypoints.Count - 1];

        for (int i = 1; i < waypoints.Count; i++)
        {
            if (distance <= cumulative[i])
            {
                float segLength = cumulative[i] - cumulative[i - 1];
                float t = segLength > 0f ? (distance - cumulative[i - 1]) / segLength : 0f;
                return Vector2.Lerp(waypoints[i - 1], waypoints[i], t);
            }
        }
        return waypoints[waypoints.Count - 1];
    }

    /// <summary>Unit direction of the path segment at <paramref name="distance"/>.</summary>
    public Vector2 DirectionAt(float distance)
    {
        EnsureCache();
        if (waypoints.Count < 2) return Vector2.right;

        int i = 1;
        while (i < waypoints.Count - 1 && distance > cumulative[i]) i++;
        return (waypoints[i] - waypoints[i - 1]).normalized;
    }

    /// <summary>True when an enemy that has travelled this far has reached the base end of the path.</summary>
    public bool IsEnd(float distance) => distance >= Length;

    private void EnsureCache()
    {
        if (cumulative == null || cumulative.Length != waypoints.Count) RebuildCache();
    }

    private void RebuildCache()
    {
        cumulative = new float[waypoints.Count];
        for (int i = 1; i < waypoints.Count; i++)
            cumulative[i] = cumulative[i - 1] + Vector2.Distance(waypoints[i - 1], waypoints[i]);
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        Gizmos.color = gizmoColor;
        for (int i = 0; i < waypoints.Count; i++)
        {
            Gizmos.DrawWireSphere(waypoints[i], 0.15f);
            if (i > 0) Gizmos.DrawLine(waypoints[i - 1], waypoints[i]);
        }
    }
}
