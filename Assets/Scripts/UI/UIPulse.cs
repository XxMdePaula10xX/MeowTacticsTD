using UnityEngine;

namespace MeowTactics.UI
{
    /// <summary>
    /// Pulso suave de escala para chamar atenção (ex: botão "Iniciar Onda" na
    /// fase de preparação). Usa tempo NÃO escalado para funcionar mesmo pausado.
    /// </summary>
    public class UIPulse : MonoBehaviour
    {
        public float speed = 3f;
        public float amount = 0.05f;
        public bool active = true;

        private Vector3 baseScale = Vector3.one;
        private bool captured;
        private float t;

        private void OnEnable()
        {
            if (!captured) { baseScale = transform.localScale; captured = true; }
        }

        private void Update()
        {
            if (!active)
            {
                transform.localScale = baseScale;
                return;
            }
            t += Time.unscaledDeltaTime * speed;
            transform.localScale = baseScale * (1f + Mathf.Sin(t) * amount);
        }
    }
}
