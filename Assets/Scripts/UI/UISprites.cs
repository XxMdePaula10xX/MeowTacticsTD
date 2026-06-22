using UnityEngine;

namespace MeowTactics.UI
{
    /// <summary>
    /// Sprites de UI gerados em código (sem precisar de arte). Hoje: um retângulo
    /// ARREDONDADO branco com borda de 9-slice, que tingido pela cor do Image vira
    /// painéis/botões arredondados de qualquer cor e tamanho.
    /// </summary>
    public static class UISprites
    {
        private static Sprite _rounded;
        public static Sprite Rounded => _rounded != null ? _rounded : (_rounded = BuildRounded());

        private static Sprite BuildRounded()
        {
            const int size = 64;
            const float r = 20f; // raio do canto
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = RoundedAlpha(x + 0.5f, y + 0.5f, size, r);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();

            var border = new Vector4(r, r, r, r); // 9-slice: mantém os cantos arredondados
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
        }

        // 1 dentro, 0 fora; suaviza ~1px só nos cantos (lados retos ficam cheios).
        private static float RoundedAlpha(float x, float y, int size, float r)
        {
            float dx = Mathf.Max(0f, Mathf.Max(r - x, x - (size - r)));
            float dy = Mathf.Max(0f, Mathf.Max(r - y, y - (size - r)));
            if (dx <= 0f || dy <= 0f) return 1f; // lado reto ou centro
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(r - dist + 0.5f);
        }
    }
}
