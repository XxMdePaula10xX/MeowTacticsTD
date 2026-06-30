using MeowTactics.Core;

namespace MeowTactics.Managers
{
    /// <summary>
    /// Desafio Diário: uma run com modificador FIXO por dia (igual para todos),
    /// num mapa que também rotaciona por dia. Determinístico pela data, então o
    /// "desafio de hoje" é sempre o mesmo até virar o dia.
    /// </summary>
    public static class DailyChallenge
    {
        // name, desc, coinMult, speedMult, hpMult, livesBonus, coinsBonus
        private static readonly (string name, string desc, float coin, float spd, float hp, int lives, int coins)[] Mods =
        {
            ("Chuva de Moedas", "Ganhe o DOBRO de moedas!", 2f, 1f, 1f, 0, 0),
            ("Horda Veloz", "Inimigos 40% mais rápidos.", 1f, 1.4f, 1f, 0, 0),
            ("Couraça", "Inimigos com +50% de vida — mas comece com +6 moedas.", 1f, 1f, 1.5f, 0, 6),
            ("Por um Fio", "Apenas 8 vidas — mas +50% de moedas.", 1.5f, 1f, 1f, -12, 0),
            ("Bolso Cheio", "Comece com +10 moedas.", 1f, 1f, 1f, 0, 10),
            ("Resistência", "Inimigos com +30% de vida.", 1f, 1f, 1.3f, 0, 0),
            ("Blitz", "Inimigos +25% rápidos e +20% de vida, moedas x2.", 2f, 1.25f, 1.2f, 0, 0),
        };

        private static readonly string[] DailyMaps = { "jardim", "bosque", "ruinas" };

        public static int TodaySeed()
        {
            var now = System.DateTime.Now;
            return now.Year * 1000 + now.DayOfYear;
        }

        public static string TodayKey()
        {
            var now = System.DateTime.Now;
            return now.Year + "-" + now.DayOfYear;
        }

        private static int Index() => TodaySeed() % Mods.Length;

        public static string TodayName() => Mods[Index()].name;
        public static string TodayDesc() => Mods[Index()].desc;
        public static string TodayMapId() => DailyMaps[TodaySeed() % DailyMaps.Length];

        /// <summary>Aplica o modificador de hoje em RunMods (chame antes de iniciar a run).</summary>
        public static void Apply()
        {
            var m = Mods[Index()];
            RunMods.Reset();
            RunMods.coinMultiplier = m.coin;
            RunMods.enemySpeedMult = m.spd;
            RunMods.enemyHpMult = m.hp;
            RunMods.startingLivesBonus = m.lives;
            RunMods.startingCoinsBonus = m.coins;
            RunMods.label = m.name;
        }
    }
}
