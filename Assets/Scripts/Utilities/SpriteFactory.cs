using UnityEngine;

namespace MeowTactics.Utilities
{
    /// <summary>
    /// Cria sprites simples (quadrado e círculo) em tempo de execução
    /// para servir de PLACEHOLDER enquanto não há arte final.
    /// Os sprites são em branco e coloridos via SpriteRenderer.color.
    /// </summary>
    public static class SpriteFactory
    {
        private static Sprite _square;
        private static Sprite _circle;
        private static Sprite _ring;

        public static Sprite Square => _square != null ? _square : (_square = BuildSquare());
        public static Sprite Circle => _circle != null ? _circle : (_circle = BuildCircle(false));
        public static Sprite Ring => _ring != null ? _ring : (_ring = BuildCircle(true));

        private static Sprite BuildSquare()
        {
            var tex = new Texture2D(8, 8);
            var px = new Color[64];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
        }

        private static Sprite BuildCircle(bool ring)
        {
            int size = 64;
            var tex = new Texture2D(size, size);
            var px = new Color[size * size];
            float r = size / 2f;
            Vector2 c = new Vector2(r, r);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    bool inside;
                    if (ring)
                        inside = d <= r && d >= r - 3f; // anel fino (range indicator)
                    else
                        inside = d <= r - 1f;
                    px[y * size + x] = inside ? Color.white : new Color(0, 0, 0, 0);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
