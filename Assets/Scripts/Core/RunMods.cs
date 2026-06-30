namespace MeowTactics.Core
{
    /// <summary>
    /// Modificadores ATIVOS da partida (Desafio Diário, relíquias futuras...).
    /// São campos estáticos, então sobrevivem ao recarregamento da cena.
    /// Os managers leem estes valores; o ponto de entrada (menu) os define ou reseta.
    /// </summary>
    public static class RunMods
    {
        public static float coinMultiplier = 1f;   // multiplica moedas ganhas
        public static float enemySpeedMult = 1f;    // x na velocidade dos inimigos
        public static float enemyHpMult = 1f;       // x na vida dos inimigos
        public static int startingLivesBonus = 0;   // soma às vidas iniciais
        public static int startingCoinsBonus = 0;   // soma às moedas iniciais
        public static string label = "";            // descrição do modificador (para a UI)

        public static void Reset()
        {
            coinMultiplier = 1f;
            enemySpeedMult = 1f;
            enemyHpMult = 1f;
            startingLivesBonus = 0;
            startingCoinsBonus = 0;
            label = "";
        }
    }
}
