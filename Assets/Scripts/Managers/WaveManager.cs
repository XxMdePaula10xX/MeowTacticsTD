using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Data;
using MeowTactics.Enemies;
using MeowTactics.Map;
using MeowTactics.Utilities;

namespace MeowTactics.Managers
{
    /// <summary>
    /// Controla a sequência de ondas: nascimento dos inimigos, contagem
    /// dos vivos e o fim de cada onda.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Tooltip("As 10 ondas do MVP, em ordem")]
        public List<WaveData> waves = new List<WaveData>();

        public int CurrentWaveIndex { get; private set; }
        public bool IsWaveRunning { get; private set; }

        /// <summary>Número da onda atual (1-based) e total de ondas.</summary>
        public event Action<int, int> OnWaveChanged;
        /// <summary>Disparado quando o progresso da onda muda (inimigos restantes/total).</summary>
        public event Action OnWaveProgress;

        public int EnemiesTotal { get; private set; }
        public int EnemiesRemaining => Mathf.Max(0, EnemiesTotal - removedThisWave);

        private int aliveCount;
        private int removedThisWave;
        private bool finishedSpawning;
        private Coroutine spawnRoutine;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            NotifyWaveChanged();
        }

        /// <summary>Define a onda atual diretamente (usado pelo carregamento de save).</summary>
        public void SetWaveIndex(int index)
        {
            CurrentWaveIndex = Mathf.Clamp(index, 0, Mathf.Max(0, waves.Count - 1));
            NotifyWaveChanged();
        }

        public WaveData CurrentWave =>
            (CurrentWaveIndex >= 0 && CurrentWaveIndex < waves.Count) ? waves[CurrentWaveIndex] : null;

        public void StartCurrentWave()
        {
            if (IsWaveRunning) return;
            WaveData wave = CurrentWave;
            if (wave == null) return;

            IsWaveRunning = true;
            finishedSpawning = false;
            aliveCount = 0;
            removedThisWave = 0;
            EnemiesTotal = 0;
            foreach (var info in wave.enemies) EnemiesTotal += info.count;
            OnWaveProgress?.Invoke();
            spawnRoutine = StartCoroutine(SpawnRoutine(wave));
        }

        private IEnumerator SpawnRoutine(WaveData wave)
        {
            // Monta a fila plana de inimigos (respeitando a ordem dos grupos).
            var queue = new List<EnemySpawnInfo>();
            foreach (var info in wave.enemies)
                for (int i = 0; i < info.count; i++)
                    queue.Add(info);

            int pathCount = MapManager.Instance != null ? MapManager.Instance.PathCount : 1;
            int spawnIndex = 0;

            foreach (var info in queue)
            {
                int pi = pathCount > 0 ? spawnIndex % pathCount : 0;
                var path = MapManager.Instance != null ? MapManager.Instance.GetPath(pi) : null;
                Vector3 spawnPos = (path != null && path.Count > 0)
                    ? path[0]
                    : (MapManager.Instance != null ? MapManager.Instance.SpawnPoint : Vector3.zero);

                UnitFactory.CreateEnemy(info.enemy, info.scalingMultiplier, spawnPos, path);
                aliveCount++;
                spawnIndex++;
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            finishedSpawning = true;
            CheckWaveCompletion();
        }

        /// <summary>Chamado pelo EnemyUnit ao morrer ou chegar à base.</summary>
        public void OnEnemyRemoved(EnemyUnit enemy)
        {
            aliveCount = Mathf.Max(0, aliveCount - 1);
            removedThisWave++;
            OnWaveProgress?.Invoke();
            CheckWaveCompletion();
        }

        private void CheckWaveCompletion()
        {
            if (!IsWaveRunning) return;
            if (!finishedSpawning) return;
            if (aliveCount > 0) return;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Defeat) return;

            EndWave();
        }

        private void EndWave()
        {
            IsWaveRunning = false;
            WaveData wave = CurrentWave;

            // Recompensa de onda: base + número da onda + bônus específico.
            int waveNumber = CurrentWaveIndex + 1;
            // Recompensa de onda = 3 + floor(onda/2) (+ bônus específico da onda)
            int reward = GameBalance.CoinsPerWaveBase + (waveNumber / 2) + (wave != null ? wave.bonusReward : 0);
            EconomyManager.Instance?.AddCoins(reward);

            int completedWaveNumber = CurrentWaveIndex + 1;
            bool wasLastWave = CurrentWaveIndex >= waves.Count - 1;
            if (!wasLastWave)
            {
                CurrentWaveIndex++;
                NotifyWaveChanged();
            }

            GameManager.Instance?.EndWave(completedWaveNumber, wasLastWave, reward);
        }

        public void StopWave()
        {
            IsWaveRunning = false;
            if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        }

        private void NotifyWaveChanged()
        {
            OnWaveChanged?.Invoke(CurrentWaveIndex + 1, waves.Count);
        }
    }
}
