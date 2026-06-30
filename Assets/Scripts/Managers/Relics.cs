using System.Collections.Generic;
using MeowTactics.Core;

namespace MeowTactics.Managers
{
    /// <summary>Uma relíquia: bônus permanente da run (escolhida no draft a cada 5 ondas).</summary>
    public class Relic
    {
        public string id;
        public string name;
        public string desc;
        public System.Action apply;

        public Relic(string id, string name, string desc, System.Action apply)
        {
            this.id = id; this.name = name; this.desc = desc; this.apply = apply;
        }
    }

    public static class Relics
    {
        private static List<Relic> _all;
        public static List<Relic> All => _all ?? (_all = Build());

        private static List<Relic> Build()
        {
            var l = new List<Relic>
            {
                new Relic("claws", "Garras de Aço", "+25% de dano em TODOS os gatos.", () => RunMods.catDamagePct += 25f),
                new Relic("eagle", "Visão de Águia", "+25% de alcance.", () => RunMods.catRangePct += 25f),
                new Relic("reflex", "Reflexos Felinos", "+25% de velocidade de ataque.", () => RunMods.catAtkSpeedPct += 25f),
                new Relic("instinct", "Instinto Assassino", "+20% de chance de crítico.", () => RunMods.catCritFlat += 20f),
                new Relic("piercer", "Perfurante", "+40 de penetração de armadura.", () => RunMods.catArmorPen += 40f),
                new Relic("rune", "Runa Antiga", "+40 de penetração mágica.", () => RunMods.catMagicPen += 40f),
                new Relic("backpack", "Mochila Extra", "+1 slot de item por gato.", () => RunMods.extraItemSlots += 1),
                new Relic("purse", "Bolsa Furada", "+4 moedas no fim de cada onda.", () => RunMods.coinsPerWaveBonus += 4),
                new Relic("merchant", "Bom Negócio", "Vender um gato devolve 100% do valor.", () => RunMods.sellFull = true),
                new Relic("luck", "Sorte do Gato", "+30% de moedas ganhas.", () => RunMods.coinMultiplier *= 1.3f),
                new Relic("heart", "Coração Valente", "+5 vidas agora.", () => GameManager.Instance?.AddLives(5)),
                new Relic("treasure", "Tesouro Felino", "+15 moedas agora.", () => EconomyManager.Instance?.AddCoins(15)),
                new Relic("ward", "Selo Protetor", "Inimigos com -10% de vida.", () => RunMods.enemyHpMult *= 0.9f),
            };
            return l;
        }

        /// <summary>Sorteia n relíquias ainda não escolhidas nesta run.</summary>
        public static List<Relic> GetRandomChoices(int n)
        {
            var pool = new List<Relic>();
            foreach (var r in All) if (!RunMods.takenRelics.Contains(r.id)) pool.Add(r);

            var result = new List<Relic>();
            for (int i = 0; i < n && pool.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }

        public static void Take(Relic r)
        {
            if (r == null) return;
            RunMods.takenRelics.Add(r.id);
            r.apply?.Invoke();
            // Reaplica os buffs para refletir nos gatos já posicionados.
            SynergyManager.Instance?.RecalculateSynergies();
        }
    }
}
