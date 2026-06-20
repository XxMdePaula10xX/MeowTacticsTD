using UnityEngine;

namespace MeowTactics.Utilities
{
    /// <summary>
    /// Pulsa a escala/transparência de um marcador (portal de spawn, base) e,
    /// opcionalmente, gira. Dá vida aos indicadores de início e fim do caminho.
    /// </summary>
    public class MarkerPulse : MonoBehaviour
    {
        public float speed = 2f;
        public float amount = 0.12f;
        public float rotateSpeed = 0f;

        private Vector3 baseScale;
        private SpriteRenderer sr;
        private float baseAlpha = 1f;
        private float t;

        private void Start()
        {
            baseScale = transform.localScale;
            sr = GetComponent<SpriteRenderer>();
            if (sr != null) baseAlpha = sr.color.a;
            t = Random.value * 6.28f;
        }

        private void Update()
        {
            t += Time.deltaTime * speed;
            float k = 1f + Mathf.Sin(t) * amount;
            transform.localScale = baseScale * k;

            if (rotateSpeed != 0f)
                transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

            if (sr != null)
            {
                var c = sr.color;
                c.a = baseAlpha * (0.7f + 0.3f * Mathf.Sin(t));
                sr.color = c;
            }
        }
    }
}
