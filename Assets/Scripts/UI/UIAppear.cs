using UnityEngine;

namespace MeowTactics.UI
{
    /// <summary>
    /// Pequena animação de surgimento para telas/modais: fade-in + leve "pop" de
    /// escala (0.96 → 1). Roda em tempo real (unscaledDeltaTime), então funciona
    /// mesmo com o jogo pausado. Dispara sempre que o objeto é (re)ativado.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIAppear : MonoBehaviour
    {
        public float duration = 0.16f;
        public float startScale = 0.96f;

        private CanvasGroup group;
        private RectTransform rt;
        private float t;
        private bool animating;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rt = transform as RectTransform;
        }

        private void OnEnable()
        {
            t = 0f;
            animating = true;
            if (group != null) group.alpha = 0f;
            Apply(0f);
        }

        private void Update()
        {
            if (!animating) return;
            t += Time.unscaledDeltaTime;
            float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            // ease-out suave
            float e = 1f - (1f - k) * (1f - k);
            Apply(e);
            if (k >= 1f) animating = false;
        }

        private void Apply(float e)
        {
            if (group != null) group.alpha = e;
            if (rt != null)
            {
                float s = Mathf.Lerp(startScale, 1f, e);
                rt.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
