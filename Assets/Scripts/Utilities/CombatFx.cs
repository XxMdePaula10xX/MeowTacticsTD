using System.Collections;
using UnityEngine;

namespace MeowTactics.Utilities
{
    /// <summary>
    /// Efeitos rápidos de combate (sem precisar de arte nova):
    ///  - faísca de impacto que cresce e some;
    ///  - projétil cosmético que voa do gato até o alvo e estoura numa faísca.
    /// </summary>
    public static class CombatFx
    {
        public static void Spark(Vector3 pos, Color color)
        {
            var go = new GameObject("HitSpark");
            go.transform.position = new Vector3(pos.x, pos.y, -0.1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle;
            sr.color = color;
            sr.sortingOrder = 30;
            go.transform.localScale = Vector3.one * 0.2f;
            go.AddComponent<FxRunner>().RunSpark(sr);
        }

        public static void Projectile(Vector3 from, Vector3 to, Color color)
        {
            var go = new GameObject("Projectile");
            go.transform.position = new Vector3(from.x, from.y, -0.1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle;
            sr.color = color;
            sr.sortingOrder = 28;
            go.transform.localScale = Vector3.one * 0.28f;
            go.AddComponent<FxRunner>().RunProjectile(sr, from, to, color);
        }
    }

    /// <summary>Componente interno que roda as corrotinas dos efeitos.</summary>
    public class FxRunner : MonoBehaviour
    {
        public void RunSpark(SpriteRenderer sr) => StartCoroutine(SparkRoutine(sr));
        public void RunProjectile(SpriteRenderer sr, Vector3 from, Vector3 to, Color color)
            => StartCoroutine(ProjectileRoutine(from, to, color));

        private IEnumerator SparkRoutine(SpriteRenderer sr)
        {
            float t = 0f;
            const float dur = 0.18f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.95f, k);
                if (sr != null) { var c = sr.color; c.a = 1f - k; sr.color = c; }
                yield return null;
            }
            Destroy(gameObject);
        }

        private IEnumerator ProjectileRoutine(Vector3 from, Vector3 to, Color color)
        {
            float dist = Vector3.Distance(from, to);
            float dur = Mathf.Clamp(0.06f + dist * 0.03f, 0.06f, 0.35f);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t / dur));
                yield return null;
            }
            CombatFx.Spark(to, color);
            Destroy(gameObject);
        }
    }
}
