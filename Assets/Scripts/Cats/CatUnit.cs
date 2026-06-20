using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Data;
using MeowTactics.Combat;
using MeowTactics.Enemies;
using MeowTactics.Utilities;

namespace MeowTactics.Cats
{
    /// <summary>
    /// Instância de um gato no jogo (no banco ou posicionado no mapa).
    /// Quando posicionado, mira e ataca automaticamente.
    /// Pode equipar até GameBalance.MaxItemsPerCat itens.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CatUnit : MonoBehaviour
    {
        public CatData Data { get; private set; }

        /// <summary>True se está num slot do mapa (ataca e conta para sinergia).</summary>
        public bool IsPlaced { get; set; }
        /// <summary>Custo investido, usado para o valor de venda.</summary>
        public int TotalInvested { get; private set; }

        public MeowTactics.Map.MapSlot CurrentSlot { get; set; }

        /// <summary>Itens equipados neste gato.</summary>
        public readonly List<ItemData> Items = new List<ItemData>();

        // ---- Buffs acumulados (de sinergias E de itens; recalculados a cada mudança) ----
        private float bonusAttackSpeedPct;
        private float bonusCritChance;
        private float bonusArmorPen;
        private float bonusMagicPen;
        private float bonusDamagePct;
        private float bonusRangePct;

        // Efeitos especiais vindos de itens (recalculados em ApplyItems)
        private float itemTrueDamageFlat;
        private bool  itemSlow;
        private float itemSlowAmount;
        private float itemSlowDuration;
        private bool  itemArea;
        private float itemAreaRadius;

        private float attackTimer;
        private SpriteRenderer sprite;
        private Transform rangeIndicator;
        private JuiceVisual juice;

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            juice = GetComponent<JuiceVisual>();
            Transform ri = transform.Find("RangeIndicator");
            if (ri != null) rangeIndicator = ri;
            HideRange();
        }

        public void Initialize(CatData data)
        {
            Data = data;
            TotalInvested = data.cost;
            ResetBuffs();
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (sprite == null || Data == null) return;
            if (Data.icon != null)
            {
                // Arte real: mostra as cores verdadeiras do sprite (sem tingir).
                sprite.sprite = Data.icon;
                sprite.color = Color.white;
            }
            else
            {
                sprite.color = Data.placeholderColor;
            }
        }

        // ---------- Itens ----------
        public bool CanEquipMore => Items.Count < GameBalance.MaxItemsPerCat;

        public void EquipItem(ItemData item)
        {
            if (item == null || !CanEquipMore) return;
            Items.Add(item);
        }

        public void UnequipAll(List<ItemData> into)
        {
            if (into != null) into.AddRange(Items);
            Items.Clear();
        }

        /// <summary>Sinergias efetivas = as do gato + as concedidas por distintivos.</summary>
        public IEnumerable<SynergyType> GetEffectiveSynergies()
        {
            var set = new HashSet<SynergyType>(Data.synergies);
            foreach (var item in Items)
                foreach (var s in item.grantedSynergies)
                    set.Add(s);
            return set;
        }

        // ---------- Status calculados ----------
        public float CurrentDamage => Data.baseDamage;
        public float CurrentAttackSpeed => Data.attackSpeed * (1f + bonusAttackSpeedPct);
        public float CurrentRange => Data.range * (1f + bonusRangePct);
        public float CurrentCritChance => Data.critChance + bonusCritChance;

        private bool EffectiveSlow => Data.appliesSlow || itemSlow;
        private bool EffectiveArea => Data.areaDamage || itemArea;
        private float EffectiveSlowAmount => Mathf.Max(Data.appliesSlow ? Data.slowAmount : 0f, itemSlow ? itemSlowAmount : 0f);
        private float EffectiveSlowDuration => Mathf.Max(Data.appliesSlow ? Data.slowDuration : 0f, itemSlow ? itemSlowDuration : 0f);
        private float EffectiveAreaRadius => Mathf.Max(Data.areaDamage ? Data.areaRadius : 0f, itemArea ? itemAreaRadius : 0f);

        // ---------- Buffs ----------
        public void ResetBuffs()
        {
            bonusAttackSpeedPct = 0f;
            bonusCritChance = 0f;
            bonusArmorPen = 0f;
            bonusMagicPen = 0f;
            bonusDamagePct = 0f;
            bonusRangePct = 0f;

            itemTrueDamageFlat = 0f;
            itemSlow = false; itemSlowAmount = 0f; itemSlowDuration = 0f;
            itemArea = false; itemAreaRadius = 0f;
        }

        /// <summary>Aplica os efeitos dos itens equipados aos buffs deste gato.</summary>
        public void ApplyItems()
        {
            foreach (var item in Items)
            {
                foreach (var eff in item.statEffects)
                    AddBuff(eff.stat, eff.value);

                itemTrueDamageFlat += item.bonusTrueDamagePerHit;

                if (item.grantsSlow)
                {
                    itemSlow = true;
                    itemSlowAmount = Mathf.Max(itemSlowAmount, item.slowAmount);
                    itemSlowDuration = Mathf.Max(itemSlowDuration, item.slowDuration);
                }
                if (item.grantsArea)
                {
                    itemArea = true;
                    itemAreaRadius = Mathf.Max(itemAreaRadius, item.areaRadius);
                }
            }
        }

