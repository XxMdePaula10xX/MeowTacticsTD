#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Data;
using MeowTactics.Map;
using MeowTactics.Managers;
using MeowTactics.UI;

namespace MeowTactics.EditorTools
{
    /// <summary>
    /// Ferramentas de editor que GERAM todo o conteúdo do jogo e MONTAM a cena
    /// automaticamente. Use o menu "MeowTactics" no topo do Unity.
    ///
    /// Passo a passo recomendado:
    ///   1) MeowTactics > 1. Gerar Conteúdo
    ///   2) MeowTactics > 2. Montar Cena de Jogo
    ///   3) Aperte Play!
    /// </summary>
    public static class MeowSetup
    {
        private const string SoRoot   = "Assets/ScriptableObjects";
        private const string CatsDir  = SoRoot + "/Cats";
        private const string EnemiesDir = SoRoot + "/Enemies";
        private const string WavesDir = SoRoot + "/Waves";
        private const string SynergiesDir = SoRoot + "/Synergies";
        private const string ItemsDir = SoRoot + "/Items";

        // =====================================================================
        [MenuItem("MeowTactics/Fazer Tudo (gerar conteúdo + montar cena)", false, 0)]
        public static void DoEverything()
        {
            GenerateContent();
            BuildGameScene();
            EditorUtility.DisplayDialog("Meow Tactics TD",
                "Tudo pronto! 🎉\n\nConteúdo gerado e cena 'Game' montada.\nAperte o botão Play para jogar.", "Eba!");
        }

        // =====================================================================
        //  1) CONTEÚDO
        // =====================================================================
        [MenuItem("MeowTactics/1. Gerar Conteúdo (gatos, inimigos, ondas, itens)", false, 20)]
        public static void GenerateContent()
        {
            EnsureFolders();
            ClearFolder(CatsDir); ClearFolder(EnemiesDir); ClearFolder(WavesDir);
            ClearFolder(SynergiesDir); ClearFolder(ItemsDir);

            var enemies = GenerateEnemies();
            GenerateCats();
            GenerateSynergies();
            GenerateItems();
            GenerateWaves(enemies);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MeowTactics] Conteúdo gerado com sucesso.");
        }

        private static Dictionary<string, EnemyData> GenerateEnemies()
        {
            var dict = new Dictionary<string, EnemyData>();

            dict["ghostling"] = MakeEnemy("ghostling", "Fantasminha", 30, 0, 0, 1.0f, 1, 1,
                new Color(0.80f, 0.78f, 0.95f), "Inimigo básico.", false, 1f);
            dict["armored"] = MakeEnemy("armored", "Fantasma Blindado", 60, 30, 5, 0.75f, 1, 2,
                new Color(0.55f, 0.58f, 0.65f), "Resistente contra dano físico.", false, 1.1f);
            dict["shadow"] = MakeEnemy("shadow", "Sombra Mística", 50, 5, 30, 0.9f, 1, 2,
                new Color(0.45f, 0.30f, 0.65f), "Resistente contra dano mágico.", false, 1.05f);
            dict["swift"] = MakeEnemy("swift", "Pesadelo Veloz", 25, 0, 0, 1.8f, 1, 1,
                new Color(0.30f, 0.70f, 0.85f), "Fraco, mas muito rápido.", false, 0.85f);
            dict["king"] = MakeEnemy("king", "Rei dos Pesadelos", 700, 20, 20, 0.55f, 5, 25,
                new Color(0.35f, 0.10f, 0.45f), "Boss final do MVP.", true, 2.2f);

            return dict;
        }

        private static EnemyData MakeEnemy(string id, string name, float hp, float armor, float mr,
            float speed, int baseDmg, int coins, Color color, string desc, bool boss, float scale)
        {
            var e = ScriptableObject.CreateInstance<EnemyData>();
            e.enemyId = id; e.enemyName = name; e.description = desc; e.isBoss = boss;
            e.maxHealth = hp; e.armor = armor; e.magicResistance = mr; e.moveSpeed = speed;
            e.baseDamage = baseDmg; e.coinReward = coins; e.placeholderColor = color; e.visualScale = scale;
            TryAssignEnemySprite(e, id);
            AssetDatabase.CreateAsset(e, $"{EnemiesDir}/Enemy_{id}.asset");
            return e;
        }

