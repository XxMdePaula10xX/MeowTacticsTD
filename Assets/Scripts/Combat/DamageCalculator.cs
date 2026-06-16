using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Enemies;

namespace MeowTactics.Combat
{
    /// <summary>
    /// Resultado de um cálculo de dano, já pronto para aplicar no inimigo.
    /// </summary>
    public struct DamageResult
    {
        public float amount;
        public bool wasCritical;
    }

    /// <summary>
    /// Centraliza TODAS as fórmulas de dano do jogo (PRD seções 8).
    /// Classe utilitária pura: não guarda estado, só calcula.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Calcula o dano final que um ataque causaria em um inimigo,
        /// considerando tipo de dano, defesas, penetração, bônus e crítico.
        /// </summary>
        public static DamageResult CalculateDamage(float baseDamage, EnemyUnit enemy, DamageContext context)
        {
            // 1) Aplica bônus de dano percentual (sinergias, etc.)
            float damage = baseDamage * (1f + context.bonusDamageMultiplier);

            // 2) Reduz pela defesa de acordo com o tipo
            switch (context.damageType)
            {
                case DamageType.Physical:
                {
                    float armor = Mathf.Max(0f, enemy.CurrentArmor - context.armorPenetration);
                    damage *= 100f / (100f + armor);
                    break;
                }
                case DamageType.Magical:
                {
                    float mr = Mathf.Max(0f, enemy.CurrentMagicResistance - context.magicPenetration);
                    damage *= 100f / (100f + mr);
                    break;
                }
                case DamageType.True:
                    // Dano verdadeiro ignora todas as defesas.
                    break;
            }

            // 3) Crítico
            bool crit = false;
            if (context.critChance > 0f && Random.value * 100f < context.critChance)
            {
                damage *= context.critMultiplier;
                crit = true;
            }

            return new DamageResult { amount = damage, wasCritical = crit };
        }
    }
}