        public void AddBuff(BonusStat stat, float value)
        {
            switch (stat)
            {
                case BonusStat.AttackSpeedPercent:   bonusAttackSpeedPct += value / 100f; break;
                case BonusStat.CritChancePercent:    bonusCritChance += value; break;
                case BonusStat.ArmorPenetrationFlat: bonusArmorPen += value; break;
                case BonusStat.MagicPenetrationFlat: bonusMagicPen += value; break;
                case BonusStat.DamagePercent:        bonusDamagePct += value / 100f; break;
                case BonusStat.RangePercent:         bonusRangePct += value / 100f; break;
            }
        }

        // ---------- Combate ----------
        private void Update()
        {
            if (!IsPlaced) return;
            if (Managers.GameManager.Instance == null) return;
            if (Managers.GameManager.Instance.State != GameState.WaveInProgress) return;

            attackTimer += Time.deltaTime;
            float interval = CurrentAttackSpeed > 0f ? 1f / CurrentAttackSpeed : 999f;
            if (attackTimer >= interval)
            {
                EnemyUnit target = FindTarget();
                if (target != null)
                {
                    Attack(target);
                    attackTimer = 0f;
                }
            }
        }

        /// <summary>Inimigo mais à frente no caminho dentro do alcance.</summary>
        public EnemyUnit FindTarget()
        {
            EnemyUnit best = null;
            float bestProgress = -1f;
            float range = CurrentRange;
            Vector3 pos = transform.position;

            foreach (var enemy in EnemyUnit.Active)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (Vector3.Distance(pos, enemy.transform.position) > range) continue;
                if (enemy.PathProgress > bestProgress)
                {
                    bestProgress = enemy.PathProgress;
                    best = enemy;
                }
            }
            return best;
        }

        public void Attack(EnemyUnit target)
        {
            juice?.Punch(0.4f); // "tranco" ao atacar
            FaceTarget(target.transform.position);
            DealDamage(target);

            if (EffectiveArea)
            {
                foreach (var enemy in EnemyUnit.Active)
                {
                    if (enemy == null || enemy == target || !enemy.IsAlive) continue;
                    if (Vector3.Distance(target.transform.position, enemy.transform.position) <= EffectiveAreaRadius)
                        DealDamage(enemy);
                }
            }

            SpawnHitFx(target.transform.position);
        }

        private void DealDamage(EnemyUnit enemy)
        {
            DamageContext ctx = BuildDamageContext();
            DamageResult result = DamageCalculator.CalculateDamage(CurrentDamage, enemy, ctx);
            enemy.TakeDamage(result.amount, ctx.damageType, ctx);
            ShowDamageNumber(enemy, result);

            // Dano verdadeiro extra concedido por itens (ignora defesas).
            if (itemTrueDamageFlat > 0f)
            {
                var trueCtx = new DamageContext { damageType = DamageType.True };
                var trueRes = DamageCalculator.CalculateDamage(itemTrueDamageFlat, enemy, trueCtx);
                enemy.TakeDamage(trueRes.amount, DamageType.True, trueCtx);
            }

            if (EffectiveSlow)
                enemy.ApplySlow(EffectiveSlowAmount, EffectiveSlowDuration);
        }

        private DamageContext BuildDamageContext()
        {
            return new DamageContext
            {
                damageType = Data.damageType,
                armorPenetration = bonusArmorPen,
                magicPenetration = bonusMagicPen,
                critChance = CurrentCritChance,
                critMultiplier = Data.critMultiplier,
                bonusDamageMultiplier = bonusDamagePct
            };
        }

        private void SpawnHitFx(Vector3 worldPos)
        {
            // Projétil cosmético do gato até o alvo (estoura numa faísca ao chegar).
            CombatFx.Projectile(transform.position, worldPos, DamageColor(Data.damageType));
        }

        /// <summary>Espelha o gato para o lado do inimigo (funciona com arte de frente).</summary>
        private void FaceTarget(Vector3 targetPos)
        {
            if (sprite == null) return;
            float dx = targetPos.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.15f)
                sprite.flipX = dx < 0f;
        }

        private void ShowDamageNumber(EnemyUnit enemy, DamageResult result)
        {
            if (enemy == null) return;
            int shown = Mathf.Max(1, Mathf.RoundToInt(result.amount));
            if (result.wasCritical)
                FloatingText.Spawn(enemy.transform.position, shown + "!", new Color(1f, 0.9f, 0.2f), 1.5f);
            else
                FloatingText.Spawn(enemy.transform.position, shown.ToString(), DamageColor(Data.damageType), 1f);
        }

        private static Color DamageColor(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return new Color(1f, 0.78f, 0.35f);  // laranja
                case DamageType.Magical:  return new Color(0.72f, 0.55f, 1f);  // roxo claro
                case DamageType.True:     return new Color(1f, 0.95f, 0.75f);  // dourado claro
                default: return Color.white;
            }
        }

        // ---------- Range indicator ----------
        public void ShowRange()
        {
            if (rangeIndicator == null) return;
            rangeIndicator.gameObject.SetActive(true);
            rangeIndicator.localScale = Vector3.one * (CurrentRange * 2f);
        }

        public void HideRange()
        {
            if (rangeIndicator != null)
                rangeIndicator.gameObject.SetActive(false);
        }
    }
}
