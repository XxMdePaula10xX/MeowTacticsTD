using System;
using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Data;
using MeowTactics.Cats;
using MeowTactics.UI;

namespace MeowTactics.Managers
{
    /// <summary>
    /// Guarda os itens que o jogador possui (mas ainda não equipou) e
    /// controla o fluxo de equipar um item em um gato.
    /// </summary>
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }

        [Tooltip("Todos os itens possíveis (pool de onde a roleta sorteia)")]
        public List<ItemData> allItems = new List<ItemData>();

        /// <summary>Itens que o jogador ganhou e ainda não equipou.</summary>
        public readonly List<ItemData> inventory = new List<ItemData>();

        /// <summary>Item selecionado para equipar no próximo gato tocado (ou null).</summary>
        public ItemData SelectedForEquip { get; private set; }

        public event Action OnInventoryChanged;
        public event Action OnSelectionChanged;

        private void Awake()
        {
            Instance = this;
        }

        public void AddToInventory(ItemData item)
        {
            if (item == null) return;
            inventory.Add(item);
            OnInventoryChanged?.Invoke();
        }

        public void ReturnItems(List<ItemData> items)
        {
            if (items == null || items.Count == 0) return;
            inventory.AddRange(items);
            OnInventoryChanged?.Invoke();
        }

        public void SelectForEquip(ItemData item)
        {
            SelectedForEquip = item;
            OnSelectionChanged?.Invoke();
        }

        public void ClearSelection()
        {
            SelectedForEquip = null;
            OnSelectionChanged?.Invoke();
        }

        /// <summary>Tenta equipar o item selecionado no gato. Retorna true se conseguiu.</summary>
        public bool TryEquipSelectedOn(CatUnit cat)
        {
            if (SelectedForEquip == null || cat == null) return false;
            if (!cat.CanEquipMore)
            {
                UIManager.Instance?.ShowMessage("Este gato já tem o máximo de itens!");
                return false;
            }

            cat.EquipItem(SelectedForEquip);
            inventory.Remove(SelectedForEquip);
            ClearSelection();
            OnInventoryChanged?.Invoke();

            SynergyManager.Instance?.RecalculateSynergies();
            return true;
        }

        /// <summary>Sorteia N itens distintos do pool para a roleta de escolha.</summary>
        public List<ItemData> GetRandomChoices(int n)
        {
            var pool = new List<ItemData>(allItems);
            var result = new List<ItemData>();
            for (int i = 0; i < n && pool.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
