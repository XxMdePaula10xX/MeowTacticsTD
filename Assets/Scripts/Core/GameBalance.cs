namespace MeowTactics.Core
{
    /// <summary>
    /// Constantes centralizadas de balanceamento.
    /// Mexa AQUI para ajustar regras gerais do jogo, em vez de procurar
    /// números espalhados pelo código.
    /// </summary>
    public static class GameBalance
    {
        // ---- Economia ---- (AJUSTE AQUI a dificuldade econômica)
        public const int StartingCoins = 7;     // menos moedas iniciais (era 10)
        public const int StartingLives = 20;
        public const int ShopSize = 5;
        public const int BenchSize = 8;
        public const int RerollCost = 2;
        public const int CoinsPerKill = 1;
        // Recompensa de onda = 3 + floor(onda/2)  (ver WaveManager.EndWave)
        public const int CoinsPerWaveBase = 3;
        public const float SellRatio = 0.55f;   // venda devolve 55% do investido (era 70%)

        // Chance de um inimigo "comum" (coinReward <= 1) dropar 1 moeda. Elites/boss dão sempre.
        public const float CommonCoinDropChance = 0.3f;

        // ---- Combate ----
        public const float CritMultiplierDefault = 1.5f;

        // ---- Itens ----
        public const int ItemDropEveryNWaves = 3; // escolha de item nas ondas 3, 6, 9...
        public const int ItemDraftChoices = 3;    // quantos itens aparecem para escolher
        public const int MaxItemsPerCat = 3;      // quantos itens um gato pode equipar

        // ---- Mapa / Movimento ----
        // Multiplicador que converte a "velocidade" do PRD (ex: 1.0) em
        // unidades de mundo por segundo. Ajuste se os inimigos andarem
        // rápido/devagar demais na sua cena.
        public const float EnemySpeedScale = 1.2f;

        // ---- Posicionamento ----
        public const int PlacementSlots = 12;
    }
}
