using UnityEngine;

namespace MeowTactics.Utilities
{
    /// <summary>
    /// Texto que sobe e some — usado para NÚMEROS DE DANO e ganho de MOEDAS.
    /// Usa TextMesh (espaço do mundo), então não precisa de Canvas.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        private TextMesh tm;
        private float life, maxLife;
        private Vector3 vel;

        /// <summary>Cria um texto flutuante na posição do mundo informada.</summary>
        public static void Spawn(Vector3 worldPos, string text, Color color, float size = 1f)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = worldPos + new Vector3(Random.Range(-0.2f, 0.2f), 0.45f, -0.1f);
            go.AddComponent<FloatingText>().Init(text, color, size);
        }

        private void Init(string text, Color color, float size)
        {
            tm = gameObject.AddComponent<TextMesh>();
            tm.text = text;
            tm.color = color;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 70;
            tm.characterSize = 0.1f * size;
            tm.fontStyle = FontStyle.Bold;

            var font = MeowTactics.UI.UIFactory.Font;
            if (font != null) tm.font = font;

            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (font != null) mr.sharedMaterial = font.material;
                mr.sortingOrder = 100; // acima de tudo
            }

            maxLife = life = 0.8f;
            vel = new Vector3(0f, 1.7f, 0f);
        }

        private void Update()
        {
            transform.position += vel * Time.deltaTime;
            vel *= 0.9f;
            life -= Time.deltaTime;

            float a = Mathf.Clamp01(life / maxLife);
            if (tm != null)
            {
                var c = tm.color; c.a = a; tm.color = c;
            }

            if (life <= 0f) Destroy(gameObject);
        }
    }
}
