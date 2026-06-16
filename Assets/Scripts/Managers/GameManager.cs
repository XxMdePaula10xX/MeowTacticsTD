using System;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.UI;

namespace MeowTactics.Managers
{
    /// <summary>
    /// Controla o estado geral da partida e a vida do jogador.
    /// É o ponto central que liga os outros gerenciadores.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Preparation;
        public int Lives { get; private set; }

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnLivesChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            StartGame();
        }

        public void StartGame()
        {
            Lives = GameBalance.StartingLives;
            SetState(GameState.Preparation);

            EconomyManager.Instance?.ResetEconomy();
            ShopManager.Instance?.GenerateShop();
            SynergyManager.Instance?.RecalculateSynergies();

            OnLivesChanged?.Invoke(Lives);
        }

        public void StartWave()
        {
            if (State != GameState.Preparation) return;
            if (WaveManager.Instance == null) return;

            SetState(GameState.WaveInProgress);
            WaveManager.Instance.StartCurrentWave();
        }

        /// <summary>Chamado pelo WaveManager quando a onda atual termina (sem derrota).</summary>
        public void EndWave(int completedWaveNumber, bool wasLastWave)
        {
            if (State == GameState.Defeat) return;

            if (wasLastWave)
            {
                WinGame();
                return;
            }

            SetState(GameState.Preparation);
            ShopManager.Instance?.GenerateShop();

            // A cada N ondas, oferece a roleta de itens (ex: ondas 3, 6, 9).
            if (GameBalance.ItemDropEveryNWaves > 0 &&
                completedWaveNumber % GameBalance.ItemDropEveryNWaves == 0 &&
                ItemManager.Instance != null && UIManager.Instance != null)
            {
                var choices = ItemManager.Instance.GetRandomChoices(GameBalance.ItemDraftChoices);
                if (choices.Count > 0)
                    UIManager.Instance.ShowItemDraft(choices);
            }
        }

        public void OnEnemyReachedBase(int damage)
        {
            if (State != GameState.WaveInProgress) return;

            Lives -= damage;
            if (Lives < 0) Lives = 0;
            OnLivesChanged?.Invoke(Lives);

            if (Lives <= 0)
                LoseGame();
        }

        public void WinGame()
        {
            SetState(GameState.Victory);
        }

        public void LoseGame()
        {
            SetState(GameState.Defeat);
            WaveManager.Instance?.StopWave();
        }

        public void PauseGame()
        {
            if (State == GameState.WaveInProgress)
            {
                Time.timeScale = 0f;
                SetState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (State == GameState.Paused)
            {
                Time.timeScale = 1f;
                SetState(GameState.WaveInProgress);
            }
        }

        /// <summary>Recomeça a fase do zero (recarrega a cena atual).</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void SetState(GameState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(State);
        }
    }
}
