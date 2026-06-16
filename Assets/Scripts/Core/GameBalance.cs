namespace MeowTactics.Core
{
    /// <summary>
    /// Constantes centralizadas de balanceamento.
    /// Mexa AQUI para ajustar regras gerais do jogo, em vez de procurar
    /// números espalhados pelo código.
    /// </summary>
    public static class GameBalance
    {
        // ---- Economia ----
        public const int StartingCoins = 10;
        public const int StartingLives = 20;
        public const int ShopSize = 5;
        public const int BenchSize = 8;
        public const int RerollCost = 2;
        public const int CoinsPerKill = 1;
        public const int CoinsPerWaveBase = 5; // recompensa = base + número da onda
        public const float SellRatio = 0.7f;   // venda devolve 70% do investido

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
