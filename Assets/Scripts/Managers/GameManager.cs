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
        /// <summary>Inimigos derrotados na partida (para a tela de fim).</summary>
        public int EnemiesDefeated { get; private set; }

        public void RegisterEnemyDefeated() => EnemiesDefeated++;

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnLivesChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            StartGame();
            if (LoadOnStart) { LoadOnStart = false; SaveSystem.Restore(); }
        }

        public void StartGame()
        {
            Lives = GameBalance.StartingLives;
            EnemiesDefeated = 0;
            Time.timeScale = 1f;
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
            SFXManager.Play(SfxType.StartWave);
        }

        /// <summary>Chamado pelo WaveManager quando a onda atual termina (sem derrota).</summary>
        public void EndWave(int completedWaveNumber, bool wasLastWave, int reward)
        {
            if (State == GameState.Defeat) return;

            if (wasLastWave)
            {
                WinGame();
                return;
            }

            SetState(GameState.Preparation);
            ShopManager.Instance?.GenerateShop();
            UIManager.Instance?.ShowWaveSummary(completedWaveNumber, reward, Lives);
            SaveSystem.Save(); // auto-save no início de cada preparação

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
            SFXManager.Play(SfxType.Victory);
            SaveSystem.RecordBest(WaveManager.Instance != null ? WaveManager.Instance.waves.Count : 0);
            SaveSystem.Clear(); // a run acabou
        }

        public void LoseGame()
        {
            SetState(GameState.Defeat);
            WaveManager.Instance?.StopWave();
            SFXManager.Play(SfxType.Defeat);
            SaveSystem.RecordBest(WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveIndex + 1 : 0);
            SaveSystem.Clear();
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

        /// <summary>
        /// Se true ao carregar a cena, o jogo começa direto (sem menu principal).
        /// Persiste entre recarregamentos de cena (campo estático).
        /// </summary>
        public static bool StartInGame = false;
        /// <summary>Se true ao carregar, restaura o jogo salvo (botão Continuar).</summary>
        public static bool LoadOnStart = false;

        /// <summary>Define as vidas diretamente (usado pelo carregamento de save).</summary>
        public void SetLives(int value)
        {
            Lives = Mathf.Max(0, value);
            OnLivesChanged?.Invoke(Lives);
        }

        /// <summary>Recomeça a fase do zero (recarrega a cena atual).</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Novo jogo: recarrega a cena e entra direto em jogo (pula o menu).</summary>
        public void NewGame()
        {
            StartInGame = true;
            Restart();
        }

        /// <summary>Volta ao menu principal (recarrega a cena e mostra o menu).</summary>
        public void GoToMenu()
        {
            StartInGame = false;
            Restart();
        }

        private void SetState(GameState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(State);
        }
    }
}
