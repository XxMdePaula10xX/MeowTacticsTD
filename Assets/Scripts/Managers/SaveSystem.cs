using System.Collections.Generic;
using UnityEngine;
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
        public string mapId = "bosque";
        public List<CatSave> placed = new List<CatSave>();
        public List<CatSave> bench = new List<CatSave>();
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
            get { return PlayerPrefs.GetString("currentMap", "bosque"); }
            set { PlayerPrefs.SetString("currentMap", value); }
        }

        public static bool HasSave() => PlayerPrefs.HasKey(Key);
        public static void Clear() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }

        public static int BestWave(string mapId) => PlayerPrefs.GetInt("best_" + mapId, 0);

        public static void RecordBest(int wave)
        {
            string k = "best_" + CurrentMapId;
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
