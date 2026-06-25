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
            SetAppIcon();
            EditorUtility.DisplayDialog("Meow Tactics TD",
                "Tudo pronto! 🎉\n\nConteúdo gerado e cena 'Game' montada.\nAperte o botão Play para jogar.", "Eba!");
        }

        // =====================================================================
        //  ÍCONE DO APP
        // =====================================================================
        private const string AppIconPath = "Assets/Art/UI/app_icon.png";

        /// <summary>
        /// Define o ícone do app a partir de "Assets/Art/UI/app_icon.png" (1024x1024,
        /// quadrado, sem transparência). Aplica como ícone padrão e para iOS.
        /// </summary>
        [MenuItem("MeowTactics/Definir Ícone do App", false, 40)]
        public static void SetAppIcon()
        {
            if (!System.IO.File.Exists(AppIconPath))
            {
                Debug.LogWarning("[MeowTactics] Ícone não encontrado. Salve a imagem (1024x1024) em " + AppIconPath);
                return;
            }

            // Importa como textura padrão e opaca (a Apple não aceita ícone com transparência).
            var importer = AssetImporter.GetAtPath(AppIconPath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; changed = true; }
                if (importer.alphaIsTransparency) { importer.alphaIsTransparency = false; changed = true; }
                if (importer.maxTextureSize < 1024) { importer.maxTextureSize = 1024; changed = true; }
                if (changed) importer.SaveAndReimport();
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (tex == null) { Debug.LogWarning("[MeowTactics] Falha ao carregar o ícone em " + AppIconPath); return; }

            // Ícone padrão (vale para plataformas sem ícone específico).
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { tex });

            // iOS: preenche todos os tamanhos com o mesmo ícone (Unity reescala no build).
            var sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.iOS);
            if (sizes != null && sizes.Length > 0)
            {
                var icons = new Texture2D[sizes.Length];
                for (int i = 0; i < icons.Length; i++) icons[i] = tex;
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, icons);
            }

            Debug.Log("[MeowTactics] Ícone do app definido a partir de " + AppIconPath);
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

            dict["ghostling"] = MakeEnemy("ghostling", "Fantasminha", 36, 0, 0, 1.0f, 1, 1,
                new Color(0.80f, 0.78f, 0.95f), "Inimigo básico.", false, 1f);
            dict["armored"] = MakeEnemy("armored", "Fantasma Blindado", 68, 45, 10, 0.75f, 1, 2,
                new Color(0.55f, 0.58f, 0.65f), "Muito resistente a dano físico (precisa de penetração).", false, 1.1f);
            dict["shadow"] = MakeEnemy("shadow", "Sombra Mística", 56, 10, 45, 0.9f, 1, 2,
                new Color(0.45f, 0.30f, 0.65f), "Muito resistente a dano mágico (precisa de penetração mágica).", false, 1.05f);
            dict["swift"] = MakeEnemy("swift", "Pesadelo Veloz", 26, 0, 0, 1.8f, 1, 1,
                new Color(0.30f, 0.70f, 0.85f), "Fraco, mas muito rápido.", false, 0.85f);

            // --- Novos inimigos com resistências distintas ---
            dict["bulwark"] = MakeEnemy("bulwark", "Golem de Pedra", 130, 65, 0, 0.55f, 2, 3,
                new Color(0.55f, 0.50f, 0.42f), "Armadura altíssima: só cede a penetração de armadura ou dano verdadeiro.", false, 1.25f);
            dict["wraith"] = MakeEnemy("wraith", "Alma Penada", 78, 0, 60, 1.05f, 1, 3,
                new Color(0.40f, 0.85f, 0.78f), "Resistência mágica altíssima: precisa de penetração mágica ou dano físico.", false, 1.0f);
            dict["warden"] = MakeEnemy("warden", "Sentinela Maldita", 96, 32, 32, 0.8f, 2, 3,
                new Color(0.62f, 0.45f, 0.30f), "Resistente a tudo: dano verdadeiro (Samurai/Monge) é a melhor resposta.", false, 1.1f);

            dict["king"] = MakeEnemy("king", "Rei dos Pesadelos", 850, 32, 32, 0.55f, 5, 25,
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
            MakeCat("ninja", "Gato Ninja", 3, DamageType.Physical,
                new[] { SynergyType.Ninja, SynergyType.Shadow, SynergyType.Assassin }, 10, 1.4f, 2.5f, 5,
                new Color(0.18f, 0.18f, 0.22f), "Um gato silencioso que ataca muito rápido.",
                atk: AttackType.Melee);
            MakeCat("archer", "Gato Arqueiro", 3, DamageType.Physical,
                new[] { SynergyType.Hunter, SynergyType.Forest, SynergyType.Adventurer }, 15, 0.9f, 3.5f, 10,
                new Color(0.30f, 0.65f, 0.35f), "Um gato preciso que dispara flechas.");
            MakeCat("sniper", "Gato Sniper", 4, DamageType.Physical,
                new[] { SynergyType.Sniper, SynergyType.Technology, SynergyType.Assassin }, 34, 0.35f, 6.0f, 20,
                new Color(0.30f, 0.55f, 0.80f), "Um gato paciente que acerta de muito longe.");
            MakeCat("mage", "Gato Mago", 4, DamageType.Magical,
                new[] { SynergyType.Mystic, SynergyType.Elemental, SynergyType.Adventurer }, 20, 0.7f, 3.5f, 0,
                new Color(0.60f, 0.40f, 0.90f), "Um gato encantado que lança magia em área.",
                area: true, areaRadius: 1.5f, atk: AttackType.Magic);
            MakeCat("shaman", "Gato Xamã", 3, DamageType.Magical,
                new[] { SynergyType.Support, SynergyType.Elemental, SynergyType.Adventurer }, 8, 0.8f, 3.0f, 0,
                new Color(0.30f, 0.70f, 0.60f), "Um gato espiritual que enfraquece inimigos.",
                slow: true, slowAmount: 0.2f, slowDuration: 2f, atk: AttackType.Magic);
            MakeCat("samurai", "Gato Samurai", 5, DamageType.True,
                new[] { SynergyType.Guardian, SynergyType.Shadow, SynergyType.Adventurer }, 17, 0.6f, 2.0f, 5,
                new Color(0.80f, 0.30f, 0.30f), "Um gato honrado que corta qualquer defesa (dano verdadeiro).",
                atk: AttackType.Melee);

            // --- Novos gatos ---
            MakeCat("pirate", "Gato Pirata", 3, DamageType.Physical,
                new[] { SynergyType.Hunter, SynergyType.Technology }, 24, 0.6f, 4.0f, 8,
                new Color(0.72f, 0.32f, 0.26f), "Dispara balas de canhão que explodem em área.",
                area: true, areaRadius: 1.6f, atk: AttackType.Projectile);
            MakeCat("witch", "Gata Feiticeira", 4, DamageType.Magical,
                new[] { SynergyType.Mystic, SynergyType.Support, SynergyType.Elemental }, 14, 0.7f, 3.3f, 0,
                new Color(0.45f, 0.25f, 0.62f), "Conjura geada que causa dano em área e deixa lento.",
                slow: true, slowAmount: 0.25f, slowDuration: 2f, area: true, areaRadius: 1.4f, atk: AttackType.Magic);
            MakeCat("wolf", "Gato Lobo", 3, DamageType.Physical,
                new[] { SynergyType.Hunter, SynergyType.Shadow, SynergyType.Assassin }, 11, 1.6f, 1.9f, 28,
                new Color(0.45f, 0.45f, 0.52f), "Fera veloz e selvagem com altíssima chance de crítico.",
                atk: AttackType.Melee);
            MakeCat("monk", "Gato Monge", 5, DamageType.True,
                new[] { SynergyType.Guardian, SynergyType.Star, SynergyType.Support }, 18, 0.95f, 2.2f, 12,
                new Color(0.92f, 0.62f, 0.26f), "Mestre do caratê felino: golpes de dano verdadeiro.",
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
        private const string ItemArtDir = "Assets/Art/Items";
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

        private static void TryAssignItemSprite(ItemData it, string id)
        {
            var sprite = EnsureSpriteByHeight($"{ItemArtDir}/item_{id}.png", 1f);
            if (sprite != null) it.icon = sprite;
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
            // Ninja: velocidade de ataque (e dano no nível alto)
            MakeSynergy(SynergyType.Ninja, "Ninja", new Color(0.7f, 0.3f, 0.3f),
                "Gatos rápidos e focados em ataque contínuo.",
                (2, "+20% vel. ataque", new[] { (BonusStat.AttackSpeedPercent, 20f) }),
                (3, "+45% vel. e +10% dano",
                    new[] { (BonusStat.AttackSpeedPercent, 45f), (BonusStat.DamagePercent, 10f) }));

            // Sniper: alcance + penetração de armadura (essencial contra blindados)
            MakeSynergy(SynergyType.Sniper, "Sniper", new Color(0.3f, 0.55f, 0.85f),
                "Gatos de longo alcance que furam armadura.",
                (2, "+20% alc. e +30 pen.arm.",
                    new[] { (BonusStat.RangePercent, 20f), (BonusStat.ArmorPenetrationFlat, 30f) }),
                (3, "+25% dano e +60 pen.arm.",
                    new[] { (BonusStat.DamagePercent, 25f), (BonusStat.ArmorPenetrationFlat, 60f) }));

            // Místico: dano mágico + penetração mágica (essencial contra sombras)
            MakeSynergy(SynergyType.Mystic, "Místico", new Color(0.6f, 0.4f, 0.9f),
                "Gatos mágicos com dano elevado.",
                (2, "+20% dano", new[] { (BonusStat.DamagePercent, 20f) }),
                (3, "+45% dano e +25 pen.mág.",
                    new[] { (BonusStat.DamagePercent, 45f), (BonusStat.MagicPenetrationFlat, 25f) }));

            // Guardião: força bruta — dano e crítico.
            MakeSynergy(SynergyType.Guardian, "Guardião", new Color(0.85f, 0.45f, 0.40f),
                "Gatos resistentes que batem forte.",
                (2, "+15% dano", new[] { (BonusStat.DamagePercent, 15f) }),
                (3, "+30% dano e +15% crítico",
                    new[] { (BonusStat.DamagePercent, 30f), (BonusStat.CritChancePercent, 15f) }));

            // Caçador: velocidade de ataque.
            MakeSynergy(SynergyType.Hunter, "Caçador", new Color(0.45f, 0.80f, 0.40f),
                "Gatos ágeis que atacam sem parar.",
                (2, "+18% vel. ataque", new[] { (BonusStat.AttackSpeedPercent, 18f) }),
                (3, "+35% vel. e +10% dano",
                    new[] { (BonusStat.AttackSpeedPercent, 35f), (BonusStat.DamagePercent, 10f) }));

            // Sombra: crítico (golpes traiçoeiros).
            MakeSynergy(SynergyType.Shadow, "Sombra", new Color(0.55f, 0.40f, 0.75f),
                "Gatos furtivos que acertam pontos fracos.",
                (2, "+20% crítico", new[] { (BonusStat.CritChancePercent, 20f) }),
                (3, "+40% crítico e +12% dano",
                    new[] { (BonusStat.CritChancePercent, 40f), (BonusStat.DamagePercent, 12f) }));

            // Floresta: alcance.
            MakeSynergy(SynergyType.Forest, "Floresta", new Color(0.40f, 0.70f, 0.45f),
                "Gatos da mata que enxergam longe.",
                (2, "+15% alcance", new[] { (BonusStat.RangePercent, 15f) }),
                (3, "+30% alcance e +10% dano",
                    new[] { (BonusStat.RangePercent, 30f), (BonusStat.DamagePercent, 10f) }));

            // Tecnologia: cadência + penetração de armadura.
            MakeSynergy(SynergyType.Technology, "Tecnologia", new Color(0.35f, 0.75f, 0.90f),
                "Gatos equipados com engenhocas precisas.",
                (2, "+20% vel. ataque", new[] { (BonusStat.AttackSpeedPercent, 20f) }),
                (3, "+35% vel. e +30 pen.arm.",
                    new[] { (BonusStat.AttackSpeedPercent, 35f), (BonusStat.ArmorPenetrationFlat, 30f) }));

            // Suporte: dano para o time (buff geral).
            MakeSynergy(SynergyType.Support, "Suporte", new Color(0.40f, 0.80f, 0.70f),
                "Gatos que fortalecem os aliados.",
                (2, "+12% dano", new[] { (BonusStat.DamagePercent, 12f) }),
                (3, "+25% dano", new[] { (BonusStat.DamagePercent, 25f) }));

            // Estrela: poder bruto raro.
            MakeSynergy(SynergyType.Star, "Estrela", new Color(1f, 0.82f, 0.32f),
                "Gatos lendários de poder estelar.",
                (2, "+20% dano", new[] { (BonusStat.DamagePercent, 20f) }),
                (3, "+40% dano e +20% crítico",
                    new[] { (BonusStat.DamagePercent, 40f), (BonusStat.CritChancePercent, 20f) }));

            // === Sinergias de ORIGEM (combinam classes diferentes) ===

            // Assassino: Ninja + Sniper + Lobo — crítico devastador.
            MakeSynergy(SynergyType.Assassin, "Assassino", new Color(0.85f, 0.20f, 0.35f),
                "Golpes precisos e mortais (Ninja, Sniper, Lobo).",
                (2, "+25% crítico", new[] { (BonusStat.CritChancePercent, 25f) }),
                (3, "+55% crítico e +20% dano",
                    new[] { (BonusStat.CritChancePercent, 55f), (BonusStat.DamagePercent, 20f) }));

            // Aventureiros: Arqueiro + Mago + Xamã + Samurai — o grupo clássico.
            MakeSynergy(SynergyType.Adventurer, "Aventureiros", new Color(0.95f, 0.70f, 0.25f),
                "A guilda de heróis (Arqueiro, Mago, Xamã, Samurai).",
                (2, "+15% dano", new[] { (BonusStat.DamagePercent, 15f) }),
                (4, "+35% dano e +25% vel. ataque",
                    new[] { (BonusStat.DamagePercent, 35f), (BonusStat.AttackSpeedPercent, 25f) }));

            // Elemental: Mago + Xamã + Feiticeira — magos que rasgam resistência mágica.
            MakeSynergy(SynergyType.Elemental, "Elemental", new Color(0.45f, 0.85f, 0.95f),
                "Conjuradores que dominam os elementos.",
                (2, "+20% dano", new[] { (BonusStat.DamagePercent, 20f) }),
                (3, "+45% dano e +35 pen. mágica",
                    new[] { (BonusStat.DamagePercent, 45f), (BonusStat.MagicPenetrationFlat, 35f) }));
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
            // --- Status (mais fortes: recompensam investir no gato certo) ---
            MakeItem("claw", "Garra Afiada", "+35% de dano", new Color(0.95f, 0.6f, 0.2f),
                stat: (BonusStat.DamagePercent, 35f));
            MakeItem("paw", "Pata Veloz", "+40% velocidade de ataque", new Color(0.95f, 0.8f, 0.2f),
                stat: (BonusStat.AttackSpeedPercent, 40f));
            MakeItem("scope", "Luneta", "+30% de alcance", new Color(0.4f, 0.8f, 0.9f),
                stat: (BonusStat.RangePercent, 30f));
            MakeItem("tiger", "Olho do Tigre", "+25% de chance de crítico", new Color(0.9f, 0.5f, 0.3f),
                stat: (BonusStat.CritChancePercent, 25f));
            MakeItem("piercer", "Furador", "+45 de penetração de armadura", new Color(0.8f, 0.5f, 0.2f),
                stat: (BonusStat.ArmorPenetrationFlat, 45f));
            MakeItem("rune", "Runa Mística", "+45 de penetração mágica", new Color(0.6f, 0.4f, 0.9f),
                stat: (BonusStat.MagicPenetrationFlat, 45f));

            // --- Especiais ---
            MakeItem("phantom", "Garra Fantasma", "+22 de dano verdadeiro por ataque", new Color(0.95f, 0.9f, 0.7f),
                trueDmg: 22f);
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
            it.grantsSlow = slow; it.slowAmount = 0.3f; it.slowDuration = 2f;
            it.grantsArea = area; it.areaRadius = 2f;
            it.grantedSynergies = new List<SynergyType>();
            if (emblem.HasValue) it.grantedSynergies.Add(emblem.Value);
            TryAssignItemSprite(it, id);
            AssetDatabase.CreateAsset(it, $"{ItemsDir}/Item_{id}.asset");
        }

        private static void GenerateWaves(Dictionary<string, EnemyData> e)
        {
            // O escalonamento de vida/armadura/RM por onda é GLOBAL (ver EnemyUnit.Initialize:
            // +18% vida/onda etc.). Aqui o multiplicador de grupo fica 1.0; cada onda traz
            // uma COMPOSIÇÃO diferente (variedade) e mais inimigos.
            MakeWave(1,  0.80f, false, (e["ghostling"], 12, 1f));                                   // básica
            MakeWave(2,  0.75f, false, (e["ghostling"], 12, 1f), (e["swift"], 5, 1f));              // intro rápidos
            MakeWave(3,  0.50f, false, (e["swift"], 16, 1f), (e["ghostling"], 4, 1f));              // enxame rápido
            MakeWave(4,  0.70f, false, (e["armored"], 10, 1f), (e["ghostling"], 6, 1f));            // blindados (pen. armadura)
            MakeWave(5,  0.70f, false, (e["shadow"], 10, 1f), (e["wraith"], 4, 1f));                // resist. mágica (pen. mágica)
            MakeWave(6,  0.60f, false, (e["bulwark"], 4, 1f), (e["armored"], 6, 1f), (e["swift"], 6, 1f)); // físico pesado
            MakeWave(7,  0.55f, false, (e["wraith"], 8, 1f), (e["shadow"], 8, 1f), (e["swift"], 6, 1f));   // mágico pesado
            MakeWave(8,  0.70f, false, (e["king"], 1, 0.5f), (e["warden"], 6, 1f), (e["bulwark"], 3, 1f)); // mini-boss + resistentes
            MakeWave(9,  0.55f, false, (e["warden"], 8, 1f), (e["bulwark"], 4, 1f), (e["wraith"], 8, 1f)); // tudo resistente (dano verdadeiro!)
            MakeWave(10, 0.70f, true,  (e["king"], 1, 1f), (e["warden"], 6, 1f), (e["swift"], 10, 1f));    // mini-boss

            // Ondas 11-50: geradas em sequência. O escalonamento global (vida/armadura)
            // já cresce a cada onda; aqui variamos a COMPOSIÇÃO e a quantidade.
            // Mini-boss a cada 10 ondas (20/30/40) e BOSS FINAL na onda 50.
            for (int n = 11; n <= 50; n++)
            {
                float interval = Mathf.Max(0.35f, 0.72f - 0.006f * (n - 11));
                int big = 8 + n / 4;
                int mid = 5 + n / 6;
                int small = 3 + n / 8;
                bool finalBoss = (n == 50);
                bool miniBoss = (n % 10 == 0); // 20, 30, 40

                var g = new List<(EnemyData enemy, int count, float scaling)>();
                switch (n % 5)
                {
                    case 0: // enxame veloz
                        g.Add((e["swift"], big + 6, 1f)); g.Add((e["ghostling"], mid, 1f)); break;
                    case 1: // físico pesado
                        g.Add((e["armored"], big, 1f)); g.Add((e["bulwark"], small, 1f)); g.Add((e["swift"], mid, 1f)); break;
                    case 2: // mágico pesado
                        g.Add((e["shadow"], big, 1f)); g.Add((e["wraith"], mid, 1f)); g.Add((e["swift"], small, 1f)); break;
                    case 3: // resistentes mistos
                        g.Add((e["warden"], mid, 1f)); g.Add((e["bulwark"], small, 1f)); g.Add((e["wraith"], mid, 1f)); break;
                    default: // caos (um pouco de tudo)
                        g.Add((e["armored"], mid, 1f)); g.Add((e["shadow"], mid, 1f));
                        g.Add((e["swift"], mid, 1f)); g.Add((e["warden"], small, 1f)); break;
                }

                if (miniBoss) g.Insert(0, (e["king"], 1, 0.45f + 0.05f * (n / 10)));
                if (finalBoss)
                {
                    g.Clear();
                    g.Add((e["king"], 1, 1.4f));
                    g.Add((e["warden"], 12, 1f));
                    g.Add((e["bulwark"], 8, 1f));
                    g.Add((e["swift"], 14, 1f));
                }

                MakeWave(n, interval, finalBoss, g.ToArray());
            }
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
            // Mantém o mapa inteiro visível em qualquer proporção (iPhone/iPad).
            var camFit = camGo.AddComponent<MeowTactics.Utilities.CameraFit>();
            camFit.mapWorldWidth = worldWidth;
            camFit.mapWorldHeight = worldHeight;

            // ---- Fundo + caminhos do mapa PADRÃO (bosque). Em runtime, o
            //      RuntimeMapBuilder troca tudo para o mapa que o jogador escolher. ----
            var bgSprite = MapBgSprite("bosque", mapH, worldHeight);
            if (bgSprite != null)
            {
                var bgGo = new GameObject("Background");
                bgGo.transform.position = new Vector3(0, 0, 0);
                var bgSr = bgGo.AddComponent<SpriteRenderer>();
                bgSr.sprite = bgSprite;
                bgSr.sortingOrder = -100; // atrás de tudo
            }

            // Caminhos do bosque (de MapPaths_bosque.json se você ajustou, senão o padrão).
            var bosqueLanes = MapLanesWorld("bosque", worldWidth, worldHeight);
            var pathParents = new List<Transform>();
            for (int i = 0; i < bosqueLanes.Count; i++)
                pathParents.Add(BuildPathParentFromVec("Path_" + (char)('A' + i), bosqueLanes[i]).transform);

            // Marcadores de início (portal) e fim (cristal), um por trilha (deduplicados).
            PlaceMarkersForLanes(bosqueLanes);

            // ---- MapManager + limites da área jogável (posicionamento livre) ----
            var mapGo = new GameObject("MapManager");
            var map = mapGo.AddComponent<MapManager>();
            map.pathParents = pathParents;
            map.placementSlots = new List<MapSlot>();
            map.boardLeft = -worldWidth / 2f + 0.6f;
            map.boardRight = worldWidth / 2f - 0.6f;
            map.boardTop = orthoSize - 1.3f;      // logo abaixo da barra do topo
            map.boardBottom = -orthoSize + 2.9f;  // logo acima da loja
            map.pathRadius = 0.95f;

            // ---- Suporte a MÚLTIPLOS MAPAS ----
            // A cena já vem montada com o "bosque". Em runtime, o RuntimeMapBuilder
            // troca fundo/caminhos/marcadores se o jogador escolher outro mapa.
            // Mapas novos sem arte própria usam o fundo noturno como fallback.
            var mapBuilderGo = new GameObject("RuntimeMapBuilder");
            var builder = mapBuilderGo.AddComponent<RuntimeMapBuilder>();
            builder.bakedMapId = "bosque";
            // Cada mapa usa seus pontos salvos (MapPaths_<id>.json) se existirem, senão o
            // traçado padrão por código. Mapas sem arte própria ficam com fundo nulo
            // (campo escuro) até você adicionar a PNG em Assets/Art/Maps.
            builder.maps = new List<RuntimeMapBuilder.MapDef>
            {
                MakeMapDef("bosque", bgSprite, bosqueLanes),
                MakeMapDef("jardim", MapBgSprite("jardim", mapH, worldHeight),
                    MapLanesWorld("jardim", worldWidth, worldHeight)),
                MakeMapDef("ruinas", MapBgSprite("ruinas", mapH, worldHeight),
                    MapLanesWorld("ruinas", worldWidth, worldHeight))
            };

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
            sys.AddComponent<MusicManager>();
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

            // Painéis/botões agora usam o retângulo arredondado procedural (UISprites.Rounded),
            // gerado em runtime e tingido pela cor — visual mais limpo que as artes de madeira.
            uiMgr.panelSprite  = null;
            uiMgr.buttonSprite = null;
            uiMgr.coinSprite   = EnsureUiSprite("Assets/Art/UI/ui_moeda.png",  Vector4.zero);
            uiMgr.heartSprite  = EnsureUiSprite("Assets/Art/UI/ui_vida.png",   Vector4.zero);
            uiMgr.menuBgSprite = EnsureUiSprite("Assets/Art/UI/menu_bg.png",   Vector4.zero);

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

        // ---- Traçados padrão dos mapas (coords normalizadas: x esq->dir, y topo->baixo) ----
        // BOSQUE: rota base (a de baixo é o espelho vertical desta). Casa com mapa_noturno.png.
        private static readonly float[,] BosqueNorm =
        {
            {0.009f,0.4995f},{0.2153f,0.4995f},{0.2691f,0.4835f},{0.2703f,0.2529f},
            {0.326f,0.2232f},{0.366f,0.2338f},{0.3977f,0.2657f},{0.4456f,0.2891f},
            {0.5054f,0.2891f},{0.5472f,0.2604f},{0.5831f,0.2508f},{0.6519f,0.2338f},
            {0.7087f,0.2657f},{0.7386f,0.3804f},{0.7805f,0.4697f},{0.8134f,0.4995f},{1.0f,0.4995f}
        };

        // JARDIM: um ÚNICO caminho em "W" (dois vales) — casa com a arte do jardim:
        // entra à esquerda, desce ao 1º vale, sobe ao pico central, desce ao 2º vale,
        // sobe e sai à direita.
        private static readonly float[,] JardimNorm =
        {
            {0.00f,0.18f},{0.06f,0.18f},{0.14f,0.42f},{0.25f,0.66f},{0.36f,0.50f},
            {0.50f,0.32f},{0.64f,0.50f},{0.75f,0.66f},{0.86f,0.42f},{0.94f,0.18f},{1.00f,0.18f}
        };

        // RUÍNAS: TRÊS trilhas entram pela esquerda (cima/meio/baixo) e CONVERGEM no
        // cristal (~78% da largura, recuado da borda direita) — casa com a arte das ruínas.
        private static readonly float[,] RuinasTopNorm =
        {
            {0.03f,0.22f},{0.22f,0.20f},{0.42f,0.21f},{0.57f,0.32f},{0.70f,0.43f},{0.79f,0.47f}
        };
        private static readonly float[,] RuinasMidNorm =
        {
            {0.03f,0.47f},{0.30f,0.47f},{0.56f,0.47f},{0.79f,0.47f}
        };
        private static readonly float[,] RuinasBotNorm =
        {
            {0.03f,0.78f},{0.22f,0.80f},{0.42f,0.79f},{0.57f,0.62f},{0.70f,0.52f},{0.79f,0.47f}
        };

        /// <summary>Converte coords normalizadas em pontos de mundo (com espelhamento opcional em Y).</summary>
        private static Vector2[] NormToVec2(float[,] norm, float worldWidth, float worldHeight, bool mirrorY)
        {
            int n = norm.GetLength(0);
            var arr = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float nx = norm[i, 0];
                float ny = mirrorY ? (1f - norm[i, 1]) : norm[i, 1];
                arr[i] = new Vector2((nx - 0.5f) * worldWidth, (0.5f - ny) * worldHeight);
            }
            return arr;
        }

        /// <summary>Lê os pontos (filhos) de um caminho já montado como Vector2 de mundo.</summary>
        private static Vector2[] ParentToVec2(Transform parent)
        {
            var list = new List<Vector2>();
            if (parent != null)
                foreach (Transform c in parent) list.Add(new Vector2(c.position.x, c.position.y));
            return list.ToArray();
        }

        /// <summary>
        /// Cria um objeto-pai com os pontos do caminho (a partir de coordenadas
        /// normalizadas). Se mirrorY, espelha verticalmente (rota de baixo).
        /// </summary>
        private static GameObject BuildPathParent(string name, float[,] norm,
            float worldWidth, float worldHeight, bool mirrorY, out Vector3 first, out Vector3 last)
        {
            int n = norm.GetLength(0);
            var parent = new GameObject(name);
            first = Vector3.zero; last = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                float nx = norm[i, 0];
                float ny = mirrorY ? (1f - norm[i, 1]) : norm[i, 1];
                var pos = new Vector3((nx - 0.5f) * worldWidth, (0.5f - ny) * worldHeight, 0f);
                var p = new GameObject("Point_" + i);
                p.transform.SetParent(parent.transform, false);
                p.transform.position = pos;
                if (i == 0) first = pos;
                if (i == n - 1) last = pos;
            }
            return parent;
        }

        // ============ Caminhos salvos em JSON, POR MAPA (ajustáveis no editor) ============
        private const string LegacyPathsFile = "Assets/MapPaths.json"; // bosque antigo
        private static string PathsFileFor(string id) => $"Assets/MapPaths_{id}.json";

        [System.Serializable] private class PtData { public float x; public float y; }
        [System.Serializable] private class PathData { public List<PtData> points = new List<PtData>(); }
        [System.Serializable] private class MapPathsData { public List<PathData> paths = new List<PathData>(); }

        private static GameObject BuildPathParentFromPoints(string name, List<PtData> pts, out Vector3 first, out Vector3 last)
        {
            var parent = new GameObject(name);
            first = Vector3.zero; last = Vector3.zero;
            for (int i = 0; i < pts.Count; i++)
            {
                var pos = new Vector3(pts[i].x, pts[i].y, 0f);
                var p = new GameObject("Point_" + i);
                p.transform.SetParent(parent.transform, false);
                p.transform.position = pos;
                if (i == 0) first = pos;
                if (i == pts.Count - 1) last = pos;
            }
            return parent;
        }

        private static MapPathsData LoadMapPaths(string id)
        {
            string file = PathsFileFor(id);
            if (!System.IO.File.Exists(file))
            {
                if (id == "bosque" && System.IO.File.Exists(LegacyPathsFile)) file = LegacyPathsFile;
                else return null;
            }
            try
            {
                var data = JsonUtility.FromJson<MapPathsData>(System.IO.File.ReadAllText(file));
                if (data != null && data.paths != null && data.paths.Count > 0) return data;
            }
            catch { /* json inválido: usa o padrão */ }
            return null;
        }

        private static void SavePathsFromParents(IEnumerable<Transform> parents, string id)
        {
            var data = new MapPathsData();
            foreach (var parent in parents)
            {
                if (parent == null) continue;
                var pd = new PathData();
                foreach (Transform c in parent)
                    pd.points.Add(new PtData { x = c.position.x, y = c.position.y });
                data.paths.Add(pd);
            }
            System.IO.File.WriteAllText(PathsFileFor(id), JsonUtility.ToJson(data, true));
            AssetDatabase.Refresh();
        }

        // =====================================================================
        //  HELPERS DE MAPA (fundo, traçados, defs)
        // =====================================================================
        private static string MapBgFile(string id)
        {
            switch (id)
            {
                case "jardim": return "Assets/Art/Maps/mapa_jardim.png";
                case "ruinas": return "Assets/Art/Maps/mapa_ruinas.png";
                default:       return "Assets/Art/Maps/mapa_noturno.png";
            }
        }

        private static Sprite MapBgSprite(string id, float mapH, float worldHeight)
            => EnsureSprite(MapBgFile(id), mapH / worldHeight);

        /// <summary>Trilhas do mapa em coords de mundo: do JSON salvo se existir, senão o padrão.</summary>
        private static List<Vector2[]> MapLanesWorld(string id, float worldWidth, float worldHeight)
        {
            var saved = LoadMapPaths(id);
            if (saved != null && saved.paths.Count > 0)
            {
                var lanes = new List<Vector2[]>();
                foreach (var pd in saved.paths)
                {
                    var arr = new Vector2[pd.points.Count];
                    for (int i = 0; i < arr.Length; i++) arr[i] = new Vector2(pd.points[i].x, pd.points[i].y);
                    if (arr.Length > 0) lanes.Add(arr);
                }
                if (lanes.Count > 0) return lanes;
            }
            switch (id)
            {
                case "jardim":
                    return new List<Vector2[]> { NormToVec2(JardimNorm, worldWidth, worldHeight, false) };
                case "ruinas":
                    return new List<Vector2[]> {
                        NormToVec2(RuinasTopNorm, worldWidth, worldHeight, false),
                        NormToVec2(RuinasMidNorm, worldWidth, worldHeight, false),
                        NormToVec2(RuinasBotNorm, worldWidth, worldHeight, false)
                    };
                default: // bosque: rota base + espelho
                    return new List<Vector2[]> {
                        NormToVec2(BosqueNorm, worldWidth, worldHeight, false),
                        NormToVec2(BosqueNorm, worldWidth, worldHeight, true)
                    };
            }
        }

        private static RuntimeMapBuilder.MapDef MakeMapDef(string id, Sprite bg, List<Vector2[]> lanes)
        {
            return new RuntimeMapBuilder.MapDef
            {
                id = id,
                background = bg,
                pathA = lanes.Count > 0 ? lanes[0] : new Vector2[0],
                pathB = lanes.Count > 1 ? lanes[1] : new Vector2[0],
                pathC = lanes.Count > 2 ? lanes[2] : new Vector2[0]
            };
        }

        /// <summary>Cria um objeto-pai com pontos ARRASTÁVEIS a partir de pontos de mundo.</summary>
        private static GameObject BuildPathParentFromVec(string name, Vector2[] pts)
        {
            var parent = new GameObject(name);
            for (int i = 0; i < pts.Length; i++)
            {
                var p = new GameObject("Point_" + i);
                p.transform.SetParent(parent.transform, false);
                p.transform.position = new Vector3(pts[i].x, pts[i].y, 0f);
            }
            return parent;
        }

        private static void PlaceMarkersForLanes(List<Vector2[]> lanes)
        {
            var spawns = new List<Vector2>();
            var bases = new List<Vector2>();
            foreach (var lane in lanes)
            {
                if (lane.Length == 0) continue;
                MarkDedup(lane[0], Marker.Kind.Spawn, "SpawnPortal", spawns);
                MarkDedup(lane[lane.Length - 1], Marker.Kind.Base, "BaseCrystal", bases);
            }
        }

        private static void MarkDedup(Vector2 pos, Marker.Kind kind, string name, List<Vector2> placed)
        {
            foreach (var p in placed) if (Vector2.Distance(p, pos) < 0.7f) return;
            placed.Add(pos);
            CreateMarker(new Vector3(pos.x, pos.y, 0f), kind, name);
        }

        // =====================================================================
        //  EDITOR DE MAPA: arraste os pontos sobre a arte e salve (igual ao bosque)
        // =====================================================================
        [MenuItem("MeowTactics/Editar Mapa/1. Jardim (1 caminho)", false, 60)]
        public static void EditJardim() => EditMap("jardim");
        [MenuItem("MeowTactics/Editar Mapa/2. Bosque (2 caminhos)", false, 61)]
        public static void EditBosque() => EditMap("bosque");
        [MenuItem("MeowTactics/Editar Mapa/3. Ruinas (3 caminhos)", false, 62)]
        public static void EditRuinas() => EditMap("ruinas");

        private static void EditMap(string id)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float orthoSize = 6f;
            const float worldHeight = orthoSize * 2f;
            const float mapW = 1672f, mapH = 941f;
            float worldWidth = worldHeight * (mapW / mapH);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = orthoSize;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.08f, 0.18f);

            var bg = MapBgSprite(id, mapH, worldHeight);
            if (bg != null)
            {
                var go = new GameObject("Background");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = bg; sr.sortingOrder = -100;
            }

            var lanes = MapLanesWorld(id, worldWidth, worldHeight);
            var parents = new List<Transform>();
            for (int i = 0; i < lanes.Count; i++)
                parents.Add(BuildPathParentFromVec("Path_" + (char)('A' + i), lanes[i]).transform);

            // MapManager só para DESENHAR as linhas dos caminhos (gizmos) no editor.
            var mapGo = new GameObject("MapManager");
            mapGo.AddComponent<MapManager>().pathParents = parents;

            PlaceMarkersForLanes(lanes);

            EditorPrefs.SetString("meow_editing_map", id);
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MapEdit.unity");
            EditorUtility.DisplayDialog("Editar Mapa — " + id,
                "Mapa aberto para edição!\n\n1) Na Hierarquia, abra Path_A / Path_B / Path_C e arraste os Point_* sobre a estrada do fundo.\n2) Depois use: MeowTactics > Salvar Caminhos do Mapa.\n3) Por fim, MeowTactics > Fazer Tudo para aplicar.",
                "Ok");
        }

        [MenuItem("MeowTactics/Salvar Caminhos do Mapa (após arrastar)", false, 63)]
        public static void SaveCurrentPaths()
        {
            string id = EditorPrefs.GetString("meow_editing_map", "bosque");
            var parents = new List<Transform>();
            foreach (var go in Object.FindObjectsOfType<GameObject>())
                if (go.transform.parent == null && go.name.StartsWith("Path_"))
                    parents.Add(go.transform);
            parents.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            if (parents.Count == 0)
            {
                EditorUtility.DisplayDialog("Meow Tactics", "Não achei caminhos (Path_A, Path_B...) na cena aberta.\nAbra um mapa em MeowTactics > Editar Mapa.", "Ok");
                return;
            }
            SavePathsFromParents(parents, id);
            EditorUtility.DisplayDialog("Meow Tactics",
                $"Caminhos do mapa '{id}' salvos ({parents.Count} trilha(s))! 🎉\nAgora rode 'Fazer Tudo' para aplicar.", "Eba!");
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