        private static void GenerateCats()
        {
            // id, nome, custo, dano, [sinergias], dmgBase, atkSpeed, range, crit, cor, desc
            MakeCat("ninja", "Gato Ninja", 2, DamageType.Physical,
                new[] { SynergyType.Ninja, SynergyType.Shadow }, 12, 1.4f, 2.5f, 5,
                new Color(0.18f, 0.18f, 0.22f), "Um gato silencioso que ataca muito rápido.",
                atk: AttackType.Melee);
            MakeCat("archer", "Gato Arqueiro", 2, DamageType.Physical,
                new[] { SynergyType.Hunter, SynergyType.Forest }, 18, 0.9f, 3.5f, 10,
                new Color(0.30f, 0.65f, 0.35f), "Um gato preciso que dispara flechas.");
            MakeCat("sniper", "Gato Sniper", 3, DamageType.Physical,
                new[] { SynergyType.Sniper, SynergyType.Technology }, 40, 0.35f, 6.0f, 20,
                new Color(0.30f, 0.55f, 0.80f), "Um gato paciente que acerta de muito longe.");
            MakeCat("mage", "Gato Mago", 3, DamageType.Magical,
                new[] { SynergyType.Mystic, SynergyType.Star }, 25, 0.7f, 3.5f, 0,
                new Color(0.60f, 0.40f, 0.90f), "Um gato encantado que lança magia.",
                area: true, areaRadius: 1.5f, atk: AttackType.Magic);
            MakeCat("shaman", "Gato Xamã", 3, DamageType.Magical,
                new[] { SynergyType.Mystic, SynergyType.Support }, 10, 0.8f, 3.0f, 0,
                new Color(0.30f, 0.70f, 0.60f), "Um gato espiritual que enfraquece inimigos.",
                slow: true, slowAmount: 0.2f, slowDuration: 2f, atk: AttackType.Magic);
            MakeCat("samurai", "Gato Samurai", 4, DamageType.True,
                new[] { SynergyType.Guardian, SynergyType.Shadow }, 20, 0.6f, 2.0f, 5,
                new Color(0.80f, 0.30f, 0.30f), "Um gato honrado que corta qualquer defesa.",
                atk: AttackType.Melee);
        }

        private static void MakeCat(string id, string name, int cost, DamageType dmgType,
            SynergyType[] synergies, float dmg, float atkSpeed, float range, float crit,
            Color color, string desc, bool slow = false, float slowAmount = 0f, float slowDuration = 0f,
            bool area = false, float areaRadius = 0f, AttackType atk = AttackType.Projectile)
        {
            var c = ScriptableObject.CreateInstance<CatData>();
            c.catId = id; c.catName = name; c.cost = cost; c.damageType = dmgType;
            c.attackType = atk;
            c.synergies = new List<SynergyType>(synergies);
            c.baseDamage = dmg; c.attackSpeed = atkSpeed; c.range = range;
            c.critChance = crit; c.critMultiplier = GameBalance.CritMultiplierDefault;
            c.placeholderColor = color; c.description = desc;
            c.appliesSlow = slow; c.slowAmount = slowAmount; c.slowDuration = slowDuration;
            c.areaDamage = area; c.areaRadius = areaRadius;
            TryAssignCatSprite(c, id);
            AssetDatabase.CreateAsset(c, $"{CatsDir}/Cat_{id}.asset");
        }

        // Pastas e tamanho-alvo (altura no mundo) da arte.
        private const string CatArtDir = "Assets/Art/Cats";
        private const string EnemyArtDir = "Assets/Art/Enemies";
        private const float CatTargetHeight = 1.25f;   // altura do gato em unidades de mundo
        private const float EnemyTargetHeight = 1.0f;  // altura-base do inimigo (antes do visualScale)

        /// <summary>
        /// Se existir uma arte em "Assets/Art/Cats/gato_{id}.png", importa como Sprite
        /// (no tamanho certo) e liga ao gato. Basta soltar a arte com o nome certo.
        /// </summary>
        private static void TryAssignCatSprite(CatData c, string id)
        {
            var sprite = EnsureSpriteByHeight($"{CatArtDir}/gato_{id}.png", CatTargetHeight);
            if (sprite != null)
            {
                c.icon = sprite;
                Debug.Log($"[MeowTactics] Arte ligada ao gato '{id}'.");
            }
        }

