using System.Collections;
using UnityEngine;
using MeowTactics.Enemies;

namespace MeowTactics.Utilities
{
    /// <summary>
    /// Efeitos de combate (sem arte nova):
    ///  - Projectile: projétil que SEGUE o alvo e estoura numa faísca ao chegar;
    ///  - Melee: corte rápido perto do alvo;
    ///  - Magic: estouro mágico (anel) no alvo / área;
    ///  - Spark: faísca de impacto.
    /// </summary>
    public static class CombatFx
    {
        public static void Spark(Vector3 pos, Color color)
        {
            var sr = NewFx("HitSpark", pos, SpriteFactory.Circle, color, 30, 0.2f);
            sr.gameObject.AddComponent<FxRunner>().RunBurst(sr, 0.2f, 0.95f, 0.18f);
        }

        /// <summary>Corte corpo a corpo: um anel branco rápido e curto.</summary>
        public static void Melee(Vector3 pos, Color color)
        {
            var sr = NewFx("MeleeSlash", pos, SpriteFactory.Ring, Color.Lerp(color, Color.white, 0.6f), 30, 0.3f);
            sr.gameObject.AddComponent<FxRunner>().RunBurst(sr, 0.3f, 1.5f, 0.14f);
        }

        /// <summary>Estouro mágico: anel colorido que cresce (raio = área do feitiço).</summary>
        public static void Magic(Vector3 pos, Color color, float radius)
        {
            float max = Mathf.Max(1.2f, radius * 2f);
            var sr = NewFx("MagicBurst", pos, SpriteFactory.Ring, color, 31, 0.3f);
            sr.gameObject.AddComponent<FxRunner>().RunBurst(sr, 0.3f, max, 0.3f);
        }

        /// <summary>Projétil cosmético que persegue o alvo; se ele morrer, vai até a última posição.</summary>
        public static void Projectile(Vector3 from, EnemyUnit target, Vector3 fallback, Color color)
        {
            var sr = NewFx("Projectile", from, SpriteFactory.Circle, color, 28, 0.28f);
            sr.gameObject.AddComponent<FxRunner>().RunProjectile(target, fallback, color);
        }

        private static SpriteRenderer NewFx(string name, Vector3 pos, Sprite sprite, Color color, int order, float scale)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, pos.y, -0.1f);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }

    /// <summary>Componente interno que roda as corrotinas dos efeitos.</summary>
    public class FxRunner : MonoBehaviour
    {
        public void RunBurst(SpriteRenderer sr, float fromScale, float toScale, float dur)
            => StartCoroutine(Burst(sr, fromScale, toScale, dur));

        public void RunProjectile(EnemyUnit target, Vector3 fallback, Color color)
            => StartCoroutine(ProjectileRoutine(target, fallback, color));

        private IEnumerator Burst(SpriteRenderer sr, float fromScale, float toScale, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                transform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, k);
                if (sr != null) { var c = sr.color; c.a = 1f - k; sr.color = c; }
                yield return null;
            }
            Destroy(gameObject);
        }

        private IEnumerator ProjectileRoutine(EnemyUnit target, Vector3 fallback, Color color)
        {
            const float speed = 16f; // unidades por segundo
            Vector3 to = fallback;
            float safety = 2f; // tempo máximo de voo
            while (safety > 0f)
            {
                safety -= Time.deltaTime;
                if (target != null && target.IsAlive) to = target.transform.position;
                transform.position = Vector3.MoveTowards(transform.position, to, speed * Time.deltaTime);
                if (Vector3.Distance(transform.position, to) <= 0.08f) break;
                yield return null;
            }
            CombatFx.Spark(to, color);
            Destroy(gameObject);
        }
    }
}
