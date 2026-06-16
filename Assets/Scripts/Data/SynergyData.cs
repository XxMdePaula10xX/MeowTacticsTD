using System;
using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;

namespace MeowTactics.Data
{
    /// <summary>
    /// Um efeito único que uma sinergia concede (ex: +25% velocidade de ataque).
    /// </summary>
    [Serializable]
    public class SynergyEffect
    {
        public BonusStat stat;
        public float value;
    }

    /// <summary>
    /// Um "nível" de sinergia: a partir de X gatos, libera estes efeitos.
    /// Ex: requiredCount = 3, efeito = +25% AttackSpeed.
    /// </summary>
    [Serializable]
    public class SynergyTier
    {
        public int requiredCount = 2;
        public List<SynergyEffect> effects = new List<SynergyEffect>();
        [TextArea] public string description;
    }

    /// <summary>
    /// Definição completa de uma sinergia (Ninja, Sniper, Místico...).
    /// Os bônus são aplicados aos gatos posicionados que possuem esta tag.
    /// Crie pelo menu: Assets > Create > MeowTactics > Synergy Data.
    /// </summary>
    [CreateAssetMenu(fileName = "Synergy_", menuName = "MeowTactics/Synergy Data", order = 3)]
    public class SynergyData : ScriptableObject
    {
        public SynergyType synergyType;
        public string displayName;
        [TextArea] public string description;
        public Color uiColor = Color.white;

        [Tooltip("Níveis em ordem crescente de quantidade exigida")]
        public List<SynergyTier> tiers = new List<SynergyTier>();

        /// <summary>
        /// Dado um número de gatos com esta tag, retorna o melhor tier ativo
        /// (ou null se nenhum atinge o mínimo).
        /// </summary>
        public SynergyTier GetActiveTier(int count)
        {
            SynergyTier active = null;
            foreach (var tier in tiers)
            {
                if (count >= tier.requiredCount)
                {
                    if (active == null || tier.requiredCount > active.requiredCount)
                        active = tier;
                }
            }
            return active;
        }
    }
}
