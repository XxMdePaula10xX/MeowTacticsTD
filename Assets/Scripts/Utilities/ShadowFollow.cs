using UnityEngine;

namespace MeowTactics.Utilities
{
    /// <summary>
    /// Sombrinha que segue a unidade (gato/inimigo) por baixo. Fica num objeto
    /// SEPARADO (não filho) para não girar junto quando o gato vira para mirar.
    /// Some sozinha quando a unidade é destruída.
    /// </summary>
    public class ShadowFollow : MonoBehaviour
    {
        private Transform target;
        private float offsetY;
        private SpriteRenderer sr;

        public void Setup(Transform follow, float yOffset)
        {
            target = follow;
            offsetY = yOffset;
            sr = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (target == null) { Destroy(gameObject); return; }

            // Esconde a sombra quando a unidade está inativa (ex.: gato no banco).
            bool visible = target.gameObject.activeInHierarchy;
            if (sr != null && sr.enabled != visible) sr.enabled = visible;
            if (!visible) return;

            Vector3 p = target.position;
            transform.position = new Vector3(p.x, p.y + offsetY, p.z + 0.01f);
        }
    }
}
