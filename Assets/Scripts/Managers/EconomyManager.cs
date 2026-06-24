using System;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Cats;

namespace MeowTactics.Managers
{
    /// <summary>
    /// Controla as moedas do jogador.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        public int Coins { get; private set; }
        /// <summary>Total de moedas ganhas na partida (para a tela de fim).</summary>
        public int TotalEarned { get; private set; }

        /// <summary>Disparado sempre que o total de moedas muda (para a UI ouvir).</summary>
        public event Action<int> OnCoinsChanged;

        private void Awake()
        {
            Instance = this;
        }

        public void ResetEconomy()
        {
            Coins = GameBalance.StartingCoins;
            TotalEarned = 0;
            OnCoinsChanged?.Invoke(Coins);
        }

        /// <summary>Define o total de moedas diretamente (usado pelo carregamento de save).</summary>
        public void SetCoins(int amount)
        {
            Coins = Mathf.Max(0, amount);
            OnCoinsChanged?.Invoke(Coins);
        }

        public bool CanAfford(int amount) => Coins >= amount;

        public void AddCoins(int amount)
        {
            if (amount == 0) return;
            Coins += amount;
            if (amount > 0) { TotalEarned += amount; AchievementManager.Instance?.Report("coins", amount); }
            OnCoinsChanged?.Invoke(Coins);
        }

        /// <summary>Gasta moedas se houver saldo. Retorna true se conseguiu.</summary>
        public bool SpendCoins(int amount)
        {
            if (!CanAfford(amount)) return false;
            Coins -= amount;
            OnCoinsChanged?.Invoke(Coins);
            return true;
        }

        /// <summary>Devolve parte do investimento ao vender um gato.</summary>
        public int SellCat(CatUnit cat)
        {
            int value = Mathf.FloorToInt(cat.TotalInvested * GameBalance.SellRatio);
            AddCoins(value);
            return value;
        }
    }
}
