using System;
using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Data;
using MeowTactics.Cats;
using MeowTactics.Utilities;
using MeowTactics.UI;

namespace MeowTactics.Managers
{
    /// <summary>
    /// Loja aleatória entre ondas. Mostra GameBalance.ShopSize gatos,
    /// permite comprar (vão para o banco) e atualizar (reroll).
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Tooltip("Todos os gatos que podem aparecer na loja")]
        public List<CatData> availableCats = new List<CatData>();

        /// <summary>Opções atuais da loja (null = slot já comprado).</summary>
        public readonly List<CatData> currentShopOptions = new List<CatData>();

        public event Action OnShopChanged;

        private void Awake()
        {
            Instance = this;
        }

        public void GenerateShop()
        {
            currentShopOptions.Clear();
            for (int i = 0; i < GameBalance.ShopSize; i++)
                currentShopOptions.Add(RandomCat());
            OnShopChanged?.Invoke();
        }

        private CatData RandomCat()
        {
            if (availableCats == null || availableCats.Count == 0) return null;
            return availableCats[UnityEngine.Random.Range(0, availableCats.Count)];
        }

        public bool CanBuy(CatData cat)
        {
            if (cat == null) return false;
            if (BenchManager.Instance == null || !BenchManager.Instance.HasSpace()) return false;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.CanAfford(cat.cost)) return false;
            return true;
        }

        /// <summary>Compra o gato no índice indicado da loja.</summary>
        public void BuyCat(int shopIndex)
        {
            if (shopIndex < 0 || shopIndex >= currentShopOptions.Count) return;
            CatData cat = currentShopOptions[shopIndex];
            if (cat == null) return;

            if (BenchManager.Instance == null || !BenchManager.Instance.HasSpace())
            {
                UIManager.Instance?.ShowMessage("Banco cheio!");
                SFXManager.Play(SfxType.Error);
                return;
            }
            if (EconomyManager.Instance == null || !EconomyManager.Instance.SpendCoins(cat.cost))
            {
                UIManager.Instance?.ShowMessage("Moedas insuficientes!");
                SFXManager.Play(SfxType.Error);
                return;
            }

            CatUnit unit = UnitFactory.CreateCat(cat, Vector3.zero);
            BenchManager.Instance.AddCatToBench(unit);

            currentShopOptions[shopIndex] = null; // slot fica vazio até o próximo reroll
            OnShopChanged?.Invoke();
            SFXManager.Play(SfxType.Buy);
            AchievementManager.Instance?.Report("catsBought", 1);
        }

        public void RerollShop()
        {
            if (EconomyManager.Instance == null) return;
            if (!EconomyManager.Instance.SpendCoins(GameBalance.RerollCost))
            {
                UIManager.Instance?.ShowMessage("Moedas insuficientes para atualizar!");
                SFXManager.Play(SfxType.Error);
                return;
            }
            GenerateShop();
            SFXManager.Play(SfxType.Click);
            AchievementManager.Instance?.Report("rerolls", 1);
        }
    }
}
