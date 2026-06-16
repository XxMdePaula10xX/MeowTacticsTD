using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;

namespace MeowTactics.Data
{
    /// <summary>
    /// Dados base (imutáveis) de um tipo de gato.
    /// Cada gato do jogo é um asset criado a partir deste ScriptableObject.
    /// Crie pelo menu: Assets > Create > MeowTactics > Cat Data.
    /// </summary>
    [CreateAssetMenu(fileName = "Cat_", menuName = "MeowTactics/Cat Data", order = 0)]
    public class CatData : ScriptableObject
    {
        [Header("Identidade")]
        public string catId;
        public string catName;
        [TextArea] public string description;

        [Header("Custo")]
        public int cost = 2;

        [Header("Combate")]
        public DamageType damageType = DamageType.Physical;
        public float baseDamage = 10f;
        [Tooltip("Ataques por segundo")] public float attackSpeed = 1f;
        [Tooltip("Alcance em unidades de mundo")] public float range = 3f;
        [Tooltip("Chance de crítico em % (0..100)")] public float critChance = 0f;
        public float critMultiplier = GameBalance.CritMultiplierDefault;

        [Header("Sinergias")]
        public List<SynergyType> synergies = new List<SynergyType>();

        [Header("Habilidade especial (opcional)")]
        [Tooltip("Se marcado, o ataque deste gato causa lentidão (ex: Xamã)")]
        public bool appliesSlow = false;
        [Range(0f, 1f)] public float slowAmount = 0.2f;   // 0.2 = -20% de velocidade
        public float slowDuration = 2f;

        [Tooltip("Se marcado, o ataque atinge em área (ex: Mago)")]
        public bool areaDamage = false;
        public float areaRadius = 1.5f;

        [Header("Visual")]
        public Sprite icon;
        public GameObject catPrefab;
        public Color placeholderColor = Color.white;
    }
}
