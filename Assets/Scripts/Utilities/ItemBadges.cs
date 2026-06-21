using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Cats;

namespace MeowTactics.Utilities
{
    /// <summary>
    /// Mostra ícones pequenos dos itens equipados ACIMA do gato (estilo TFT).
    /// Fica num objeto separado (não gira com o gato) e se reconstrói quando o
    /// número de itens muda. Some quando o gato está inativo (no banco).
    /// </summary>
    public class ItemBadges : MonoBehaviour
    {
        private CatUnit cat;
        private int lastCount = -1;
        private readonly List<GameObject> icons = new List<GameObject>();

        public void Setup(CatUnit c) { cat = c; }

        private void LateUpdate()
        {
            if (cat == null) { Destroy(gameObject); return; }

            var p = cat.transform.position;
            transform.position = new Vector3(p.x, p.y + 0.78f, p.z);

            if (cat.Items.Count != lastCount) Rebuild();

            bool visible = cat.gameObject.activeInHierarchy && cat.IsPlaced;
            foreach (var g in icons)
            {
                if (g == null) continue;
                var sr = g.GetComponent<SpriteRenderer>();
                if (sr != null && sr.enabled != visible) sr.enabled = visible;
            }
        }

        private void Rebuild()
        {
            foreach (var g in icons) if (g != null) Destroy(g);
            icons.Clear();
            lastCount = cat.Items.Count;

            int n = cat.Items.Count;
            const float spacing = 0.34f;
            float startX = -(n - 1) * spacing * 0.5f;
            for (int i = 0; i < n; i++)
            {
                var it = cat.Items[i];
                var go = new GameObject("ItemBadge");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(startX + i * spacing, 0f, 0f);
                go.transform.localScale = Vector3.one * 0.30f;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = it.icon != null ? it.icon : SpriteFactory.Circle;
                sr.color = it.icon != null ? Color.white : it.uiColor;
                sr.sortingOrder = 30; // acima do gato
                icons.Add(go);
            }
        }
    }
}
