using System.Collections;
using UnityEngine;

namespace MeowTactics.Utilities
{
    /// <summary>
    /// "Game juice" para unidades (gatos e inimigos):
    ///  - tranco (estica/encolhe) ao agir/levar dano,
    ///  - flash de cor ao tomar dano,
    ///  - "poof" de morte (encolhe + gira + some).
    ///
    /// IMPORTANTE: mexe SÓ em escala, cor e rotação do próprio objeto — NUNCA na
    /// posição — para não atrapalhar o movimento dos inimigos pelo caminho.
    /// </summary>
    public class JuiceVisual : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Vector3 baseScale = Vector3.one;
        private Color baseColor = Color.white;
        private bool ready;

        private float punch;   // 0..1, decai
        private float flash;   // 0..1, decai
        private bool dying;

        private static readonly Color FlashColor = new Color(1f, 0.45f, 0.4f);

        private void Start() => Capture();

        private void Capture()
        {
            if (ready) return;
            sr = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            if (baseScale == Vector3.zero) baseScale = Vector3.one;
            if (sr != null) baseColor = sr.color;
            ready = true;
        }

        /// <summary>Dá um "tranco" (escala) — use ao atacar ou levar dano.</summary>
        public void Punch(float strength = 0.35f)
        {
            Capture();
            punch = Mathf.Max(punch, strength);
        }

        /// <summary>Pisca a cor — use ao tomar dano.</summary>
        public void Flash(float strength = 1f)
        {
            Capture();
            flash = Mathf.Max(flash, Mathf.Clamp01(strength));
        }

        private void Update()
        {
            if (!ready || dying) return;

            punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 3.5f);
            float pop = 1f + punch * 0.5f;
            transform.localScale = baseScale * pop;

            if (sr != null)
            {
                flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 4f);
                sr.color = flash > 0f ? Color.Lerp(baseColor, FlashColor, flash) : baseColor;
            }
        }

        /// <summary>Toca o "poof" de morte e destrói o objeto ao final.</summary>
        public void PlayDeath()
        {
            Capture();
            if (dying) return;
            dying = true;
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            float t = 0f;
            const float dur = 0.32f;
            Vector3 from = baseScale * 1.15f;
            float startZ = transform.localEulerAngles.z;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                transform.localScale = Vector3.Lerp(from, Vector3.zero, k);
                transform.localEulerAngles = new Vector3(0, 0, startZ + k * 120f);
                if (sr != null)
                {
                    var c = baseColor; c.a = 1f - k; sr.color = c;
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
