using UnityEngine;

namespace MeowTactics.Enemies
{
    /// <summary>
    /// Barra de vida simples para inimigos.
    /// Escala um sprite "preenchimento" de 0 a 1 sobre um fundo.
    /// O prefab/objeto do inimigo é montado pela UnitFactory ou pelo gerador.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Transform fill;
        private float fullWidth = -1f;

        /// <summary>Define qual transform é o preenchimento (usado pela UnitFactory).</summary>
        public void SetFillTransform(Transform fillTransform)
        {
            fill = fillTransform;
            fullWidth = fill != null ? fill.localScale.x : 1f;
        }

        private void EnsureWidth()
        {
            if (fullWidth < 0f && fill != null)
                fullWidth = fill.localScale.x;
        }

        public void SetFill(float ratio01)
        {
            if (fill == null) return;
            EnsureWidth();
            ratio01 = Mathf.Clamp01(ratio01);
            Vector3 s = fill.localScale;
            s.x = fullWidth * ratio01;
            fill.localScale = s;
            // Mantém a barra "crescendo" a partir da esquerda.
            fill.localPosition = new Vector3(-(fullWidth - s.x) * 0.5f, fill.localPosition.y, fill.localPosition.z);
        }
    }
}
