using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Data;
using MeowTactics.Combat;
using MeowTactics.Managers;

namespace MeowTactics.Enemies
{
    /// <summary>
    /// Instância viva de um inimigo no mapa.
    /// Anda pelo caminho, recebe dano e morre ou chega à base.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyUnit : MonoBehaviour
    {
        /// <summary>Lista global de todos os inimigos vivos (usada pelos gatos para mirar).</summary>
        public static readonly List<EnemyUnit> Active = new List<EnemyUnit>();

        public EnemyData Data { get; private set; }

        // ---- Atributos atuais (já escalados pela onda) ----
        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public float CurrentArmor { get; private set; }
        public float CurrentMagicResistance { get; private set; }
        public float CurrentMoveSpeed { get; private set; }

        /// <summary>Progresso ao longo do caminho (0 = início, 1 = base). Usado para priorizar alvos.</summary>
        public float PathProgress { get; private set; }

        public bool IsAlive { get; private set; }

        private IReadOnlyList<Vector3> path;
        private int pathIndex;
        private SpriteRenderer sprite;
        private HealthBar healthBar;

        // Lentidão temporária
        private float slowFactor = 1f; // 1 = sem lentidão
        private float slowTimer = 0f;

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            healthBar = GetComponentInChildren<HealthBar>();
        }

        /// <summary>
        /// Prepara o inimigo com seus dados e o multiplicador da onda.
        /// </summary>
        public void Initialize(EnemyData data, float scalingMultiplier, IReadOnlyList<Vector3> pathPoints)
        {
            Data = data;
            path = pathPoints;
            pathIndex = 0;
            PathProgress = 0f;

            MaxHealth = data.maxHealth * scalingMultiplier;
            CurrentHealth = MaxHealth;
            CurrentArmor = data.armor * scalingMultiplier;
            CurrentMagicResistance = data.magicResistance * scalingMultiplier;
            CurrentMoveSpeed = data.moveSpeed; // velocidade não escala por padrão

            slowFactor = 1f;
            slowTimer = 0f;
            IsAlive = true;

            if (sprite != null)
            {
                if (data.icon != null) sprite.sprite = data.icon;
                sprite.color = data.placeholderColor;
            }
            transform.localScale = Vector3.one * data.visualScale;

            if (path != null && path.Count > 0)
                transform.position = path[0];

            UpdateHealthBar();

            if (!Active.Contains(this)) Active.Add(this);
        }

        private void Update()
        {
            if (!IsAlive) return;

            // Atualiza lentidão
            if (slowTimer > 0f)
            {
                slowTimer -= Time.deltaTime;
                if (slowTimer <= 0f) slowFactor = 1f;
            }

            MoveAlongPath();
        }

        private void MoveAlongPath()
        {
            if (path == null || pathIndex >= path.Count)
            {
                ReachBase();
                return;
            }

            Vector3 target = path[pathIndex];
            float step = CurrentMoveSpeed * GameBalance.EnemySpeedScale * slowFactor * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, step);

            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                pathIndex++;
            }

            // Progresso aproximado (0..1) para priorização de alvo
            PathProgress = path.Count <= 1 ? 1f : (float)pathIndex / (path.Count - 1);
        }

        public void TakeDamage(float amount, DamageType type, DamageContext context)
        {
            if (!IsAlive) return;

            CurrentHealth -= amount;
            UpdateHealthBar();

            if (CurrentHealth <= 0f)
                Die();
        }

        public void ApplySlow(float percentage, float duration)
        {
            // Pega a lentidão mais forte vigente
            float newFactor = 1f - Mathf.Clamp01(percentage);
            if (newFactor < slowFactor) slowFactor = newFactor;
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        private void Die()
        {
            if (!IsAlive) return;
            IsAlive = false;
            Active.Remove(this);

            EconomyManager.Instance?.AddCoins(Data.coinReward);
            WaveManager.Instance?.OnEnemyRemoved(this);

            // TODO (arte): tocar animação/efeito de morte aqui.
            Destroy(gameObject);
        }

        private void ReachBase()
        {
            if (!IsAlive) return;
            IsAlive = false;
            Active.Remove(this);

            GameManager.Instance?.OnEnemyReachedBase(Data.baseDamage);
            WaveManager.Instance?.OnEnemyRemoved(this);

            Destroy(gameObject);
        }

        private void UpdateHealthBar()
        {
            if (healthBar != null && MaxHealth > 0f)
                healthBar.SetFill(CurrentHealth / MaxHealth);
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }
    }
}
