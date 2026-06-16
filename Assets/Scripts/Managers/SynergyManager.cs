using System;
using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Data;
using MeowTactics.Cats;
using MeowTactics.Map;

namespace MeowTactics.Managers
{
    /// <summary>
    /// Estado atual de uma sinergia, para a UI mostrar.
    /// </summary>
    public class SynergyStatus
    {
        public SynergyData data;
        public int count;
        public SynergyTier activeTier; // null se nenhum nível ativo
        public bool IsActive => activeTier != null;
    }

    /// <summary>
    /// Calcula quais sinergias estão ativas com base nos gatos posicionados
    /// (cada gato conta, mesmo repetido) e aplica os buffs correspondentes.
    /// Itens "distintivo" concedem tags extras, que também contam aqui.
    /// </summary>
    public class SynergyManager : MonoBehaviour
    {
        public static SynergyManager Instance { get; private set; }

        [Tooltip("Definições de sinergia ativas no jogo (Ninja, Sniper, Místico...)")]
        public List<SynergyData> allSynergies = new List<SynergyData>();

        private readonly List<SynergyStatus> currentStatuses = new List<SynergyStatus>();

        /// <summary>Disparado quando as sinergias mudam (para a UI atualizar).</summary>
        public event Action<List<SynergyStatus>> OnSynergiesChanged;

        private void Awake()
        {
            Instance = this;
        }

        public List<SynergyStatus> GetActiveSynergies() => currentStatuses;

        public void RecalculateSynergies()
        {
            List<CatUnit> placed = GatherPlacedCats();

            // 1) Zera buffs e reaplica itens (itens valem sempre que o gato está em campo).
            foreach (var cat in placed)
            {
                cat.ResetBuffs();
                cat.ApplyItems();
            }

            // 2) Conta cada sinergia (cada gato posicionado conta para cada tag efetiva).
            currentStatuses.Clear();
            foreach (var synergy in allSynergies)
            {
                if (synergy == null) continue;
                int count = 0;
                foreach (var cat in placed)
                    if (HasSynergy(cat, synergy.synergyType)) count++;

                var status = new SynergyStatus
                {
                    data = synergy,
                    count = count,
                    activeTier = synergy.GetActiveTier(count)
                };
                currentStatuses.Add(status);
            }

            // 3) Aplica os buffs dos níveis ativos aos gatos com a tag.
            foreach (var status in currentStatuses)
            {
                if (!status.IsActive) continue;
                foreach (var cat in placed)
                {
                    if (!HasSynergy(cat, status.data.synergyType)) continue;
                    foreach (var eff in status.activeTier.effects)
                        cat.AddBuff(eff.stat, eff.value);
                }
            }

            OnSynergiesChanged?.Invoke(currentStatuses);
        }

        private static bool HasSynergy(CatUnit cat, SynergyType type)
        {
            foreach (var s in cat.GetEffectiveSynergies())
                if (s == type) return true;
            return false;
        }

        private List<CatUnit> GatherPlacedCats()
        {
            var placed = new List<CatUnit>();
            if (MapManager.Instance != null)
            {
                foreach (var slot in MapManager.Instance.placementSlots)
                    if (slot != null && !slot.IsEmpty && slot.Occupant.IsPlaced)
                        placed.Add(slot.Occupant);
            }
            return placed;
        }
    }
}
