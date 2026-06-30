using System.Collections.Generic;

namespace MeowTactics.Core
{
    /// <summary>
    /// Modificadores ATIVOS da partida (Desafio Diário, Relíquias...).
    /// São campos estáticos, então sobrevivem ao recarregamento da cena.
    /// Os managers leem estes valores; o ponto de entrada (menu) os define ou reseta.
    /// </summary>
    public static class RunMods
    {
        // --- Desafio Diário ---
        public static float coinMultiplier = 1f;   // multiplica moedas ganhas
        public static float enemySpeedMult = 1f;    // x na velocidade dos inimigos
        public static float enemyHpMult = 1f;       // x na vida dos inimigos
        public static int startingLivesBonus = 0;   // soma às vidas iniciais
        public static int startingCoinsBonus = 0;   // soma às moedas iniciais
        public static string label = "";            // descrição do modificador (para a UI)

        // --- Relíquias (bônus que acumulam durante a run) ---
        public static float catDamagePct = 0f;      // +% dano em TODOS os gatos
        public static float catRangePct = 0f;       // +% alcance
        public static float catAtkSpeedPct = 0f;    // +% vel. ataque
        public static float catCritFlat = 0f;       // +% crítico (pontos)
        public static float catArmorPen = 0f;       // +pen. armadura
        public static float catMagicPen = 0f;       // +pen. mágica
        public static int extraItemSlots = 0;       // +slots de item por gato
        public static int coinsPerWaveBonus = 0;    // +moedas por onda
        public static bool sellFull = false;        // vender devolve 100%

        /// <summary>Relíquias já escolhidas (para não repetir na oferta).</summary>
        public static readonly HashSet<string> takenRelics = new HashSet<string>();

        public static void Reset()
        {
            coinMultiplier = 1f;
            enemySpeedMult = 1f;
            enemyHpMult = 1f;
            startingLivesBonus = 0;
            startingCoinsBonus = 0;
            label = "";

            catDamagePct = 0f; catRangePct = 0f; catAtkSpeedPct = 0f;
            catCritFlat = 0f; catArmorPen = 0f; catMagicPen = 0f;
            extraItemSlots = 0; coinsPerWaveBonus = 0; sellFull = false;
            takenRelics.Clear();
        }
    }
}
