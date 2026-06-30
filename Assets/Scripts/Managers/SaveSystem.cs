using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Data;
using MeowTactics.Cats;
using MeowTactics.Utilities;

namespace MeowTactics.Managers
{
    [System.Serializable]
    public class CatSave
    {
        public string catId;
        public float x;
        public float y;
        public List<string> items = new List<string>();
    }

    [System.Serializable]
    public class SaveData
    {
        public int wave;
        public int coins;
        public int lives;
        public string mapId = "jardim";
        public List<CatSave> placed = new List<CatSave>();
        public List<CatSave> bench = new List<CatSave>();
        public List<string> inventory = new List<string>(); // itens ganhos e ainda não equipados

        // Estado das RELÍQUIAS (para o Continuar não perder os bônus da run).
        public float rmCoinMult = 1f;
        public float rmEnemyHp = 1f;
        public float rmCatDmg, rmCatRange, rmCatAtkSpd, rmCatCrit, rmCatArmorPen, rmCatMagicPen;
        public int rmExtraSlots, rmCoinsPerWave;
        public bool rmSellFull;
        public List<string> takenRelics = new List<string>();
    }

    /// <summary>
    /// Save/Load via PlayerPrefs (JSON). Salva no início de cada preparação
    /// (moedas, vidas, onda, gatos posicionados e do banco, com itens).
    /// </summary>
    public static class SaveSystem
    {
        private const string Key = "meow_save";

        public static string CurrentMapId
        {
            get { return PlayerPrefs.GetString("currentMap", "jardim"); }
            set { PlayerPrefs.SetString("currentMap", value); }
        }

        public static bool HasSave() => PlayerPrefs.HasKey(Key);
        public static void Clear() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }

