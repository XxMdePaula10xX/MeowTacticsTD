using UnityEngine;

namespace MeowTactics.Data
{
    /// <summary>
    /// Dados base de um tipo de inimigo.
    /// Crie pelo menu: Assets > Create > MeowTactics > Enemy Data.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "MeowTactics/Enemy Data", order = 1)]
    public class EnemyData : ScriptableObject
    {
        [Header("Identidade")]
        public string enemyId;
        public string enemyName;
        [TextArea] public string description;
        public bool isBoss = false;

        [Header("Atributos")]
        public float maxHealth = 30f;
        public float armor = 0f;
        public float magicResistance = 0f;
        [Tooltip("Velocidade base (1.0 = padrão). Convertida por GameBalance.EnemySpeedScale")]
        public float moveSpeed = 1f;

        [Header("Recompensa / Dano")]
        [Tooltip("Vidas que o jogador perde se este inimigo chegar ao final")]
        public int baseDamage = 1;
        public int coinReward = 1;

        [Header("Visual")]
        public Sprite icon;
        public GameObject enemyPrefab;
        public Color placeholderColor = Color.white;
        [Tooltip("Tamanho visual relativo (boss maior que inimigos comuns)")]
        public float visualScale = 1f;
    }
}
