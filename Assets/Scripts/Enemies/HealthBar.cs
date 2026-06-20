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
        private SpriteRenderer fillRenderer;
        private float fullWidth = -1f;

        /// <summary>Define qual transform é o preenchimento (usado pela UnitFactory).</summary>
        public void SetFillTransform(Transform fillTransform)
        {
            fill = fillTransform;
            fullWidth = fill != null ? fill.localScale.x : 1f;
            fillRenderer = fill != null ? fill.GetComponent<SpriteRenderer>() : null;
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

            // Cor por % de vida: verde -> amarelo -> vermelho.
            if (fillRenderer != null)
            {
                Color c = ratio01 > 0.5f
                    ? Color.Lerp(new Color(0.95f, 0.85f, 0.2f), new Color(0.3f, 0.85f, 0.3f), (ratio01 - 0.5f) * 2f)
                    : Color.Lerp(new Color(0.9f, 0.25f, 0.25f), new Color(0.95f, 0.85f, 0.2f), ratio01 * 2f);
                fillRenderer.color = c;
            }
        }
    }
}