        /// <summary>
        /// Se existir uma arte em "Assets/Art/Enemies/inimigo_{id}.png", importa como
        /// Sprite e liga ao inimigo. Basta soltar a arte com o nome certo.
        /// </summary>
        private static void TryAssignEnemySprite(EnemyData e, string id)
        {
            var sprite = EnsureSpriteByHeight($"{EnemyArtDir}/inimigo_{id}.png", EnemyTargetHeight);
            if (sprite != null)
            {
                e.icon = sprite;
                Debug.Log($"[MeowTactics] Arte ligada ao inimigo '{id}'.");
            }
        }

        /// <summary>
        /// Garante que a imagem em 'path' está importada como Sprite (recorte único)
        /// com o 'pixelsPerUnit' informado, e devolve o Sprite. Se não existir, null.
        /// Usado pelo mapa de fundo (tamanho exato).
        /// </summary>
        private static Sprite EnsureSprite(string path, float pixelsPerUnit)
        {
            if (!System.IO.File.Exists(path)) return null;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                { importer.textureType = TextureImporterType.Sprite; changed = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
                { importer.spritePixelsPerUnit = pixelsPerUnit; changed = true; }
                if (!importer.alphaIsTransparency)
                { importer.alphaIsTransparency = true; changed = true; }
                if (changed) importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// Importa a imagem como Sprite e ajusta o pixelsPerUnit para que ela tenha
        /// exatamente 'targetWorldHeight' unidades de altura na cena — assim a arte
        /// fica no tamanho certo INDEPENDENTE da resolução do arquivo gerado.
        /// </summary>
        private static Sprite EnsureSpriteByHeight(string path, float targetWorldHeight)
        {
            if (!System.IO.File.Exists(path)) return null;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return AssetDatabase.LoadAssetAtPath<Sprite>(path);

            // 1) Garante que é um Sprite (pra conseguir ler a textura).
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!importer.alphaIsTransparency)
            { importer.alphaIsTransparency = true; changed = true; }
            if (changed) importer.SaveAndReimport();

            // 2) Ajusta o PPU pela altura real da imagem.
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null && targetWorldHeight > 0f)
            {
                float ppu = Mathf.Max(1f, tex.height / targetWorldHeight);
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, ppu))
                {
                    importer.spritePixelsPerUnit = ppu;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// Importa uma arte de UI como Sprite e define a borda do 9-slice (em pixels),
        /// para a moldura não distorcer quando o painel/botão estica. Borda zero = ícone.
        /// </summary>
        private static Sprite EnsureUiSprite(string path, Vector4 border)
        {
            if (!System.IO.File.Exists(path)) return null;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                var s = new TextureImporterSettings();
                importer.ReadTextureSettings(s);
                bool changed = false;
                if (s.textureType != TextureImporterType.Sprite)
                { s.textureType = TextureImporterType.Sprite; changed = true; }
                if (s.spriteMode != (int)SpriteImportMode.Single)
                { s.spriteMode = (int)SpriteImportMode.Single; changed = true; }
                if (!s.alphaIsTransparency)
                { s.alphaIsTransparency = true; changed = true; }
                if (s.spriteBorder != border)
                { s.spriteBorder = border; changed = true; }
                if (changed) { importer.SetTextureSettings(s); importer.SaveAndReimport(); }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void GenerateSynergies()
        {
            // Ninja: velocidade de ataque
            MakeSynergy(SynergyType.Ninja, "Ninja", new Color(0.7f, 0.3f, 0.3f),
                "Gatos rápidos e focados em ataque contínuo.",
                (2, "+10% vel. ataque", new[] { (BonusStat.AttackSpeedPercent, 10f) }),
                (3, "+25% vel. ataque", new[] { (BonusStat.AttackSpeedPercent, 25f) }));

            // Sniper: alcance e penetração de armadura
            MakeSynergy(SynergyType.Sniper, "Sniper", new Color(0.3f, 0.55f, 0.85f),
                "Gatos de longo alcance e alto dano físico.",
                (2, "+15% alcance", new[] { (BonusStat.RangePercent, 15f) }),
                (4, "+30 pen. armadura", new[] { (BonusStat.ArmorPenetrationFlat, 30f) }));

            // Místico: dano (mágico)
            MakeSynergy(SynergyType.Mystic, "Místico", new Color(0.6f, 0.4f, 0.9f),
                "Gatos mágicos com dano elevado.",
                (2, "+15% dano", new[] { (BonusStat.DamagePercent, 15f) }),
                (3, "+25% dano", new[] { (BonusStat.DamagePercent, 25f) }));
        }

        private static void MakeSynergy(SynergyType type, string name, Color color, string desc,
            params (int count, string tierDesc, (BonusStat stat, float val)[] effects)[] tiers)
        {
            var s = ScriptableObject.CreateInstance<SynergyData>();
            s.synergyType = type; s.displayName = name; s.uiColor = color; s.description = desc;
            s.tiers = new List<SynergyTier>();
            foreach (var t in tiers)
            {
                var tier = new SynergyTier { requiredCount = t.count, description = t.tierDesc, effects = new List<SynergyEffect>() };
                foreach (var e in t.effects)
                    tier.effects.Add(new SynergyEffect { stat = e.stat, value = e.val });
                s.tiers.Add(tier);
            }
            AssetDatabase.CreateAsset(s, $"{SynergiesDir}/Synergy_{type}.asset");
        }

        private static void GenerateItems()
        {
            // --- Status ---
            MakeItem("claw", "Garra Afiada", "+20% de dano", new Color(0.95f, 0.6f, 0.2f),
                stat: (BonusStat.DamagePercent, 20f));
            MakeItem("paw", "Pata Veloz", "+25% velocidade de ataque", new Color(0.95f, 0.8f, 0.2f),
                stat: (BonusStat.AttackSpeedPercent, 25f));
            MakeItem("scope", "Luneta", "+20% de alcance", new Color(0.4f, 0.8f, 0.9f),
                stat: (BonusStat.RangePercent, 20f));
            MakeItem("tiger", "Olho do Tigre", "+15% de chance de crítico", new Color(0.9f, 0.5f, 0.3f),
                stat: (BonusStat.CritChancePercent, 15f));
            MakeItem("piercer", "Furador", "+20 de penetração de armadura", new Color(0.8f, 0.5f, 0.2f),
                stat: (BonusStat.ArmorPenetrationFlat, 20f));
            MakeItem("rune", "Runa Mística", "+20 de penetração mágica", new Color(0.6f, 0.4f, 0.9f),
                stat: (BonusStat.MagicPenetrationFlat, 20f));

            // --- Especiais ---
            MakeItem("phantom", "Garra Fantasma", "+15 de dano verdadeiro por ataque", new Color(0.95f, 0.9f, 0.7f),
                trueDmg: 15f);
            MakeItem("frost", "Amuleto Gélido", "Ataques deixam inimigos lentos", new Color(0.5f, 0.8f, 1f),
                slow: true);
            MakeItem("herb", "Bomba de Erva", "Ataques causam dano em área", new Color(0.5f, 0.85f, 0.4f),
                area: true);

            // --- Distintivos (sinergia extra) ---
            MakeItem("emblem_ninja", "Distintivo Ninja", "O gato também conta como NINJA", new Color(0.7f, 0.3f, 0.3f),
                emblem: SynergyType.Ninja);
            MakeItem("emblem_sniper", "Distintivo Sniper", "O gato também conta como SNIPER", new Color(0.3f, 0.55f, 0.85f),
                emblem: SynergyType.Sniper);
            MakeItem("emblem_mystic", "Distintivo Místico", "O gato também conta como MÍSTICO", new Color(0.6f, 0.4f, 0.9f),
                emblem: SynergyType.Mystic);
        }

        private static void MakeItem(string id, string name, string desc, Color color,
            (BonusStat stat, float val)? stat = null, float trueDmg = 0f, bool slow = false,
            bool area = false, SynergyType? emblem = null)
        {
            var it = ScriptableObject.CreateInstance<ItemData>();
            it.itemId = id; it.itemName = name; it.description = desc; it.uiColor = color;
            it.statEffects = new List<SynergyEffect>();
            if (stat.HasValue)
                it.statEffects.Add(new SynergyEffect { stat = stat.Value.stat, value = stat.Value.val });
            it.bonusTrueDamagePerHit = trueDmg;
            it.grantsSlow = slow; it.slowAmount = 0.2f; it.slowDuration = 1.5f;
            it.grantsArea = area; it.areaRadius = 1.5f;
            it.grantedSynergies = new List<SynergyType>();
            if (emblem.HasValue) it.grantedSynergies.Add(emblem.Value);
            AssetDatabase.CreateAsset(it, $"{ItemsDir}/Item_{id}.asset");
        }

        private static void GenerateWaves(Dictionary<string, EnemyData> e)
        {
            MakeWave(1, 0.8f, false, (e["ghostling"], 10, 1.0f));
            MakeWave(2, 0.8f, false, (e["ghostling"], 14, 1.15f));
            MakeWave(3, 0.75f, false, (e["ghostling"], 10, 1.2f), (e["swift"], 4, 1.2f));
            MakeWave(4, 0.75f, false, (e["armored"], 8, 1.2f), (e["ghostling"], 8, 1.2f));
            MakeWave(5, 0.7f, false, (e["shadow"], 10, 1.25f));
            MakeWave(6, 0.65f, false, (e["swift"], 12, 1.3f), (e["ghostling"], 8, 1.3f));
            MakeWave(7, 0.65f, false, (e["armored"], 10, 1.35f), (e["shadow"], 10, 1.35f));
            MakeWave(8, 0.6f, false, (e["ghostling"], 10, 1.4f), (e["swift"], 8, 1.4f), (e["shadow"], 7, 1.4f));
            MakeWave(9, 0.55f, false, (e["armored"], 10, 1.5f), (e["shadow"], 10, 1.5f), (e["swift"], 10, 1.5f));
            MakeWave(10, 0.9f, true, (e["king"], 1, 1.0f), (e["ghostling"], 10, 1.3f));
        }

        private static void MakeWave(int number, float interval, bool boss,
            params (EnemyData enemy, int count, float scaling)[] groups)
        {
            var w = ScriptableObject.CreateInstance<WaveData>();
            w.waveNumber = number; w.spawnInterval = interval; w.isBossWave = boss;
            w.enemies = new List<EnemySpawnInfo>();
            foreach (var g in groups)
                w.enemies.Add(new EnemySpawnInfo { enemy = g.enemy, count = g.count, scalingMultiplier = g.scaling });
            AssetDatabase.CreateAsset(w, $"{WavesDir}/Wave_{number:00}.asset");
        }

        // =====================================================================
        //  2) CENA
        // =====================================================================
        [MenuItem("MeowTactics/2. Montar Cena de Jogo", false, 21)]
        public static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- Dimensões do mundo (alinhadas ao mapa de fundo 16:9) ----
            const float orthoSize = 6f;
            const float worldHeight = orthoSize * 2f;      // 12 unidades de altura
            const float mapW = 1672f, mapH = 941f;          // resolução do mapa_noturno.png
            float worldWidth = worldHeight * (mapW / mapH); // ~21.3 unidades de largura

            // ---- Câmera ----
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.08f, 0.18f);
            camGo.AddComponent<AudioListener>();

            // ---- Fundo: mapa noturno ----
            var bgSprite = EnsureSprite("Assets/Art/Maps/mapa_noturno.png", mapH / worldHeight);
            if (bgSprite != null)
            {
                var bgGo = new GameObject("Background");
                bgGo.transform.position = new Vector3(0, 0, 0);
                var bgSr = bgGo.AddComponent<SpriteRenderer>();
                bgSr.sprite = bgSprite;
                bgSr.sortingOrder = -100; // atrás de tudo
            }

            // ---- Caminho (segue a estrada PINTADA no mapa) ----
            // Coordenadas normalizadas (x: 0..1 esq->dir, y: 0..1 topo->baixo)
            // extraídas de docs/mapa_caminho.json para casar com a estrada do mapa.
            float[,] pathNorm =
            {
                {0.009f,0.4995f},{0.2153f,0.4995f},{0.2691f,0.4835f},{0.2703f,0.2529f},
                {0.326f,0.2232f},{0.366f,0.2338f},{0.3977f,0.2657f},{0.4456f,0.2891f},
                {0.5054f,0.2891f},{0.5472f,0.2604f},{0.5831f,0.2508f},{0.6519f,0.2338f},
                {0.7087f,0.2657f},{0.7386f,0.3804f},{0.7805f,0.4697f},{0.8134f,0.4995f},{1.0f,0.4995f}
            };
            int pn = pathNorm.GetLength(0);
            var path = new Vector3[pn];
            for (int i = 0; i < pn; i++)
            {
                float nx = pathNorm[i, 0], ny = pathNorm[i, 1];
                path[i] = new Vector3((nx - 0.5f) * worldWidth, (0.5f - ny) * worldHeight, 0f);
            }

            var pathParent = new GameObject("PathPoints");
            for (int i = 0; i < path.Length; i++)
            {
                var p = new GameObject("Point_" + i);
                p.transform.SetParent(pathParent.transform, false);
                p.transform.position = path[i];
            }
            // Sem LineRenderer: a estrada já está desenhada no mapa de fundo.

            // ---- Marcadores de INÍCIO (portal) e FIM (cristal) ----
            CreateMarker(path[0], Marker.Kind.Spawn, "SpawnPortal");
            CreateMarker(path[path.Length - 1], Marker.Kind.Base, "BaseCrystal");

            // ---- MapManager + limites da área jogável (posicionamento livre) ----
            var mapGo = new GameObject("MapManager");
            var map = mapGo.AddComponent<MapManager>();
            map.pathParent = pathParent.transform;
            map.placementSlots = new List<MapSlot>();
            map.boardLeft = -worldWidth / 2f + 0.6f;
            map.boardRight = worldWidth / 2f - 0.6f;
            map.boardTop = orthoSize - 1.3f;      // logo abaixo da barra do topo
            map.boardBottom = -orthoSize + 2.9f;  // logo acima da loja
            map.pathRadius = 0.95f;

            // ---- Colisor do tabuleiro (captura cliques para posicionar) ----
            var boardGo = new GameObject("BoardClicker");
            boardGo.transform.position = new Vector3(
                (map.boardLeft + map.boardRight) / 2f,
                (map.boardBottom + map.boardTop) / 2f, 0f);
            var boardBox = boardGo.AddComponent<BoxCollider2D>();
            boardBox.size = new Vector2(map.boardRight - map.boardLeft, map.boardTop - map.boardBottom);
            boardGo.AddComponent<BoardClicker>();

            // ---- Sistemas (managers) ----
            var sys = new GameObject("GameSystems");
            sys.AddComponent<SFXManager>();
            sys.AddComponent<EconomyManager>();
            var shop = sys.AddComponent<ShopManager>();
            sys.AddComponent<BenchManager>();
            var items = sys.AddComponent<ItemManager>();
            var syn = sys.AddComponent<SynergyManager>();
            sys.AddComponent<PlacementManager>();
            var waveMgr = sys.AddComponent<WaveManager>();
            sys.AddComponent<GameManager>();
            var uiMgr = sys.AddComponent<UIManager>();

            // Liga os dados gerados aos managers.
            shop.availableCats = LoadAll<CatData>(CatsDir);
            syn.allSynergies = LoadAll<SynergyData>(SynergiesDir);
            items.allItems = LoadAll<ItemData>(ItemsDir);
            waveMgr.waves = LoadWavesSorted();

            // Liga as artes de interface (se existirem). Borda em pixels = moldura do 9-slice.
            uiMgr.panelSprite  = EnsureUiSprite("Assets/Art/UI/ui_painel.png", new Vector4(70, 70, 70, 70));
            uiMgr.buttonSprite = EnsureUiSprite("Assets/Art/UI/ui_botao.png",  new Vector4(50, 45, 50, 45));
            uiMgr.coinSprite   = EnsureUiSprite("Assets/Art/UI/ui_moeda.png",  Vector4.zero);
            uiMgr.heartSprite  = EnsureUiSprite("Assets/Art/UI/ui_vida.png",   Vector4.zero);

            // ---- Salvar ----
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
            AddSceneToBuild("Assets/Scenes/Game.unity");

            Debug.Log("[MeowTactics] Cena 'Game' montada e salva em Assets/Scenes/Game.unity");
        }

        // =====================================================================
        //  HELPERS DE EDITOR
        // =====================================================================
        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(SoRoot)) AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            foreach (var dir in new[] { "Cats", "Enemies", "Waves", "Synergies", "Items" })
                if (!AssetDatabase.IsValidFolder(SoRoot + "/" + dir))
                    AssetDatabase.CreateFolder(SoRoot, dir);
        }

        private static void ClearFolder(string dir)
        {
            if (!AssetDatabase.IsValidFolder(dir)) return;
            foreach (var guid in AssetDatabase.FindAssets("", new[] { dir }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static List<T> LoadAll<T>(string dir) where T : Object
        {
            var list = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { dir }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }
            return list;
        }

        private static List<WaveData> LoadWavesSorted()
        {
            var waves = LoadAll<WaveData>(WavesDir);
            waves.Sort((a, b) => a.waveNumber.CompareTo(b.waveNumber));
            return waves;
        }

        private static void CreateMarker(Vector3 pos, Marker.Kind kind, string name)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.AddComponent<Marker>().kind = kind;
        }

        private static void AddSceneToBuild(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == scenePath))
                scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
