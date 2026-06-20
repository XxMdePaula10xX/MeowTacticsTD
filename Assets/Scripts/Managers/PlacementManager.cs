using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using MeowTactics.Core;
using MeowTactics.Cats;
using MeowTactics.Map;
using MeowTactics.Data;
using MeowTactics.UI;
using MeowTactics.Utilities;

namespace MeowTactics.Managers
{
    /// <summary>
    /// Posicionamento LIVRE de gatos: o jogador clica em qualquer lugar do gramado,
    /// desde que não seja em cima do caminho nem colado em outro gato.
    /// Também cuida de selecionar/equipar/vender.
    /// </summary>
    public class PlacementManager : MonoBehaviour
    {
        public static PlacementManager Instance { get; private set; }

        // Distâncias (em unidades de mundo).
        private const float CatMinDistance = 1.1f; // espaço mínimo entre gatos
        private const float CatClickRadius = 0.7f; // raio para clicar num gato

        public CatUnit SelectedBenchCat { get; private set; }
        public CatUnit FocusedCat { get; private set; }

        public event Action OnSelectionChanged;

        private readonly List<CatUnit> placed = new List<CatUnit>();

        // Prévia de posicionamento (segue o cursor).
        private GameObject previewGo;
        private SpriteRenderer previewRing, previewDot;

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
            UIManager.Instance?.ShowMessage("Toque no gramado para posicionar (longe do caminho).");
            OnSelectionChanged?.Invoke();
        }

        public void ClearSelection()
        {
            SelectedBenchCat = null;
            HidePreview();
            OnSelectionChanged?.Invoke();
        }

        // ---------- Clique no mapa (posicionamento livre) ----------
        public void OnBoardClicked(Vector3 worldPos)
        {
            // 1) Equipar item, se houver item selecionado.
            if (ItemManager.Instance != null && ItemManager.Instance.SelectedForEquip != null)
            {
                var cat = CatNear(worldPos, CatClickRadius);
                if (cat != null) ItemManager.Instance.TryEquipSelectedOn(cat);
                else UIManager.Instance?.ShowMessage("Toque em um gato para equipar o item.");
                return;
            }

            // 2) Posicionar o gato selecionado do banco.
            if (SelectedBenchCat != null)
            {
                TryPlaceSelected(worldPos);
                return;
            }

            // 3) Sem seleção: focar um gato clicado, ou limpar foco.
            var clicked = CatNear(worldPos, CatClickRadius);
            if (clicked != null) FocusCat(clicked);
            else ClearFocus();
        }

        private void TryPlaceSelected(Vector3 pos)
        {
            if (!CanEditBoard)
            {
                UIManager.Instance?.ShowMessage("Não dá para posicionar durante a onda!");
                return;
            }
            string reason;
            if (!IsValidPlacement(pos, out reason))
            {
                UIManager.Instance?.ShowMessage(reason);
                return;
            }

            PlaceCatAt(SelectedBenchCat, pos);
            ClearSelection();
        }

        /// <summary>Regras: dentro da área, fora do caminho, sem outro gato perto.</summary>
        public bool IsValidPlacement(Vector3 pos, out string reason)
        {
            var map = MapManager.Instance;
            if (map != null && !map.InsideBoard(pos)) { reason = "Fora da área de jogo."; return false; }
            if (map != null && map.IsOnPath(pos)) { reason = "Não pode em cima do caminho!"; return false; }
            if (CatNear(pos, CatMinDistance) != null) { reason = "Muito perto de outro gato."; return false; }
            reason = null;
            return true;
        }

        private void PlaceCatAt(CatUnit cat, Vector3 pos)
        {
            BenchManager.Instance?.RemoveCatFromBench(cat);
            cat.gameObject.SetActive(true);
            cat.transform.position = new Vector3(pos.x, pos.y, 0f);
            cat.transform.rotation = Quaternion.identity;
            cat.IsPlaced = true;
            if (!placed.Contains(cat)) placed.Add(cat);

            SynergyManager.Instance?.RecalculateSynergies();
        }

        private CatUnit CatNear(Vector3 pos, float radius)
        {
            CatUnit best = null;
            float bestDist = radius;
            foreach (var c in placed)
            {
                if (c == null) continue;
                float d = Vector2.Distance(c.transform.position, pos);
                if (d <= bestDist) { bestDist = d; best = c; }
            }
            return best;
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

            placed.Remove(cat);
            BenchManager.Instance.AddCatToBench(cat);
            ClearFocus();
            SynergyManager.Instance?.RecalculateSynergies();
        }

        // ---------- Vender ----------
        public void SellCat(CatUnit cat)
        {
            if (cat == null) return;

            var returned = new List<ItemData>();
            cat.UnequipAll(returned);
            ItemManager.Instance?.ReturnItems(returned);

            EconomyManager.Instance?.SellCat(cat);

            placed.Remove(cat);
            BenchManager.Instance?.RemoveCatFromBench(cat);

            ClearFocus();
            Destroy(cat.gameObject);
            SynergyManager.Instance?.RecalculateSynergies();
            BenchManager.Instance?.NotifyChanged();
        }

        // ---------- Prévia de posicionamento ----------
        private void Update()
        {
            if (SelectedBenchCat == null || !CanEditBoard)
            {
                HidePreview();
                return;
            }
            ShowPreview(MouseWorld());
        }

        private void ShowPreview(Vector3 pos)
        {
            EnsurePreview();
            previewGo.SetActive(true);
            previewGo.transform.position = new Vector3(pos.x, pos.y, 0f);

            bool valid = IsValidPlacement(pos, out _);
            Color tint = valid ? new Color(0.4f, 1f, 0.45f) : new Color(1f, 0.4f, 0.4f);

            float range = SelectedBenchCat != null ? SelectedBenchCat.CurrentRange : 2.5f;
            previewRing.transform.localScale = Vector3.one * (range * 2f);
            previewRing.color = new Color(tint.r, tint.g, tint.b, 0.5f);
            previewDot.color = new Color(tint.r, tint.g, tint.b, 0.35f);
        }

        private void HidePreview()
        {
            if (previewGo != null) previewGo.SetActive(false);
        }

        private void EnsurePreview()
        {
            if (previewGo != null) return;
            previewGo = new GameObject("PlacementPreview");

            var ring = new GameObject("Range");
            ring.transform.SetParent(previewGo.transform, false);
            previewRing = ring.AddComponent<SpriteRenderer>();
            previewRing.sprite = SpriteFactory.Ring;
            previewRing.sortingOrder = 6;

            var dot = new GameObject("Dot");
            dot.transform.SetParent(previewGo.transform, false);
            previewDot = dot.AddComponent<SpriteRenderer>();
            previewDot.sprite = SpriteFactory.Circle;
            previewDot.sortingOrder = 6;
            dot.transform.localScale = Vector3.one * 0.9f;
        }

        private Vector3 MouseWorld()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;
            Vector3 m = Input.mousePosition;
            m.z = -cam.transform.position.z;
            Vector3 w = cam.ScreenToWorldPoint(m);
            w.z = 0f;
            return w;
        }

        // ---------- Legado (slots desativados) ----------
        public void OnSlotClicked(MapSlot slot)
        {
            if (slot != null && !slot.IsEmpty) FocusCat(slot.Occupant);
        }
    }
}
