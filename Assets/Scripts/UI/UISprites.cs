using UnityEngine;

namespace MeowTactics.UI
{
    /// <summary>
    /// Sprites de UI gerados em código (sem precisar de arte). Servem de base para
    /// a identidade visual "dark fantasy cute": cantos arredondados, vidro/gradiente,
    /// brilho (glow) e discos suaves para as bases dos gatos no tabuleiro.
    /// Todos são brancos e tingidos pela cor do Image/SpriteRenderer.
    /// </summary>
    public static class UISprites
    {
        private static Sprite _rounded;
        private static Sprite _sheen;
        private static Sprite _glow;
        private static Sprite _disc;

        /// <summary>Retângulo arredondado (9-slice) — painéis e botões.</summary>
        public static Sprite Rounded => _rounded != null ? _rounded : (_rounded = BuildRounded());

        /// <summary>Gradiente vertical (claro em cima → transparente embaixo) p/ efeito de vidro.</summary>
        public static Sprite Sheen => _sheen != null ? _sheen : (_sheen = BuildSheen());

        /// <summary>Brilho radial suave (centro opaco → bordas transparentes).</summary>
        public static Sprite Glow => _glow != null ? _glow : (_glow = BuildGlow());

        /// <summary>Disco cheio com borda suave (base do gato no tabuleiro).</summary>
        public static Sprite Disc => _disc != null ? _disc : (_disc = BuildDisc());

        // ---------------------------------------------------------------
        private static Sprite BuildRounded()
        {
            const int size = 64;
            const float r = 20f; // raio do canto
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = new Color(1f, 1f, 1f, RoundedAlpha(x + 0.5f, y + 0.5f, size, r));
            tex.SetPixels(px);
            tex.Apply();

            var border = new Vector4(r, r, r, r);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
        }

        private static float RoundedAlpha(float x, float y, int size, float r)
        {
            float dx = Mathf.Max(0f, Mathf.Max(r - x, x - (size - r)));
            float dy = Mathf.Max(0f, Mathf.Max(r - y, y - (size - r)));
            if (dx <= 0f || dy <= 0f) return 1f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(r - dist + 0.5f);
        }

        // Gradiente vertical: alpha alto no topo, 0 embaixo (usado como brilho de vidro).
        private static Sprite BuildSheen()
        {
            const int w = 8, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);          // 0 embaixo, 1 no topo
                float a = Mathf.SmoothStep(0f, 1f, t); // mais forte perto do topo
                for (int x = 0; x < w; x++) px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            // 9-slice só na horizontal (colunas idênticas), preserva o gradiente vertical.
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(2, 0, 2, 0));
        }

        private static Sprite BuildGlow()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[size * size];
            float c = (size - 1) / 2f;
            float max = c;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / max;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a; // queda mais suave nas bordas
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // Disco cheio com 1px de antialias na borda (base do gato).
        private static Sprite BuildDisc()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[size * size];
            float c = (size - 1) / 2f;
            float r = c - 1f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
