using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;

namespace MeowTactics.Data
{
    /// <summary>
    /// Um item equipável em um gato. Pode dar status, efeitos especiais
    /// e/ou uma sinergia extra (distintivo).
    /// Crie pelo menu: Assets > Create > MeowTactics > Item Data.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "MeowTactics/Item Data", order = 4)]
    public class ItemData : ScriptableObject
    {
        [Header("Identidade")]
        public string itemId;
        public string itemName;
        [TextArea] public string description;
        public Sprite icon;
        public Color uiColor = Color.white;

        [Header("Status (bônus no gato equipado)")]
        public List<SynergyEffect> statEffects = new List<SynergyEffect>();

        [Header("Distintivo: sinergias extras concedidas ao gato")]
        [Tooltip("Ex: um Distintivo Sniper faz o gato contar como Sniper também")]
        public List<SynergyType> grantedSynergies = new List<SynergyType>();

        [Header("Efeitos especiais")]
        [Tooltip("Dano verdadeiro extra somado a cada ataque")]
        public float bonusTrueDamagePerHit = 0f;

        [Tooltip("Ataques passam a aplicar lentidão")]
        public bool grantsSlow = false;
        [Range(0f, 1f)] public float slowAmount = 0.2f;
        public float slowDuration = 1.5f;

        [Tooltip("Ataques passam a causar dano em área")]
        public bool grantsArea = false;
        public float areaRadius = 1.5f;
    }
}
