using UnityEngine;
using UnityEngine.UI;

/// Minimap during survival: base, towers, monsters, character, open spawn sides and the camera rectangle.
/// Redrawn into a small texture every frame and shown on a RawImage.
/// Setup: put this on a UI object that stays active, with a RawImage (any size, square) as Image.
public class MinimapView : MonoBehaviour
{
    [Tooltip("RawImage that shows the map. Hidden outside survival and level-up.")]
    [SerializeField] private RawImage image;
    [Tooltip("Texture pixels per map cell")]
    [SerializeField, Min(1)] private int pixelsPerCell = 4;

    [Header("Colours")]
    [SerializeField] private Color32 ground = new Color32(25, 40, 30, 210);
    [SerializeField] private Color32 closedEdge = new Color32(45, 55, 50, 230);
    [SerializeField] private Color32 openEdge = new Color32(200, 70, 60, 255);
    [SerializeField] private Color32 baseColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color32 monsterColor = new Color32(255, 70, 70, 255);
    [SerializeField] private Color32 playerColor = new Color32(255, 230, 60, 255);
    [SerializeField] private Color32 viewColor = new Color32(255, 255, 255, 160);
    [SerializeField] private Color32 deadTowerColor = new Color32(90, 90, 90, 255);

    private Texture2D tex;
    private Color32[] pixels;
    private int w, h;

    private void LateUpdate()
    {
        if (image == null) return;

        GameManager gm = GameManager.Instance;
        bool show = gm != null && (gm.Phase == GamePhase.Survival || gm.Phase == GamePhase.LevelUp);
        image.enabled = show;
        if (!show) return;

        EnsureTexture(gm.Grid);
        Draw(gm);
        tex.SetPixels32(pixels);
        tex.Apply(false);
    }

    private void EnsureTexture(GridMap grid)
    {
        if (tex != null) return;
        w = grid.Width * pixelsPerCell;
        h = grid.Height * pixelsPerCell;
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        pixels = new Color32[w * h];
        image.texture = tex;
    }

    private void Draw(GameManager gm)
    {
        GridMap grid = gm.Grid;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = ground;

        // Spawn ring: open sides this loop in openEdge.
        bool left = false, right = false, top = false, bottom = false;
        foreach (SpawnSide s in gm.Spawner.SidesFor(gm.CurrentLoop))
        {
            if (s == SpawnSide.Left) left = true;
            else if (s == SpawnSide.Right) right = true;
            else if (s == SpawnSide.Top) top = true;
            else bottom = true;
        }
        for (int x = 0; x < grid.Width; x++)
        {
            FillCell(x, 0, bottom ? openEdge : closedEdge);
            FillCell(x, grid.Height - 1, top ? openEdge : closedEdge);
        }
        for (int y = 1; y < grid.Height - 1; y++)
        {
            FillCell(0, y, left ? openEdge : closedEdge);
            FillCell(grid.Width - 1, y, right ? openEdge : closedEdge);
        }

        foreach (Tower t in gm.Builder.Towers)
            FillCell(t.Cell.x, t.Cell.y, t.IsAlive() ? (Color32)t.Data.Color : deadTowerColor);

        foreach (Vector2Int c in gm.Base.Cells) FillCell(c.x, c.y, baseColor);

        foreach (Enemy e in Enemy.Active)
            if (e.IsAlive()) Dot(e.Position, 1, monsterColor);

        Dot(gm.Player.Position, 2, playerColor);
        DrawViewRect();
    }

    private void DrawViewRect()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        Vector2 c = cam.transform.position;
        int x0 = ToPx(c.x - halfW), x1 = ToPx(c.x + halfW) - 1;
        int y0 = ToPx(c.y - halfH), y1 = ToPx(c.y + halfH) - 1;
        for (int x = x0; x <= x1; x++) { Set(x, y0, viewColor); Set(x, y1, viewColor); }
        for (int y = y0; y <= y1; y++) { Set(x0, y, viewColor); Set(x1, y, viewColor); }
    }

    private int ToPx(float world) => Mathf.RoundToInt(world * pixelsPerCell);

    private void FillCell(int cx, int cy, Color32 color)
    {
        for (int y = 0; y < pixelsPerCell; y++)
        for (int x = 0; x < pixelsPerCell; x++)
            Set(cx * pixelsPerCell + x, cy * pixelsPerCell + y, color);
    }

    /// A square dot of (2 × radius) pixels centred on a world position.
    private void Dot(Vector2 world, int radius, Color32 color)
    {
        int px = ToPx(world.x), py = ToPx(world.y);
        for (int y = py - radius; y < py + radius; y++)
        for (int x = px - radius; x < px + radius; x++)
            Set(x, y, color);
    }

    private void Set(int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= w || y >= h) return;
        pixels[y * w + x] = color;
    }

    private void OnDestroy()
    {
        if (tex != null) Destroy(tex);
    }
}
