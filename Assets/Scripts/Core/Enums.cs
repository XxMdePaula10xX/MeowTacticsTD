namespace MeowTactics.Core
{
    /// <summary>
    /// Os três tipos de dano do jogo.
    /// Físico é reduzido por armadura, Mágico por resistência mágica,
    /// e Verdadeiro ignora as duas defesas.
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magical,
        True
    }

    /// <summary>
    /// Como o gato entrega o ataque (afeta só o visual/feedback, não o cálculo).
    /// Melee = corte perto do alvo; Projectile = dispara projétil; Magic = estouro mágico.
    /// </summary>
    public enum AttackType
    {
        Melee,
        Projectile,
        Magic
    }

    /// <summary>
    /// Todas as tags de sinergia possíveis.
    /// No MVP só Ninja, Sniper e Mystic estão totalmente balanceadas,
    /// as outras já ficam preparadas para expansão futura.
    /// </summary>
    public enum SynergyType
    {
        Ninja,
        Sniper,
        Mystic,
        Guardian,
        Hunter,
        Shadow,
        Forest,
        Technology,
        Support,
        Star
    }

    /// <summary>
    /// Estado geral da partida, controlado pelo GameManager.
    /// </summary>
    public enum GameState
    {
        Preparation,
        WaveInProgress,
        Victory,
        Defeat,
        Paused
    }

    /// <summary>
    /// Estatísticas que uma sinergia pode modificar.
    /// Usado pelo SynergyManager para aplicar buffs nos gatos certos.
    /// </summary>
    public enum BonusStat
    {
        AttackSpeedPercent,   // +X% velocidade de ataque
        CritChancePercent,    // +X pontos de chance de crítico (ex: 10 = +10%)
        ArmorPenetrationFlat, // +X penetração de armadura
        MagicPenetrationFlat, // +X penetração mágica
        DamagePercent,        // +X% dano
        RangePercent          // +X% alcance
    }
}
