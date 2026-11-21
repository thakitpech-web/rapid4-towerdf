using UnityEngine;

/// Simple generated sprites for placeholder visuals (range circle, place ghost),
/// plus the sorting orders they use. Sprites are 1 world unit across.
public static class PlaceholderArt
{
    // Sorting orders: lane overlay 1, chevrons 2, towers 3 (set in their prefabs), base 3,
    // monsters 4, character 5, projectiles 6.
    public const int BuildCursorOrder = 7;

    private static Sprite square, circle, ring;

    /// Filled white square.
    public static Sprite Square => square ? square : square = Make(8, (x, y, n) => 1f);

    /// Filled white circle.
    public static Sprite Circle => circle ? circle : circle = Make(64, (x, y, n) => Disc(x, y, n, 0f));

    /// White circle outline (for range circles).
    public static Sprite Ring => ring ? ring : ring = Make(128, (x, y, n) => Disc(x, y, n, 3f));

    /// A child SpriteRenderer with the given sprite, colour, local scale and sorting order.
    public static SpriteRenderer AddChild(Transform parent, string name, Sprite sprite, Color color, float scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    // Alpha for a disc (ringWidth 0) or a ring of ringWidth pixels, with a 1-pixel soft edge.
    private static float Disc(int x, int y, int n, float ringWidth)
    {
        float r = n * 0.5f;
        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
        float outer = Mathf.Clamp01(r - d);
        if (ringWidth <= 0f) return outer;
        float inner = Mathf.Clamp01(d - (r - ringWidth));
        return Mathf.Min(outer, inner);
    }

    private static Sprite Make(int size, System.Func<int, int, int, float> alpha)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha(x, y, size));
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
