using UnityEngine;
using MeowTactics.Utilities;

namespace MeowTactics.Map
{
    /// <summary>
    /// Marcador visual de INÍCIO (portal roxo girando) e FIM (cristal ciano pulsante
    /// com brilho). Construído em runtime, então combina com a arte gerada em código.
    /// </summary>
    public class Marker : MonoBehaviour
    {
        public enum Kind { Spawn, Base }
        public Kind kind = Kind.Spawn;

        private void Start()
        {
            if (kind == Kind.Spawn) BuildSpawn();
            else BuildBase();
        }

        private void BuildSpawn()
        {
            Color col = new Color(0.62f, 0.32f, 0.95f); // roxo

            var ring = Make("Ring", SpriteFactory.Ring, new Color(col.r, col.g, col.b, 0.85f), 1, 1.9f);
            var rp = ring.AddComponent<MarkerPulse>();
            rp.rotateSpeed = 50f; rp.amount = 0.1f;

            var core = Make("Core", SpriteFactory.Circle, new Color(col.r, col.g, col.b, 0.45f), 2, 1.1f);
            core.AddComponent<MarkerPulse>();
        }

        private void BuildBase()
        {
            Color col = new Color(0.40f, 0.92f, 1f); // ciano

            // Brilho suave atrás
            var glow = Make("Glow", SpriteFactory.Circle, new Color(col.r, col.g, col.b, 0.22f), 0, 2.6f);
            var gp = glow.AddComponent<MarkerPulse>();
            gp.amount = 0.18f; gp.speed = 2.5f;

            // Anel
            var ring = Make("Ring", SpriteFactory.Ring, new Color(col.r, col.g, col.b, 0.9f), 1, 2.0f);
            var rp = ring.AddComponent<MarkerPulse>();
            rp.rotateSpeed = -35f; rp.amount = 0.08f;

            // Cristal = quadrado girado 45° (losango)
            var crystal = Make("Crystal", SpriteFactory.Square, new Color(0.7f, 0.97f, 1f, 0.95f), 2, 0.9f);
            crystal.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            var cp = crystal.AddComponent<MarkerPulse>();
            cp.amount = 0.12f; cp.speed = 3f;
        }

        private GameObject Make(string name, Sprite sprite, Color color, int order, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            go.transform.localScale = Vector3.one * scale;
            return go;
        }
    }
}
