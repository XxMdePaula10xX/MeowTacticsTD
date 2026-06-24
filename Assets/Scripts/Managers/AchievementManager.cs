using System.Collections.Generic;
using UnityEngine;
using MeowTactics.UI;

namespace MeowTactics.Managers
{
    /// <summary>Definição de uma conquista: desbloqueada quando um "stat" atinge o alvo.</summary>
    public class AchievementDef
    {
        public string id;
        public string title;
        public string desc;
        public string stat;   // chave do stat acompanhado
        public int target;    // valor necessário

        public AchievementDef(string id, string title, string desc, string stat, int target)
        {
            this.id = id; this.title = title; this.desc = desc; this.stat = stat; this.target = target;
        }
    }

    /// <summary>
    /// Sistema de conquistas. Acompanha "stats" persistentes (PlayerPrefs) e
    /// desbloqueia conquistas quando o stat atinge o alvo. Os eventos do jogo
    /// chamam Report()/ReportMax(). Auto-instancia (não precisa estar na cena).
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        public readonly List<AchievementDef> All = new List<AchievementDef>();

        private const string StatPrefix = "ach.stat.";
        private const string DonePrefix = "ach.done.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("AchievementManager");
            go.AddComponent<AchievementManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildList();
        }

        // ---------------------------------------------------------------
        //  Stats persistentes
        // ---------------------------------------------------------------
        public int GetStat(string key) => PlayerPrefs.GetInt(StatPrefix + key, 0);
        private void SetStat(string key, int value) => PlayerPrefs.SetInt(StatPrefix + key, value);

        public bool IsUnlocked(AchievementDef a) => PlayerPrefs.GetInt(DonePrefix + a.id, 0) == 1;

        /// <summary>Progresso atual rumo ao alvo (limitado ao alvo).</summary>
        public int Progress(AchievementDef a) => Mathf.Clamp(GetStat(a.stat), 0, a.target);

        public int UnlockedCount()
        {
            int n = 0;
            foreach (var a in All) if (IsUnlocked(a)) n++;
            return n;
        }

        /// <summary>Soma 'amount' a um stat acumulativo (kills, moedas, etc.).</summary>
        public void Report(string stat, int amount = 1)
        {
            if (amount == 0) return;
            SetStat(stat, GetStat(stat) + amount);
            PlayerPrefs.Save();
            Check(stat);
        }

        /// <summary>Guarda o MAIOR valor já visto (ex.: maior onda, mais gatos no campo).</summary>
        public void ReportMax(string stat, int value)
        {
            if (value <= GetStat(stat)) return;
            SetStat(stat, value);
            PlayerPrefs.Save();
            Check(stat);
        }

        private void Check(string stat)
        {
            int val = GetStat(stat);
            foreach (var a in All)
            {
                if (a.stat != stat) continue;
                if (IsUnlocked(a)) continue;
                if (val >= a.target) Unlock(a);
            }
        }

        private void Unlock(AchievementDef a)
        {
            PlayerPrefs.SetInt(DonePrefix + a.id, 1);
            PlayerPrefs.Save();
            UIManager.Instance?.ShowMessage($"★ Conquista desbloqueada: {a.title}!");
            SFXManager.Play(SfxType.Synergy);
        }

        // ---------------------------------------------------------------
        //  Lista das conquistas (35)
        // ---------------------------------------------------------------
        private void Add(string id, string title, string desc, string stat, int target)
            => All.Add(new AchievementDef(id, title, desc, stat, target));

        private void BuildList()
        {
            if (All.Count > 0) return;

            // Progresso / ondas
            Add("w3", "Primeiros Passos", "Chegue à onda 3", "bestWave", 3);
            Add("w5", "Aquecendo", "Chegue à onda 5", "bestWave", 5);
            Add("w8", "Quase Lá", "Chegue à onda 8", "bestWave", 8);
            Add("win1", "Vitória!", "Vença uma partida", "wins", 1);
            Add("win3", "Defensor do Reino", "Vença 3 partidas", "wins", 3);
            Add("win10", "Lenda Felina", "Vença 10 partidas", "wins", 10);
            Add("waves25", "Maratonista", "Complete 25 ondas no total", "waves", 25);
            Add("waves100", "Veterano", "Complete 100 ondas no total", "waves", 100);
            Add("waves500", "Imparável", "Complete 500 ondas no total", "waves", 500);

            // Abates
            Add("kill100", "Caçador de Pesadelos", "Derrote 100 inimigos", "kills", 100);
            Add("kill1k", "Exterminador", "Derrote 1.000 inimigos", "kills", 1000);
            Add("kill5k", "Tempestade Felina", "Derrote 5.000 inimigos", "kills", 5000);
            Add("kill10k", "Apocalipse Miau", "Derrote 10.000 inimigos", "kills", 10000);
            Add("boss1", "Mata-Chefe", "Derrote um boss", "bossKills", 1);
            Add("boss10", "Pesadelo dos Chefes", "Derrote 10 bosses", "bossKills", 10);

            // Economia
            Add("coin100", "Mealheiro", "Acumule 100 moedas no total", "coins", 100);
            Add("coin1k", "Comerciante", "Acumule 1.000 moedas no total", "coins", 1000);
            Add("coin10k", "Magnata", "Acumule 10.000 moedas no total", "coins", 10000);
            Add("reroll10", "Indeciso", "Atualize a loja 10 vezes", "rerolls", 10);
            Add("reroll100", "Viciado em Reroll", "Atualize a loja 100 vezes", "rerolls", 100);

            // Gatos
            Add("buy1", "Primeiro Amigo", "Compre seu primeiro gato", "catsBought", 1);
            Add("buy25", "Colecionador", "Compre 25 gatos", "catsBought", 25);
            Add("buy100", "Doido por Gatos", "Compre 100 gatos", "catsBought", 100);
            Add("place5", "Esquadrão", "Tenha 5 gatos no campo ao mesmo tempo", "maxCatsPlaced", 5);
            Add("place8", "Exército Felino", "Tenha 8 gatos no campo ao mesmo tempo", "maxCatsPlaced", 8);

            // Itens
            Add("item1", "Equipado", "Equipe seu primeiro item", "itemsEquipped", 1);
            Add("item10", "Ferreiro", "Equipe 10 itens", "itemsEquipped", 10);
            Add("item50", "Arsenal", "Equipe 50 itens", "itemsEquipped", 50);

            // Sinergias
            Add("syn1", "Sintonia", "Ative sua primeira sinergia", "synergiesActivated", 1);
            Add("syn3", "Combo!", "Tenha 3 sinergias ativas na mesma partida", "maxSynergiesInMatch", 3);
            Add("syn5", "Mestre Tático", "Tenha 5 sinergias ativas na mesma partida", "maxSynergiesInMatch", 5);
            Add("syn50", "Estrategista", "Ative 50 sinergias no total", "synergiesActivated", 50);

            // Partidas
            Add("play1", "Bem-vindo!", "Jogue sua primeira partida", "games", 1);
            Add("play10", "Habitué", "Jogue 10 partidas", "games", 10);
            Add("play50", "Fã de Carteirinha", "Jogue 50 partidas", "games", 50);
        }
    }
}
