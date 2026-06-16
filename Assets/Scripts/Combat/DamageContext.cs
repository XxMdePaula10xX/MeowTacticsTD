using MeowTactics.Core;

namespace MeowTactics.Combat
{
    /// <summary>
    /// Pacote de modificadores que acompanha um ataque até o cálculo de dano.
    /// É montado pelo CatUnit (juntando status base + estrela + buffs de sinergia)
    /// e lido pelo DamageCalculator.
    /// </summary>
    public class DamageContext
    {
        public DamageType damageType;
        public float armorPenetration;
        public float magicPenetration;
        public float critChance;       // 0..100 (porcentagem)
        public float critMultiplier;
        public float bonusDamageMultiplier; // ex: 0.25 = +25% de dano

        public DamageContext()
        {
            damageType = DamageType.Physical;
            armorPenetration = 0f;
            magicPenetration = 0f;
            critChance = 0f;
            critMultiplier = GameBalance.CritMultiplierDefault;
            bonusDamageMultiplier = 0f;
        }
    }
}
