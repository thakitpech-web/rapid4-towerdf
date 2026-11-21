using UnityEngine;

/// Survival: a fixed view that fits the whole map (no following).
/// Build: starts fitted to the whole map; BuildManager zooms (ZoomAt) and pans (Pan) from player input.
/// GameManager decides when each mode applies.
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private Camera cam;
    private GameConfig.CameraSettings settings;
    private GridMap grid;

    public void Init(GameConfig config, GridMap grid)
    {
        cam = GetComponent<Camera>();
        settings = config.cameraView;
        this.grid = grid;
    }

    /// Shows the whole map, centred (survival every frame, and the start of the build phase).
    public void FitWholeMap()
    {
        cam.orthographicSize = MaxZoomOutSize();
        SetPosition(new Vector2(grid.Width * 0.5f, grid.Height * 0.5f));
    }

    /// Closest build-phase zoom: shows at least BuildMinView tiles.
    public float MinZoom => Mathf.Max(settings.buildMinView.y * 0.5f, settings.buildMinView.x * 0.5f / cam.aspect);

    /// Build phase: multiplies the view size by <paramref name="factor"/> (&gt;1 zooms out), keeping the world
    /// point under <paramref name="screenPos"/> in place. Clamped between MinZoom and the whole map.
    public void ZoomAt(float factor, Vector2 screenPos)
    {
        Vector2 before = ScreenToWorld(screenPos);
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * factor, Mathf.Min(MinZoom, MaxZoomOutSize()),
                                           MaxZoomOutSize());
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
        return settings.fillScreen
            ? Mathf.Min(h * 0.5f, w * 0.5f / cam.aspect)    // fill: crop what does not fit
            : Mathf.Max(h * 0.5f, w * 0.5f / cam.aspect);   // fit: show everything
    }

    /// Keeps the view over the map plus the fit padding (centres it on an axis where the view is larger).
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

    private void SetPosition(Vector2 pos) => transform.position = new Vector3(pos.x, pos.y, transform.position.z);
}
