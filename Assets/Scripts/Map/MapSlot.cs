using UnityEngine;
using UnityEngine.EventSystems;
using MeowTactics.Cats;
using MeowTactics.Utilities;

namespace MeowTactics.Map
{
    /// <summary>
    /// Um ponto fixo do mapa onde um gato pode ser posicionado.
    /// Aceita apenas 1 gato. Clicável (toque) para posicionar/selecionar.
    /// Desenha a si mesmo (quadrado translúcido) em runtime.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MapSlot : MonoBehaviour
    {
        public CatUnit Occupant { get; private set; }
        public bool IsEmpty => Occupant == null;

        private SpriteRenderer sprite;
        private static readonly Color EmptyColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color FullColor  = new Color(1f, 1f, 1f, 0.05f);

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            if (sprite == null) sprite = gameObject.AddComponent<SpriteRenderer>();
            if (sprite.sprite == null) sprite.sprite = SpriteFactory.Square;
            sprite.sortingOrder = 1;
            sprite.color = EmptyColor;
        }

        public void SetOccupant(CatUnit cat)
        {
            Occupant = cat;
            if (cat != null)
            {
                cat.transform.position = transform.position;
                cat.CurrentSlot = this;
            }
            RefreshColor();
        }

        public void Clear()
        {
            if (Occupant != null) Occupant.CurrentSlot = null;
            Occupant = null;
            RefreshColor();
        }

        private void RefreshColor()
        {
            if (sprite != null) sprite.color = IsEmpty ? EmptyColor : FullColor;
        }

        private void OnMouseDown()
        {
            // Ignora o clique se o toque estiver sobre algum elemento de UI
            // (loja, banco, painéis, roleta de itens, telas de fim de jogo).
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            Managers.PlacementManager.Instance?.OnSlotClicked(this);
        }
    }
}
