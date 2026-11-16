using UnityEngine;

/// Survival: follows the character smoothly and shows at least ViewSize tiles, clamped to the map.
/// Build: starts fitted to the whole map; BuildManager zooms (ZoomAt) and pans (Pan) from player input.
/// GameManager decides when each mode applies.
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private Camera cam;
    private GameConfig.CameraSettings settings;
    private GridMap grid;

    public Vector2 ViewSize => settings.viewSize;
    public float FollowSharpness => settings.followSharpness;

    public void Init(GameConfig config, GridMap grid)
    {
        cam = GetComponent<Camera>();
        settings = config.cameraView;
        this.grid = grid;
    }

    /// Jumps straight to the target at the survival zoom (loop start).
    public void SnapTo(Vector2 target)
    {
        cam.orthographicSize = SurvivalSize();
        SetPosition(Clamp(target));
    }

    /// Moves toward the target at FollowSharpness per second, clamped to the map.
    public void Follow(Vector2 target, float dt)
    {
        cam.orthographicSize = SurvivalSize();   // also handles window resizes
        Vector2 pos = transform.position;
        pos = Vector2.Lerp(pos, Clamp(target), Mathf.Min(1f, FollowSharpness * dt));
        SetPosition(Clamp(pos));
    }

    /// Zooms out so the whole map is visible (build phase).
    public void FitWholeMap()
    {
        cam.orthographicSize = MaxZoomOutSize();
        SetPosition(new Vector2(grid.Width * 0.5f, grid.Height * 0.5f));
    }

    /// Closest build-phase zoom: the survival view.
    public float MinZoom => SurvivalSize();

    /// Build phase: multiplies the view size by <paramref name="factor"/> (&gt;1 zooms out), keeping the world
    /// point under <paramref name="screenPos"/> in place. Clamped between MinZoom and the whole map.
    public void ZoomAt(float factor, Vector2 screenPos)
    {
        Vector2 before = ScreenToWorld(screenPos);
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * factor, MinZoom, MaxZoomOutSize());
        Vector2 after = ScreenToWorld(screenPos);
        SetPosition(ClampBuild((Vector2)transform.position + before - after));
    }

    /// Build phase: moves the view by a world-space delta, kept over the map.
    public void Pan(Vector2 delta) => SetPosition(ClampBuild((Vector2)transform.position + delta));

    public Vector2 ScreenToWorld(Vector2 screenPos) => cam.ScreenToWorldPoint(screenPos);

    /// World units per screen pixel at the current zoom.
    public float WorldPerPixel => cam.orthographicSize * 2f / Screen.height;

    private float MaxZoomOutSize()
    {
        float pad = settings.fitPadding;
        float w = grid.Width + 2f * pad;
        float h = grid.Height + 2f * pad;
        return Mathf.Max(h * 0.5f, w * 0.5f / cam.aspect);
    }

    /// Like Clamp, but allows the fit padding around the map.
    private Vector2 ClampBuild(Vector2 pos)
    {
        float pad = settings.fitPadding;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float w = grid.Width + 2f * pad, h = grid.Height + 2f * pad;
        pos.x = w > 2f * halfW ? Mathf.Clamp(pos.x, halfW - pad, grid.Width + pad - halfW) : grid.Width * 0.5f;
        pos.y = h > 2f * halfH ? Mathf.Clamp(pos.y, halfH - pad, grid.Height + pad - halfH) : grid.Height * 0.5f;
        return pos;
    }

    /// Orthographic size that shows at least ViewSize tiles in both directions.
    private float SurvivalSize() => Mathf.Max(ViewSize.y * 0.5f, ViewSize.x * 0.5f / cam.aspect);

    /// Keeps the view inside the map (centres it on an axis where the map is smaller than the view).
    private Vector2 Clamp(Vector2 pos)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        pos.x = grid.Width > 2f * halfW ? Mathf.Clamp(pos.x, halfW, grid.Width - halfW) : grid.Width * 0.5f;
        pos.y = grid.Height > 2f * halfH ? Mathf.Clamp(pos.y, halfH, grid.Height - halfH) : grid.Height * 0.5f;
        return pos;
    }

    private void SetPosition(Vector2 pos) => transform.position = new Vector3(pos.x, pos.y, transform.position.z);
}
