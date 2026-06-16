using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeowTactics.Data
{
    /// <summary>
    /// Descreve um grupo de inimigos dentro de uma onda
    /// (qual inimigo, quantos, e com qual multiplicador de atributos).
    /// </summary>
    [Serializable]
    public class EnemySpawnInfo
    {
        public EnemyData enemy;
        public int count = 1;
        [Tooltip("Multiplicador de vida/armadura/etc. para esta onda (1 = base)")]
        public float scalingMultiplier = 1f;
    }

    /// <summary>
    /// Uma onda completa de inimigos.
    /// Crie pelo menu: Assets > Create > MeowTactics > Wave Data.
    /// </summary>
    [CreateAssetMenu(fileName = "Wave_", menuName = "MeowTactics/Wave Data", order = 2)]
    public class WaveData : ScriptableObject
    {
        public int waveNumber = 1;
        public List<EnemySpawnInfo> enemies = new List<EnemySpawnInfo>();
        [Tooltip("Tempo (segundos) entre o nascimento de cada inimigo")]
        public float spawnInterval = 0.8f;
        [Tooltip("Recompensa extra ao terminar a onda (além do padrão)")]
        public int bonusReward = 0;
        public bool isBossWave = false;
    }
}