        /// <summary>Mapa gravado no save (para o Continuar carregar no mapa certo).</summary>
        public static string SavedMapId()
        {
            if (!HasSave()) return CurrentMapId;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
                return (d != null && !string.IsNullOrEmpty(d.mapId)) ? d.mapId : CurrentMapId;
            }
            catch { return CurrentMapId; }
        }

        public static int BestWave(string mapId) => PlayerPrefs.GetInt("best_" + mapId, 0);
        public static int BestEndless(string mapId) => PlayerPrefs.GetInt("bestE_" + mapId, 0);
        public static int BestDaily() => PlayerPrefs.GetInt("bestDaily_" + DailyChallenge.TodayKey(), 0);

        /// <summary>Grava o recorde no slot do modo atual (normal / infinito / diário).</summary>
        public static void RecordBest(int wave)
        {
            string k;
            if (GameManager.DailyMode) k = "bestDaily_" + DailyChallenge.TodayKey();
            else if (GameManager.EndlessMode) k = "bestE_" + CurrentMapId;
            else k = "best_" + CurrentMapId;
            if (wave > PlayerPrefs.GetInt(k, 0)) { PlayerPrefs.SetInt(k, wave); PlayerPrefs.Save(); }
        }

        public static void Save()
        {
            var d = new SaveData { mapId = CurrentMapId };
            d.coins = EconomyManager.Instance != null ? EconomyManager.Instance.Coins : 0;
            d.lives = GameManager.Instance != null ? GameManager.Instance.Lives : 0;
            d.wave  = WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveIndex : 0;

            if (PlacementManager.Instance != null)
                foreach (var c in PlacementManager.Instance.PlacedCats)
                    if (c != null) d.placed.Add(ToSave(c, true));
            if (BenchManager.Instance != null)
                foreach (var c in BenchManager.Instance.benchCats)
                    if (c != null) d.bench.Add(ToSave(c, false));
            if (ItemManager.Instance != null)
                foreach (var it in ItemManager.Instance.inventory)
                    if (it != null) d.inventory.Add(it.itemId);

            // Relíquias da run.
            d.rmCoinMult = RunMods.coinMultiplier; d.rmEnemyHp = RunMods.enemyHpMult;
            d.rmCatDmg = RunMods.catDamagePct; d.rmCatRange = RunMods.catRangePct;
            d.rmCatAtkSpd = RunMods.catAtkSpeedPct; d.rmCatCrit = RunMods.catCritFlat;
            d.rmCatArmorPen = RunMods.catArmorPen; d.rmCatMagicPen = RunMods.catMagicPen;
            d.rmExtraSlots = RunMods.extraItemSlots; d.rmCoinsPerWave = RunMods.coinsPerWaveBonus;
            d.rmSellFull = RunMods.sellFull;
            d.takenRelics = new List<string>(RunMods.takenRelics);

            PlayerPrefs.SetString(Key, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        private static CatSave ToSave(CatUnit c, bool placed)
        {
            var cs = new CatSave { catId = c.Data.catId };
            if (placed) { cs.x = c.transform.position.x; cs.y = c.transform.position.y; }
            foreach (var it in c.Items) cs.items.Add(it.itemId);
            return cs;
        }

        public static void Restore()
        {
            if (!HasSave()) return;
            SaveData d;
            try { d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key)); }
            catch { return; }
            if (d == null) return;

            // Restaura as relíquias da run ANTES dos gatos (para os buffs aplicarem).
            RunMods.coinMultiplier = d.rmCoinMult > 0f ? d.rmCoinMult : 1f;
            RunMods.enemyHpMult = d.rmEnemyHp > 0f ? d.rmEnemyHp : 1f;
            RunMods.catDamagePct = d.rmCatDmg; RunMods.catRangePct = d.rmCatRange;
            RunMods.catAtkSpeedPct = d.rmCatAtkSpd; RunMods.catCritFlat = d.rmCatCrit;
            RunMods.catArmorPen = d.rmCatArmorPen; RunMods.catMagicPen = d.rmCatMagicPen;
            RunMods.extraItemSlots = d.rmExtraSlots; RunMods.coinsPerWaveBonus = d.rmCoinsPerWave;
            RunMods.sellFull = d.rmSellFull;
            RunMods.takenRelics.Clear();
            if (d.takenRelics != null) foreach (var id in d.takenRelics) RunMods.takenRelics.Add(id);

            if (EconomyManager.Instance != null) EconomyManager.Instance.SetCoins(d.coins);
            if (GameManager.Instance != null) GameManager.Instance.SetLives(d.lives);
            if (WaveManager.Instance != null) WaveManager.Instance.SetWaveIndex(d.wave);

            if (d.bench != null)
                foreach (var cs in d.bench)
                {
                    var cat = Build(cs);
                    if (cat != null && BenchManager.Instance != null) BenchManager.Instance.AddCatToBench(cat);
                }
            if (d.placed != null)
                foreach (var cs in d.placed)
                {
                    var cat = Build(cs);
                    if (cat != null && PlacementManager.Instance != null)
                        PlacementManager.Instance.PlaceRestoredCat(cat, new Vector3(cs.x, cs.y, 0f));
                }

            // Itens do inventário (ganhos e não equipados) voltam pra você equipar.
            if (d.inventory != null && ItemManager.Instance != null)
                foreach (var id in d.inventory)
                {
                    var it = FindItem(id);
                    if (it != null) ItemManager.Instance.AddToInventory(it);
                }

            SynergyManager.Instance?.RecalculateSynergies();
        }

        private static CatUnit Build(CatSave cs)
        {
            var data = FindCat(cs.catId);
            if (data == null) return null;
            var cat = UnitFactory.CreateCat(data, Vector3.zero);
            if (cs.items != null)
                foreach (var id in cs.items)
                {
                    var it = FindItem(id);
                    if (it != null) cat.EquipItem(it);
                }
            return cat;
        }

        private static CatData FindCat(string id)
        {
            if (ShopManager.Instance == null) return null;
            foreach (var c in ShopManager.Instance.availableCats)
                if (c != null && c.catId == id) return c;
            return null;
        }

        private static ItemData FindItem(string id)
        {
            if (ItemManager.Instance == null) return null;
            foreach (var it in ItemManager.Instance.allItems)
                if (it != null && it.itemId == id) return it;
            return null;
        }
    }
}
