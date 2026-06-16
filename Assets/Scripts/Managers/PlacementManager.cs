using System;
using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Cats;
using MeowTactics.Map;
using MeowTactics.Data;
using MeowTactics.UI;

namespace MeowTactics.Managers
{
    /// <summary>
    /// Controla a seleção e o posicionamento de gatos nos slots do mapa,
    /// o equipamento de itens por toque e a venda de gatos.
    /// </summary>
    public class PlacementManager : MonoBehaviour
    {
        public static PlacementManager Instance { get; private set; }

        /// <summary>Gato do banco selecionado para ser posicionado.</summary>
        public CatUnit SelectedBenchCat { get; private set; }
        /// <summary>Gato posicionado atualmente em foco (para mostrar alcance/detalhes).</summary>
        public CatUnit FocusedCat { get; private set; }

        public event Action OnSelectionChanged;

        private void Awake()
        {
            Instance = this;
        }

        private bool CanEditBoard =>
            GameManager.Instance == null || GameManager.Instance.State != GameState.WaveInProgress;

        // ---------- Seleção a partir do banco ----------
        public void SelectBenchCatForPlacement(CatUnit cat)
        {
            ClearFocus();
            SelectedBenchCat = cat;
            ItemManager.Instance?.ClearSelection();
            OnSelectionChanged?.Invoke();
        }

        public void ClearSelection()
        {
            SelectedBenchCat = null;
            OnSelectionChanged?.Invoke();
        }

        // ---------- Clique em um slot do mapa ----------
        public void OnSlotClicked(MapSlot slot)
        {
            if (slot == null) return;

            // 1) Equipar item, se houver um item selecionado e o slot tiver gato.
            if (ItemManager.Instance != null && ItemManager.Instance.SelectedForEquip != null)
            {
                if (!slot.IsEmpty)
                    ItemManager.Instance.TryEquipSelectedOn(slot.Occupant);
                else
                    UIManager.Instance?.ShowMessage("Toque em um gato para equipar o item.");
                return;
            }

            // 2) Posicionar o gato selecionado do banco em um slot vazio.
            if (SelectedBenchCat != null && slot.IsEmpty)
            {
                if (!CanEditBoard)
                {
                    UIManager.Instance?.ShowMessage("Não dá para posicionar durante a onda!");
                    return;
                }
                PlaceCat(SelectedBenchCat, slot);
                ClearSelection();
                return;
            }

            // 3) Slot ocupado: foca o gato (mostra alcance e detalhes).
            if (!slot.IsEmpty)
            {
                FocusCat(slot.Occupant);
            }
        }

        private void PlaceCat(CatUnit cat, MapSlot slot)
        {
            BenchManager.Instance?.RemoveCatFromBench(cat);
            cat.gameObject.SetActive(true);
            cat.IsPlaced = true;
            slot.SetOccupant(cat);

            SynergyManager.Instance?.RecalculateSynergies();
        }

        // ---------- Foco / alcance ----------
        public void FocusCat(CatUnit cat)
        {
            ClearFocus();
            FocusedCat = cat;
            cat.ShowRange();
            UIManager.Instance?.ShowCatDetail(cat);
            OnSelectionChanged?.Invoke();
        }

        public void ClearFocus()
        {
            if (FocusedCat != null) FocusedCat.HideRange();
            FocusedCat = null;
            UIManager.Instance?.HideCatDetail();
        }

        // ---------- Devolver ao banco ----------
        public void ReturnCatToBench(CatUnit cat)
        {
            if (cat == null) return;
            if (!CanEditBoard)
            {
                UIManager.Instance?.ShowMessage("Não dá para mover durante a onda!");
                return;
            }
            if (BenchManager.Instance == null || !BenchManager.Instance.HasSpace())
            {
                UIManager.Instance?.ShowMessage("Banco cheio!");
                return;
            }

            if (cat.CurrentSlot != null) cat.CurrentSlot.Clear();
            BenchManager.Instance.AddCatToBench(cat);
            ClearFocus();
            SynergyManager.Instance?.RecalculateSynergies();
        }

        // ---------- Vender ----------
        public void SellCat(CatUnit cat)
        {
            if (cat == null) return;

            // Devolve os itens equipados ao inventário (não se perde nada).
            var returned = new List<ItemData>();
            cat.UnequipAll(returned);
            ItemManager.Instance?.ReturnItems(returned);

            EconomyManager.Instance?.SellCat(cat);

            if (cat.CurrentSlot != null) cat.CurrentSlot.Clear();
            BenchManager.Instance?.RemoveCatFromBench(cat);

            ClearFocus();
            Destroy(cat.gameObject);
            SynergyManager.Instance?.RecalculateSynergies();
            BenchManager.Instance?.NotifyChanged();
        }
    }
}
