using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Data;
using MeowTactics.Combat;
using MeowTactics.Managers;
using MeowTactics.Utilities;

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
        private JuiceVisual juice;
        private Transform visual; // filho que gira para a direção do movimento

        // Arte vista de cima aponta a "frente" para BAIXO; +90 faz mirar a direção certa.
        private const float SpriteForwardOffset = 90f;
        private const float TurnSpeed = 720f; // graus por segundo

        // Lentidão temporária
        private float slowFactor = 1f; // 1 = sem lentidão
        private float slowTimer = 0f;

        private void Awake()
        {
            // O visual (sprite + juice) fica num filho "Visual" que gira; a barra de
            // vida fica na raiz, em pé. Fallback: tudo na raiz (compatível com prefabs).
            Transform v = transform.Find("Visual");
            if (v != null)
            {
                visual = v;
                sprite = v.GetComponent<SpriteRenderer>();
                juice = v.GetComponent<JuiceVisual>();
            }
            else
            {
                visual = transform;
                sprite = GetComponent<SpriteRenderer>();
                juice = GetComponent<JuiceVisual>();
            }
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

            // Escalonamento por onda (0-based): vida +18%/onda; armadura/RM +5%/onda
            // a partir da onda 4; velocidade +3%/onda (limite 1.3x).
            int w = WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveIndex : 0;
            float hpScale  = scalingMultiplier * (1f + 0.18f * w);
            float defScale = scalingMultiplier * (w >= 3 ? 1f + 0.05f * (w - 2) : 1f);
            float spdScale = Mathf.Min(1.3f, 1f + 0.03f * w);

            MaxHealth = data.maxHealth * hpScale;
            CurrentHealth = MaxHealth;
            CurrentArmor = data.armor * defScale;
            CurrentMagicResistance = data.magicResistance * defScale;
            CurrentMoveSpeed = data.moveSpeed * spdScale;

            slowFactor = 1f;
            slowTimer = 0f;
            IsAlive = true;

            if (sprite != null)
            {
                if (data.icon != null)
                {
                    sprite.sprite = data.icon;
                    sprite.color = Color.white;
                }
                else
                {
                    sprite.color = data.placeholderColor;
                }
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
            FaceDirection(target - transform.position);
            transform.position = Vector3.MoveTowards(transform.position, target, step);

            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                pathIndex++;
            }

            // Progresso aproximado (0..1) para priorização de alvo
            PathProgress = path.Count <= 1 ? 1f : (float)pathIndex / (path.Count - 1);
        }

        /// <summary>Gira o visual para apontar na direção do movimento (próximo ponto).</summary>
        private void FaceDirection(Vector3 dir)
        {
            if (visual == null || dir.sqrMagnitude < 0.0001f) return;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + SpriteForwardOffset;
            Quaternion want = Quaternion.Euler(0f, 0f, ang);
            visual.rotation = Quaternion.RotateTowards(visual.rotation, want, TurnSpeed * Time.deltaTime);
        }

        public void TakeDamage(float amount, DamageType type, DamageContext context)
        {
            if (!IsAlive) return;

            CurrentHealth -= amount;
            juice?.Flash();
            juice?.Punch(0.22f);
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

            // Moeda: comuns (coinReward <= 1) têm CHANCE de dropar; elites/boss dão sempre.
            int dropped = Data.coinReward;
            if (dropped <= 1)
                dropped = (Random.value < GameBalance.CommonCoinDropChance) ? 1 : 0;

            if (dropped > 0)
            {
                EconomyManager.Instance?.AddCoins(dropped);
                SFXManager.Play(SfxType.Coin);
                FloatingText.Spawn(transform.position, "+" + dropped, new Color(1f, 0.85f, 0.3f), 1f);
            }

            GameManager.Instance?.RegisterEnemyDefeated();
            WaveManager.Instance?.OnEnemyRemoved(this);
            SFXManager.Play(SfxType.EnemyDeath);

            // "poof" de morte.
            if (healthBar != null) healthBar.gameObject.SetActive(false);
            if (juice != null) juice.PlayDeath();
            else Destroy(gameObject);
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
