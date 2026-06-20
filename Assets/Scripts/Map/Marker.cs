using UnityEngine;
using MeowTactics.Utilities;

namespace MeowTactics.Map
{
    /// <summary>
    /// Marcador visual de INÍCIO (portal) e FIM (cristal) do caminho.
    /// Constrói o visual em runtime (anel + núcleo pulsando), então não depende
    /// de sprites salvos — combina com o resto da arte gerada em código.
    /// </summary>
    public class Marker : MonoBehaviour
    {
        public enum Kind { Spawn, Base }
        public Kind kind = Kind.Spawn;

        private void Start()
        {
            Color col = kind == Kind.Spawn
                ? new Color(0.62f, 0.32f, 0.95f)   // roxo — portal de spawn
                : new Color(0.40f, 0.92f, 1f);     // ciano — base/cristal

            var ring = new GameObject("Ring");
            ring.transform.SetParent(transform, false);
            var rsr = ring.AddComponent<SpriteRenderer>();
            rsr.sprite = SpriteFactory.Ring;
            rsr.color = new Color(col.r, col.g, col.b, 0.85f);
            rsr.sortingOrder = 1;
            ring.transform.localScale = Vector3.one * 1.9f;
            var rp = ring.AddComponent<MarkerPulse>();
            rp.rotateSpeed = kind == Kind.Spawn ? 45f : -30f;
            rp.amount = 0.10f;

            var core = new GameObject("Core");
            core.transform.SetParent(transform, false);
            var csr = core.AddComponent<SpriteRenderer>();
            csr.sprite = SpriteFactory.Circle;
            csr.color = new Color(col.r, col.g, col.b, 0.5f);
            csr.sortingOrder = 2;
            core.transform.localScale = Vector3.one * 1.15f;
            core.AddComponent<MarkerPulse>();
        }
    }
}
