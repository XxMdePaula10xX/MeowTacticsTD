using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using MeowTactics.Core;
using MeowTactics.Data;
using MeowTactics.Cats;
using MeowTactics.Managers;

namespace MeowTactics.UI
{
    /// <summary>
    /// Constrói TODA a interface por código e a mantém sincronizada com o jogo.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        // ---- Paleta (noite aconchegante, alto contraste) ----
        // Base índigo-noturno + acento dourado quente. Tons harmonizados para
        // que painel/card/botão tenham separação clara sem brigar entre si.
        private static readonly Color ColDark   = new Color(0.06f, 0.07f, 0.13f, 0.98f); // barras (topo/baixo)
        private static readonly Color ColPanel  = new Color(0.12f, 0.12f, 0.22f, 0.98f); // painéis flutuantes
        private static readonly Color ColCard   = new Color(0.21f, 0.20f, 0.34f, 1f);    // cards/itens
        private static readonly Color ColGold   = new Color(1f, 0.82f, 0.32f);           // acento/títulos
        private static readonly Color ColText   = new Color(0.95f, 0.95f, 0.99f);        // texto principal
        private static readonly Color ColGreen  = new Color(0.30f, 0.78f, 0.46f);        // positivo/comprar
        private static readonly Color ColRed    = new Color(0.93f, 0.39f, 0.42f);        // perigo/vender
        private static readonly Color ColBlue   = new Color(0.36f, 0.64f, 0.95f);        // neutro/ação
        private static readonly Color ColDim    = new Color(0.56f, 0.57f, 0.66f);        // texto secundário

        // Vidro escuro (barras/painéis translúcidos) + linha de borda iluminada.
        private static readonly Color ColGlass     = new Color(0.10f, 0.10f, 0.20f, 0.82f);
        private static readonly Color ColGlassDeep = new Color(0.11f, 0.10f, 0.24f, 0.90f); // loja (roxo/azul)
        private static readonly Color ColBorder    = new Color(0.55f, 0.62f, 0.95f, 0.55f); // borda azulada sutil
        private static readonly Color ColSlot      = new Color(0f, 0f, 0f, 0.30f);          // slot vazio

        // Espaçamentos do design system (8 / 12 / 16 / 24).
        private const int S8 = 8, S12 = 12, S16 = 16, S24 = 24;

        // ---- Sprites de UI (ligados pelo MeowSetup) ----
        public Sprite panelSprite;
        public Sprite buttonSprite;
        public Sprite coinSprite;
        public Sprite heartSprite;
        public Sprite menuBgSprite;

        // ---- Referências de runtime ----
        private Text waveText, waveProgressText, livesText, coinsText, messageText;
        private GameObject waveBarBg;
        private RectTransform waveBarFill;
        private Transform shopContainer, benchContainer, synergyContainer, itemsContainer;
        private Text benchLabel, startLabel;
        private GameObject startPlayIcon;
        private Button startWaveButton, rerollButton;
        private Button pauseBtn, speed1Btn, speed2Btn;
        private UIPulse startPulse;

        private GameObject itemsPanel, bottomPanel;
        private GameObject nextWavePanel;
        private Transform threatContainer;
        private Button collapseBtn;
        private bool collapsed;
        private RectTransform synergyPanelRt;
        private Button synergyToggleBtn;
        private bool synergyCollapsed;

        private readonly HashSet<SynergyType> prevActiveSynergies = new HashSet<SynergyType>();
        private readonly HashSet<SynergyType> everActivated = new HashSet<SynergyType>();

        private GameObject waveBanner, tooltipPanel;
        private Text waveBannerText, tooltipText;
        private Coroutine waveBannerRoutine;

        private GameObject detailPanel, detailContent;
        private Button detailSellBtn, detailReturnBtn;
        private CatUnit detailCat;

        private GameObject draftPanel;
        private Transform draftContainer;

        private GameObject endPanel;
        private Text endText;

        private GameObject mainMenuPanel, pausePanel, settingsPanel;
        private float musicVolume = 0.6f;
        private bool resetArmed;
        private Button resetBtn;

        private GameObject tutorialPanel, mapSelectPanel;
        private GameObject collectionPanel;
        private GameObject achievementsPanel;
        private Text tutorialText;
        private int tutorialStep;
        private static readonly string[] TutorialSteps =
        {
            "1/7 — Compre um gato na LOJA (embaixo). Ele vai para o BANCO.",
            "2/7 — Clique num gato do BANCO e toque no gramado para posicioná-lo.",
            "3/7 — Clique em INICIAR ONDA (canto direito) para começar a luta.",
            "4/7 — Derrote inimigos para ganhar moedas e comprar mais gatos.",
            "5/7 — TIPOS DE DANO importam: Físico sofre com armadura, Mágico com resist. mágica, e Verdadeiro (Samurai/Monge) ignora as duas. Veja a fraqueza de cada inimigo na COLEÇÃO.",
            "6/7 — Junte gatos do mesmo tipo para ativar SINERGIAS (painel à direita).",
            "7/7 — A cada 3 ondas você escolhe um ITEM — equipe-o tocando num gato!"
        };

        private Coroutine messageRoutine;
        private Coroutine tooltipHideRoutine;
        private int gameSpeed = 1;

        private void Awake()
        {
            Instance = this;
            // 60 FPS estável em mobile (evita oscilação e poupa bateria).
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            BuildUI();
            Subscribe();
            RefreshAll();
            if (!GameManager.StartInGame) { ShowMainMenu(); MusicManager.Play(MusicTrack.Menu); }
            else
            {
                MusicManager.Play(MusicTrack.Game);
                if (PlayerPrefs.GetInt("tutorialDone", 0) == 0) ShowTutorial();
            }
        }

        // =========================================================
        //  CONSTRUÇÃO
        // =========================================================
        private void BuildUI()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("MeowCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasGo.transform;

            // Container que respeita a Safe Area do iPhone (notch/Dynamic Island/home).
            // A HUD de jogo (barras, painéis, botões nas bordas) vai AQUI dentro.
            // Os overlays de tela cheia (menu, pausa, etc.) ficam no root, full-bleed.
            var safeGo = new GameObject("SafeArea", typeof(RectTransform));
            safeGo.transform.SetParent(root, false);
            var safeRt = safeGo.GetComponent<RectTransform>();
            safeRt.anchorMin = Vector2.zero; safeRt.anchorMax = Vector2.one;
            safeRt.offsetMin = Vector2.zero; safeRt.offsetMax = Vector2.zero;
            safeGo.AddComponent<SafeArea>();
            Transform safe = safeGo.transform;

            BuildTopBar(safe);
            BuildItemsPanel(safe);
            BuildSynergyPanel(safe);
            BuildBottomArea(safe);
            BuildDetailPanel(root);
            BuildDraftPanel(root);
            BuildEndPanel(root);
            BuildMessage(safe);
            BuildWaveBanner(safe);
            BuildTooltip(root);
            BuildMainMenu(root);
            BuildPauseMenu(root);
            BuildSettings(root);
            BuildMapSelect(root);
            BuildCollection(root);
            BuildAchievements(root);
            BuildTutorial(root);
        }

        // =========================================================
        //  SELEÇÃO DE MAPA
        // =========================================================
        private void BuildMapSelect(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "MapSelect", new Color(0.07f, 0.06f, 0.14f, 1f), null, false);
            mapSelectPanel = panel.gameObject;
            UIFactory.StretchFull(panel.rectTransform);
            Appear(mapSelectPanel, 1f);

            var title = UIFactory.CreateText(panel.transform, "Title", "ESCOLHER MAPA", 54, ColGold, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold; NoWrap(title); AddOutline(title);
            var trt = title.rectTransform;
            UIFactory.SetAnchors(trt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            trt.sizeDelta = new Vector2(900, 80); trt.anchoredPosition = new Vector2(0, -110);

            var row = new GameObject("Cards", typeof(RectTransform));
            row.transform.SetParent(panel.transform, false);
            var rrt = row.GetComponent<RectTransform>();
            UIFactory.SetAnchors(rrt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rrt.sizeDelta = new Vector2(1140, 460);
            rrt.anchoredPosition = new Vector2(0, 10);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 28; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            MapCard(row.transform, "jardim", "Jardim Místico", "Fácil", "1 caminho em grande arco — ideal pra começar.", true);
            MapCard(row.transform, "bosque", "Bosque Fantasma", "Médio", "2 caminhos espelhados na floresta noturna.", true);
            MapCard(row.transform, "ruinas", "Ruínas Lunares", "Difícil", "3 trilhas que convergem para o cristal.", true);

            var back = UIFactory.CreateButton(panel.transform, "Back", "Voltar", ColGreen, HideMapSelect, 26, buttonSprite);
            back.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            var brt = UIFactory.AsRect(back);
            UIFactory.SetAnchors(brt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            brt.sizeDelta = new Vector2(260, 64); brt.anchoredPosition = new Vector2(0, 110);

            mapSelectPanel.SetActive(false);
        }

        private void MapCard(Transform parent, string mapId, string name, string difficulty, string desc, bool playable)
        {
            var card = UIFactory.CreatePanel(parent, "MapCard", new Color(0.16f, 0.13f, 0.27f, 0.98f));
            AddShadow(card, 5f, 0.35f);
            var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 18, 18); vlg.spacing = 8;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            var nm = UIFactory.CreateText(card.transform, "Name", name, 26, playable ? ColGold : ColDim, TextAnchor.MiddleCenter);
            nm.fontStyle = FontStyle.Bold; NoWrap(nm); AddMinHeight(nm, 34);
            var diff = UIFactory.CreateText(card.transform, "Diff", "Dificuldade: " + difficulty, 16, ColText, TextAnchor.MiddleCenter);
            AddMinHeight(diff, 24);
            var ds = UIFactory.CreateText(card.transform, "Desc", desc, 15, ColDim, TextAnchor.UpperCenter);
            var dle = ds.gameObject.AddComponent<LayoutElement>(); dle.minHeight = 120; dle.flexibleHeight = 1;
            var rec = UIFactory.CreateText(card.transform, "Rec", $"Recorde: onda {SaveSystem.BestWave(mapId)}", 15, ColGold, TextAnchor.MiddleCenter);
            AddMinHeight(rec, 24);

            if (playable)
            {
                var play = UIFactory.CreateButton(card.transform, "Play", "Jogar", ColGreen,
                    () => { SaveSystem.CurrentMapId = mapId; GameManager.Instance?.NewGame(); }, 22, buttonSprite);
                play.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
                var le = play.gameObject.AddComponent<LayoutElement>(); le.minHeight = 54;
            }
            else
            {
                var locked = UIFactory.CreateText(card.transform, "Locked", "Em breve", 18, ColDim, TextAnchor.MiddleCenter);
                AddMinHeight(locked, 54);
            }
        }

        public void ShowMapSelect() { if (mapSelectPanel != null) mapSelectPanel.SetActive(true); SFXManager.Play(SfxType.Click); }
        public void HideMapSelect() { if (mapSelectPanel != null) mapSelectPanel.SetActive(false); SFXManager.Play(SfxType.Click); }

        private void ContinueGame()
        {
            GameManager.Instance?.ContinueSavedGame();
        }

        // =========================================================
        //  COLEÇÃO (gatos, inimigos e sinergias)
        // =========================================================
        private void BuildCollection(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "Collection", new Color(0.07f, 0.06f, 0.14f, 1f), null, false);
            collectionPanel = panel.gameObject;
            UIFactory.StretchFull(panel.rectTransform);
            Appear(collectionPanel, 1f);

            var title = UIFactory.CreateText(panel.transform, "Title", "COLEÇÃO", 54, ColGold, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold; NoWrap(title); AddOutline(title);
            var trt = title.rectTransform;
            UIFactory.SetAnchors(trt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            trt.sizeDelta = new Vector2(900, 80); trt.anchoredPosition = new Vector2(0, -60);

            // Área rolável (entre o título e o botão Voltar)
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrt = scrollGo.GetComponent<RectTransform>();
            UIFactory.SetAnchors(scrt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            scrt.sizeDelta = new Vector2(1520, 760);
            scrt.anchoredPosition = new Vector2(0, -20);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            var vprt = viewport.GetComponent<RectTransform>();
            UIFactory.StretchFull(vprt);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.spacing = S12; vlg.padding = new RectOffset(6, S16, 6, S16);
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = vprt; scroll.content = crt;

            // --- Conteúdo ---
            CollectionHeader(content.transform, "GATOS");
            if (ShopManager.Instance != null)
            {
                var cats = new List<CatData>(ShopManager.Instance.availableCats);
                cats.RemoveAll(c => c == null);
                cats.Sort((a, b) => a.cost.CompareTo(b.cost));
                foreach (var c in cats) CatCodexRow(content.transform, c);
            }

            CollectionHeader(content.transform, "INIMIGOS");
            foreach (var e in AllEnemies()) EnemyCodexRow(content.transform, e);

            CollectionHeader(content.transform, "SINERGIAS");
            if (SynergyManager.Instance != null)
                foreach (var s in SynergyManager.Instance.allSynergies) if (s != null) SynergyCodexRow(content.transform, s);

            // Botão Voltar
            var back = UIFactory.CreateButton(panel.transform, "Back", "Voltar", ColGreen, HideCollection, 26, buttonSprite);
            back.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            AddShadow(back.image, 4f, 0.35f);
            var brt = UIFactory.AsRect(back);
            UIFactory.SetAnchors(brt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            brt.sizeDelta = new Vector2(260, 64); brt.anchoredPosition = new Vector2(0, 110);

            collectionPanel.SetActive(false);
        }

        private void CollectionHeader(Transform parent, string text)
        {
            var h = UIFactory.CreatePanel(parent, "Hdr", new Color(ColGold.r, ColGold.g, ColGold.b, 0.14f));
            var le = h.gameObject.AddComponent<LayoutElement>(); le.minHeight = 46; le.preferredHeight = 46;
            var t = UIFactory.CreateText(h.transform, "T", text, 28, ColGold, TextAnchor.MiddleLeft);
            t.fontStyle = FontStyle.Bold; NoWrap(t);
            var trt = t.rectTransform; UIFactory.StretchFull(trt, 0f);
            trt.offsetMin = new Vector2(16, 0);
        }

        private void CatCodexRow(Transform parent, CatData c)
        {
            var row = UIFactory.CreatePanel(parent, "CatRow", ColCard);
            AddShadow(row, 3f, 0.25f);
            var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = 162; le.preferredHeight = 162;
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 12, 12); hlg.spacing = S16;
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            CodexIcon(row.transform, c.icon, c.placeholderColor, 120);

            var colGo = NewColumn(row.transform);
            var nm = UIFactory.CreateText(colGo, "Name", c.catName, 24, ColGold, TextAnchor.UpperLeft);
            nm.fontStyle = FontStyle.Bold; NoWrap(nm); AddMinHeight(nm, 30);

            string mech = "";
            if (c.areaDamage) mech += "   •   Área";
            if (c.appliesSlow) mech += "   •   Lentidão";
            var stats = UIFactory.CreateText(colGo, "Stats",
                $"<color=#{ToHex(DamageColor(c.damageType))}><b>{DamageName(c.damageType)}</b></color>   •   Dano {c.baseDamage:0}   •   Vel {c.attackSpeed:0.##}/s   •   Alcance {c.range:0.#}   •   Crít {c.critChance:0}%{mech}",
                15, ColText, TextAnchor.UpperLeft);
            NoWrap(stats); AddMinHeight(stats, 22);

            var desc = UIFactory.CreateText(colGo, "Desc", c.description, 14, ColDim, TextAnchor.UpperLeft);
            var dle = desc.gameObject.AddComponent<LayoutElement>(); dle.minHeight = 36; dle.flexibleHeight = 1;

            var chips = new GameObject("Chips", typeof(RectTransform));
            chips.transform.SetParent(colGo, false);
            var chl = chips.AddComponent<HorizontalLayoutGroup>();
            chl.childAlignment = TextAnchor.MiddleLeft; chl.spacing = 6;
            chl.childForceExpandWidth = false; chl.childForceExpandHeight = false;
            var chle = chips.AddComponent<LayoutElement>(); chle.minHeight = 26; chle.preferredHeight = 26;
            foreach (var t in c.synergies) SynergyChip(chips.transform, t);
        }

        private void EnemyCodexRow(Transform parent, EnemyData e)
        {
            var row = UIFactory.CreatePanel(parent, "EnemyRow", ColCard);
            AddShadow(row, 3f, 0.25f);
            var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = 150; le.preferredHeight = 150;
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 12, 12); hlg.spacing = S16;
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            CodexIcon(row.transform, e.icon, e.placeholderColor, 108);

            var colGo = NewColumn(row.transform);
            var nm = UIFactory.CreateText(colGo, "Name", e.enemyName + (e.isBoss ? "  ★ BOSS" : ""), 24,
                e.isBoss ? ColGold : ColText, TextAnchor.UpperLeft);
            nm.fontStyle = FontStyle.Bold; NoWrap(nm); AddMinHeight(nm, 30);

            var stats = UIFactory.CreateText(colGo, "Stats",
                $"Vida {e.maxHealth:0}   •   <color=#bfc6d0>Armadura {e.armor:0}</color>   •   <color=#b89cff>Resist. Mág {e.magicResistance:0}</color>   •   Vel {e.moveSpeed:0.##}",
                15, ColText, TextAnchor.UpperLeft);
            NoWrap(stats); AddMinHeight(stats, 22);

            Color rc = ThreatColor(e);
            var hint = UIFactory.CreateText(colGo, "Hint", ResistText(e), 14, rc, TextAnchor.UpperLeft);
            NoWrap(hint); AddMinHeight(hint, 22);

            var desc = UIFactory.CreateText(colGo, "Desc", e.description, 14, ColDim, TextAnchor.UpperLeft);
            var dle = desc.gameObject.AddComponent<LayoutElement>(); dle.minHeight = 30; dle.flexibleHeight = 1;
        }

        private void SynergyCodexRow(Transform parent, SynergyData s)
        {
            var row = UIFactory.CreatePanel(parent, "SynRow2", ColCard);
            AddShadow(row, 3f, 0.25f);
            var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = 134; le.preferredHeight = 134;
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 12, 12); hlg.spacing = S16;
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            // Badge colorido com a inicial
            var badge = UIFactory.CreatePanel(row.transform, "Badge", s.uiColor);
            Fixed(badge, 64, 64);
            var bt = UIFactory.CreateText(badge.transform, "B",
                string.IsNullOrEmpty(s.displayName) ? "?" : s.displayName.Substring(0, 1).ToUpper(), 32, Color.white, TextAnchor.MiddleCenter);
            bt.fontStyle = FontStyle.Bold; AddOutline(bt); UIFactory.StretchFull(bt.rectTransform);

            var colGo = NewColumn(row.transform);
            var nm = UIFactory.CreateText(colGo, "Name", DisplayName(s), 24, Color.Lerp(s.uiColor, Color.white, 0.4f), TextAnchor.UpperLeft);
            nm.fontStyle = FontStyle.Bold; NoWrap(nm); AddMinHeight(nm, 30);

            var dsc = UIFactory.CreateText(colGo, "Desc", s.description, 14, ColDim, TextAnchor.UpperLeft);
            NoWrap(dsc); AddMinHeight(dsc, 20);

            foreach (var tier in s.tiers)
            {
                var t = UIFactory.CreateText(colGo, "Tier",
                    $"<b>{tier.requiredCount}</b>:  {tier.description}", 15, ColText, TextAnchor.UpperLeft);
                NoWrap(t); AddMinHeight(t, 22);
            }
        }

        // Coluna vertical flexível usada nas linhas da coleção.
        private Transform NewColumn(Transform parent)
        {
            var colGo = new GameObject("Col", typeof(RectTransform));
            colGo.transform.SetParent(parent, false);
            var cle = colGo.AddComponent<LayoutElement>(); cle.flexibleWidth = 1;
            var cvl = colGo.AddComponent<VerticalLayoutGroup>();
            cvl.childAlignment = TextAnchor.UpperLeft; cvl.spacing = 3;
            cvl.childForceExpandWidth = true; cvl.childForceExpandHeight = false;
            return colGo.transform;
        }

        // Ícone (arte) ou amostra de cor quando não há arte.
        private void CodexIcon(Transform parent, Sprite icon, Color fallback, float size)
        {
            if (icon != null)
            {
                var ic = UIFactory.CreateIcon(parent, "Icon", icon, size);
                Fixed(ic, size, size);
            }
            else
            {
                var sw = UIFactory.CreatePanel(parent, "Swatch", fallback);
                Fixed(sw, size, size);
            }
        }

        private List<EnemyData> AllEnemies()
        {
            var list = new List<EnemyData>();
            var seen = new HashSet<EnemyData>();
            if (WaveManager.Instance != null)
                foreach (var w in WaveManager.Instance.waves)
                    if (w != null)
                        foreach (var info in w.enemies)
                            if (info.enemy != null && seen.Add(info.enemy)) list.Add(info.enemy);
            return list;
        }

        private static string ResistText(EnemyData e)
        {
            if (e.armor >= 30f && e.magicResistance >= 30f) return "Resiste a físico E mágico — use DANO VERDADEIRO (Samurai/Monge)";
            if (e.armor >= 30f) return "Resistente a físico — use penetração de armadura ou dano verdadeiro";
            if (e.magicResistance >= 30f) return "Resistente a mágico — use penetração mágica ou dano físico";
            if (e.moveSpeed >= 1.5f) return "Muito rápido — priorize alcance e lentidão";
            return "Sem resistências especiais";
        }

        private static string ToHex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        public void ShowCollection() { if (collectionPanel != null) collectionPanel.SetActive(true); SFXManager.Play(SfxType.Click); }
        public void HideCollection() { if (collectionPanel != null) collectionPanel.SetActive(false); SFXManager.Play(SfxType.Click); }

        // =========================================================
        //  CONQUISTAS
        // =========================================================
        private Text achievementsCountText;

        private void BuildAchievements(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "Achievements", new Color(0.07f, 0.06f, 0.14f, 1f), null, false);
            achievementsPanel = panel.gameObject;
            UIFactory.StretchFull(panel.rectTransform);
            Appear(achievementsPanel, 1f);

            var title = UIFactory.CreateText(panel.transform, "Title", "CONQUISTAS", 54, ColGold, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold; NoWrap(title); AddOutline(title);
            var trt = title.rectTransform;
            UIFactory.SetAnchors(trt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            trt.sizeDelta = new Vector2(900, 80); trt.anchoredPosition = new Vector2(0, -52);

            achievementsCountText = UIFactory.CreateText(panel.transform, "Count", "", 22, ColText, TextAnchor.MiddleCenter);
            achievementsCountText.fontStyle = FontStyle.Bold; NoWrap(achievementsCountText);
            var ctrt = achievementsCountText.rectTransform;
            UIFactory.SetAnchors(ctrt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            ctrt.sizeDelta = new Vector2(600, 30); ctrt.anchoredPosition = new Vector2(0, -104);

            // Área rolável
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrt = scrollGo.GetComponent<RectTransform>();
            UIFactory.SetAnchors(scrt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            scrt.sizeDelta = new Vector2(1320, 720);
            scrt.anchoredPosition = new Vector2(0, -36);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            UIFactory.StretchFull(viewport.GetComponent<RectTransform>());

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.spacing = S8; vlg.padding = new RectOffset(6, S16, 6, S16);
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = crt;

            if (AchievementManager.Instance != null)
                foreach (var a in AchievementManager.Instance.All) AchievementRow(content.transform, a);

            var back = UIFactory.CreateButton(panel.transform, "Back", "Voltar", ColGreen, HideAchievements, 26, buttonSprite);
            back.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            AddShadow(back.image, 4f, 0.35f);
            var brt = UIFactory.AsRect(back);
            UIFactory.SetAnchors(brt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            brt.sizeDelta = new Vector2(260, 64); brt.anchoredPosition = new Vector2(0, 110);

            achievementsPanel.SetActive(false);
        }

        private void AchievementRow(Transform parent, MeowTactics.Managers.AchievementDef a)
        {
            var mgr = AchievementManager.Instance;
            bool done = mgr != null && mgr.IsUnlocked(a);
            int prog = mgr != null ? mgr.Progress(a) : 0;

            // Moldura dourada quando desbloqueada.
            Color border = done ? ColGold : new Color(0.30f, 0.30f, 0.40f, 1f);
            var row = UIFactory.CreatePanel(parent, "AchRow", border);
            AddShadow(row, 3f, 0.25f);
            var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = 96; le.preferredHeight = 96;

            var inner = UIFactory.CreatePanel(row.transform, "Fill",
                done ? new Color(0.20f, 0.17f, 0.10f, 1f) : new Color(0.15f, 0.14f, 0.22f, 1f));
            UIFactory.StretchFull(inner.rectTransform, 3f); inner.raycastTarget = false;
            var hlg = inner.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 16, 10, 10); hlg.spacing = S16;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            // Badge (troféu/cadeado)
            var badge = UIFactory.CreatePanel(inner.transform, "Badge",
                done ? ColGold : new Color(0.10f, 0.10f, 0.16f, 1f));
            Fixed(badge, 64, 64);
            var bt = UIFactory.CreateText(badge.transform, "B", done ? "★" : "?", 34,
                done ? new Color(0.2f, 0.15f, 0f) : ColDim, TextAnchor.MiddleCenter);
            bt.fontStyle = FontStyle.Bold;
            UIFactory.StretchFull(bt.rectTransform);

            // Coluna: título + descrição + barra de progresso
            var colGo = new GameObject("Col", typeof(RectTransform));
            colGo.transform.SetParent(inner.transform, false);
            var cle = colGo.AddComponent<LayoutElement>(); cle.flexibleWidth = 1;
            var cvl = colGo.AddComponent<VerticalLayoutGroup>();
            cvl.childAlignment = TextAnchor.MiddleLeft; cvl.spacing = 3;
            cvl.childForceExpandWidth = true; cvl.childForceExpandHeight = false;

            var nm = UIFactory.CreateText(colGo.transform, "Name", a.title, 22,
                done ? ColGold : ColText, TextAnchor.UpperLeft);
            nm.fontStyle = FontStyle.Bold; NoWrap(nm); AddMinHeight(nm, 28);
            var ds = UIFactory.CreateText(colGo.transform, "Desc", a.desc, 14, ColDim, TextAnchor.UpperLeft);
            NoWrap(ds); AddMinHeight(ds, 18);

            // Barra de progresso
            ProgressBar(colGo.transform, prog, a.target, done);
        }

        /// <summary>Barra de progresso com texto "x/y" (verde quando completa).</summary>
        private void ProgressBar(Transform parent, int value, int target, bool done)
        {
            var bar = UIFactory.CreatePanel(parent, "Bar", new Color(0f, 0f, 0f, 0.45f));
            var le = bar.gameObject.AddComponent<LayoutElement>(); le.minHeight = 22; le.preferredHeight = 22;

            float ratio = target > 0 ? Mathf.Clamp01((float)value / target) : 0f;
            var fill = UIFactory.CreatePanel(bar.transform, "Fill", done ? ColGreen : ColBlue);
            fill.raycastTarget = false;
            var frt = fill.rectTransform;
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(ratio, 1);
            frt.pivot = new Vector2(0, 0.5f); frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;

            var txt = UIFactory.CreateText(bar.transform, "Txt",
                done ? "Concluída!" : $"{value} / {target}", 13, Color.white, TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold; AddOutline(txt);
            UIFactory.StretchFull(txt.rectTransform);
        }

        public void ShowAchievements()
        {
            if (achievementsPanel == null) return;
            RefreshAchievements();
            achievementsPanel.SetActive(true);
            SFXManager.Play(SfxType.Click);
        }

        public void HideAchievements() { if (achievementsPanel != null) achievementsPanel.SetActive(false); SFXManager.Play(SfxType.Click); }

        // Reconstrói as linhas (progresso pode ter mudado desde a última abertura).
        private void RefreshAchievements()
        {
            if (achievementsPanel == null || AchievementManager.Instance == null) return;
            var content = achievementsPanel.transform.Find("Scroll/Viewport/Content");
            if (content != null)
            {
                ClearDynamic(content, "AchRow");
                foreach (var a in AchievementManager.Instance.All) AchievementRow(content, a);
            }
            if (achievementsCountText != null)
                achievementsCountText.text =
                    $"{AchievementManager.Instance.UnlockedCount()} / {AchievementManager.Instance.All.Count} desbloqueadas";
        }

        // =========================================================
        //  TUTORIAL (passos guiados, pulável)
        // =========================================================
        private void BuildTutorial(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "Tutorial", new Color(0.09f, 0.07f, 0.17f, 0.97f));
            tutorialPanel = panel.gameObject;
            Appear(tutorialPanel);
            var rt = panel.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            rt.sizeDelta = new Vector2(880, 84);
            rt.anchoredPosition = new Vector2(0, -150);

            tutorialText = UIFactory.CreateText(panel.transform, "Text", "", 20, ColText, TextAnchor.MiddleLeft);
            tutorialText.fontStyle = FontStyle.Bold;
            var txrt = tutorialText.rectTransform;
            UIFactory.SetAnchors(txrt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f));
            txrt.offsetMin = new Vector2(22, 6); txrt.offsetMax = new Vector2(-250, -6);

            var nextBtn = UIFactory.CreateButton(panel.transform, "Next", "Próximo", ColGreen, NextTutorial, 18, buttonSprite);
            nextBtn.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            var nrt = UIFactory.AsRect(nextBtn);
            UIFactory.SetAnchors(nrt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
            nrt.sizeDelta = new Vector2(120, 58); nrt.anchoredPosition = new Vector2(-128, 0);

            var skipBtn = UIFactory.CreateButton(panel.transform, "Skip", "Pular", ColCard, CloseTutorial, 18, buttonSprite);
            var srt = UIFactory.AsRect(skipBtn);
            UIFactory.SetAnchors(srt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
            srt.sizeDelta = new Vector2(104, 58); srt.anchoredPosition = new Vector2(-12, 0);

            tutorialPanel.SetActive(false);
        }

        public void ShowTutorial()
        {
            if (tutorialPanel == null) return;
            tutorialStep = 0;
            tutorialText.text = TutorialSteps[0];
            tutorialPanel.SetActive(true);
        }

        private void NextTutorial()
        {
            tutorialStep++;
            if (tutorialStep >= TutorialSteps.Length) { CloseTutorial(); return; }
            tutorialText.text = TutorialSteps[tutorialStep];
            SFXManager.Play(SfxType.Click);
        }

        private void CloseTutorial()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            PlayerPrefs.SetInt("tutorialDone", 1);
            SFXManager.Play(SfxType.Click);
        }

        // =========================================================
        //  MENU PRINCIPAL / PAUSE / CONFIGURAÇÕES (overlays)
        // =========================================================
        private void BuildMainMenu(Transform root)
        {
            Image panel;
            if (menuBgSprite != null)
            {
                // Arte de fundo personalizada + leve escurecimento para legibilidade.
                panel = UIFactory.CreatePanel(root, "MainMenu", Color.white, menuBgSprite);
                var overlay = UIFactory.CreatePanel(panel.transform, "Overlay", new Color(0f, 0f, 0f, 0.42f), null, false);
                UIFactory.StretchFull(overlay.rectTransform);
            }
            else
            {
                panel = UIFactory.CreatePanel(root, "MainMenu", new Color(0.07f, 0.06f, 0.14f, 1f), null, false);
            }
            mainMenuPanel = panel.gameObject;
            UIFactory.StretchFull(panel.rectTransform);
            Appear(mainMenuPanel, 1f);

            var title = UIFactory.CreateText(panel.transform, "Title", "MEOW TACTICS TD", 72, ColGold, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold; NoWrap(title); AddOutline(title);
            var trt = title.rectTransform;
            UIFactory.SetAnchors(trt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            trt.sizeDelta = new Vector2(1200, 100); trt.anchoredPosition = new Vector2(0, -170);

            var col = MenuColumn(panel.transform, 540);
            MenuButton(col, "Novo Jogo", ColGreen, ShowMapSelect);
            bool hasSave = SaveSystem.HasSave();
            var cont = MenuButton(col, "Continuar", hasSave ? ColGreen : ColCard, ContinueGame);
            cont.interactable = hasSave;
            MenuButton(col, "Coleção", ColBlue, ShowCollection);
            MenuButton(col, "Conquistas", ColBlue, ShowAchievements);
            MenuButton(col, "Configurações", ColBlue, ShowSettings);
            // A Apple não permite/recomenda botão de "sair" no iOS (o sistema gerencia isso).
#if UNITY_STANDALONE || UNITY_EDITOR
            MenuButton(col, "Sair", ColRed, () => Application.Quit());
#endif

            mainMenuPanel.SetActive(false);
        }

        private void BuildPauseMenu(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "PauseMenu", new Color(0f, 0f, 0f, 0.84f), null, false);
            pausePanel = panel.gameObject;
            UIFactory.StretchFull(panel.rectTransform);
            Appear(pausePanel, 1f);

            var title = UIFactory.CreateText(panel.transform, "Title", "PAUSADO", 60, ColGold, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold; NoWrap(title); AddOutline(title);
            var trt = title.rectTransform;
            UIFactory.SetAnchors(trt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            trt.sizeDelta = new Vector2(800, 90); trt.anchoredPosition = new Vector2(0, -180);

            var col = MenuColumn(panel.transform, 440);
            MenuButton(col, "Continuar", ColGreen, HidePause);
            MenuButton(col, "Reiniciar", ColBlue, () => GameManager.Instance?.NewGame());
            MenuButton(col, "Voltar ao Menu", ColCard, () => GameManager.Instance?.GoToMenu());
            MenuButton(col, "Configurações", ColBlue, ShowSettings);

            pausePanel.SetActive(false);
        }

        private void BuildSettings(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "Settings", new Color(0.07f, 0.06f, 0.14f, 1f), null, false);
            settingsPanel = panel.gameObject;
            UIFactory.StretchFull(panel.rectTransform);
            Appear(settingsPanel, 1f);

            var title = UIFactory.CreateText(panel.transform, "Title", "CONFIGURAÇÕES", 54, ColGold, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold; NoWrap(title); AddOutline(title);
            var trt = title.rectTransform;
            UIFactory.SetAnchors(trt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            trt.sizeDelta = new Vector2(900, 80); trt.anchoredPosition = new Vector2(0, -170);

            var col = MenuColumn(panel.transform, 560);

            float music = MusicManager.Instance != null ? MusicManager.Instance.volume : musicVolume;
            SliderRow(col, "Volume da Música", music,
                v => { if (MusicManager.Instance != null) MusicManager.Instance.SetVolume(v); });
            float sfx = SFXManager.Instance != null ? SFXManager.Instance.volume : 0.55f;
            SliderRow(col, "Volume dos Efeitos", sfx, v => { if (SFXManager.Instance != null) SFXManager.Instance.volume = v; });

            // Reset com confirmação em 2 toques (evita apagar tudo por engano).
            resetArmed = false;
            resetBtn = MenuButton(col, "Resetar Progresso", ColRed, () =>
            {
                var lbl = resetBtn.GetComponentInChildren<Text>();
                if (!resetArmed)
                {
                    resetArmed = true;
                    if (lbl != null) lbl.text = "Confirmar reset? (toque de novo)";
                }
                else
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    resetArmed = false;
                    if (lbl != null) lbl.text = "Resetar Progresso";
                    ShowMessage("Progresso resetado.");
                }
            });
            MenuButton(col, "Voltar", ColGreen, HideSettings);

            settingsPanel.SetActive(false);
        }

        private Transform MenuColumn(Transform parent, float height)
        {
            var col = new GameObject("Buttons", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            var rt = col.GetComponent<RectTransform>();
            UIFactory.SetAnchors(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(440, height);
            rt.anchoredPosition = new Vector2(0, -40);
            var vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16; vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            return col.transform;
        }

        private Button MenuButton(Transform parent, string label, Color color, UnityAction onClick)
        {
            // Botão sólido (sem sprite): preenche melhor que o 9-slice em botões largos/baixos.
            var b = UIFactory.CreateButton(parent, "MenuBtn", label, color, onClick, 28);
            b.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            AddOutline(b.GetComponentInChildren<Text>());
            AddShadow(b.image, 4f, 0.35f);
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 70; le.preferredHeight = 70;
            return b;
        }

        private void SliderRow(Transform parent, string label, float value, UnityAction<float> onChanged)
        {
            var lbl = UIFactory.CreateText(parent, "SLabel", label, 24, ColText, TextAnchor.MiddleCenter);
            lbl.fontStyle = FontStyle.Bold; NoWrap(lbl);
            var lle = lbl.gameObject.AddComponent<LayoutElement>(); lle.minHeight = 34;

            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>(); le.minHeight = 34; le.preferredHeight = 34;
            var slider = go.GetComponent<Slider>();

            var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgi = bg.GetComponent<Image>(); bgi.color = new Color(0f, 0f, 0f, 0.5f);
            UIFactory.StretchFull(bg.GetComponent<RectTransform>());

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(go.transform, false);
            fill.GetComponent<Image>().color = ColGold;
            var frt = fill.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;

            slider.fillRect = frt;
            slider.targetGraphic = bgi;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = value;
            if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
        }

        public void ShowMainMenu() { if (mainMenuPanel != null) mainMenuPanel.SetActive(true); }
        public void ShowSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
            // Reseta o "armar" do reset ao reabrir as configurações.
            resetArmed = false;
            if (resetBtn != null)
            {
                var lbl = resetBtn.GetComponentInChildren<Text>();
                if (lbl != null) lbl.text = "Resetar Progresso";
            }
            SFXManager.Play(SfxType.Click);
        }
        public void HideSettings() { if (settingsPanel != null) settingsPanel.SetActive(false); SFXManager.Play(SfxType.Click); }

        public void OpenPause()
        {
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
            SFXManager.Play(SfxType.Click);
        }

        public void HidePause()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            HideSettings();
            Time.timeScale = (GameManager.Instance != null && GameManager.Instance.State == GameState.WaveInProgress) ? gameSpeed : 1f;
            SFXManager.Play(SfxType.Click);
        }

        // Banner de fim de onda (centro-superior, some sozinho)
        private void BuildWaveBanner(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "WaveBanner", new Color(0.10f, 0.08f, 0.18f, 0.95f), panelSprite);
            AddShadow(panel);
            waveBanner = panel.gameObject;
            var rt = panel.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            rt.sizeDelta = new Vector2(560, 150);
            rt.anchoredPosition = new Vector2(0, -230);

            waveBannerText = UIFactory.CreateText(panel.transform, "Text", "", 26, ColGold, TextAnchor.MiddleCenter);
            waveBannerText.fontStyle = FontStyle.Bold;
            AddOutline(waveBannerText);
            UIFactory.StretchFull(waveBannerText.rectTransform, 16);
            waveBanner.SetActive(false);
        }

        // Tooltip de sinergia (segue o cursor)
        private void BuildTooltip(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "Tooltip", new Color(0.05f, 0.04f, 0.10f, 0.97f), panelSprite);
            AddShadow(panel);
            panel.raycastTarget = false; // não rouba o cursor (evita flicker)
            tooltipPanel = panel.gameObject;
            var rt = panel.rectTransform;
            rt.pivot = new Vector2(1f, 0f); // aparece acima/à esquerda do cursor
            rt.sizeDelta = new Vector2(330, 150);

            tooltipText = UIFactory.CreateText(panel.transform, "Text", "", 18, ColText, TextAnchor.UpperLeft);
            UIFactory.StretchFull(tooltipText.rectTransform, 14);
            tooltipPanel.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // ---------- Top bar ----------
        private void BuildTopBar(Transform root)
        {
            var bar = UIFactory.CreatePanel(root, "TopBar", ColGlass, null, false);
            var rt = bar.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            rt.sizeDelta = new Vector2(0, 100);
            rt.anchoredPosition = Vector2.zero;

            // Linha de borda inferior sutil (estilo vidro).
            var border = new GameObject("BottomBorder", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(bar.transform, false);
            var bimg = border.GetComponent<Image>();
            bimg.color = new Color(ColBorder.r, ColBorder.g, ColBorder.b, 0.35f); bimg.raycastTarget = false;
            var bordRt = border.GetComponent<RectTransform>();
            UIFactory.SetAnchors(bordRt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            bordRt.sizeDelta = new Vector2(0, 2); bordRt.anchoredPosition = Vector2.zero;

            // Título (esquerda)
            var title = UIFactory.CreateText(bar.transform, "Title", "MEOW TACTICS", 26, ColGold, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            NoWrap(title); AddOutline(title);
            var trt = title.rectTransform;
            UIFactory.SetAnchors(trt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            trt.sizeDelta = new Vector2(360, 60);
            trt.anchoredPosition = new Vector2(30, 0);

            // Onda (centro) + progresso
            waveText = UIFactory.CreateText(bar.transform, "Wave", "ONDA 1 / 10", 32, ColText, TextAnchor.MiddleCenter);
            waveText.fontStyle = FontStyle.Bold;
            NoWrap(waveText); AddOutline(waveText);
            var wrt = waveText.rectTransform;
            UIFactory.SetAnchors(wrt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            wrt.sizeDelta = new Vector2(440, 46);
            wrt.anchoredPosition = new Vector2(0, 12);

            waveProgressText = UIFactory.CreateText(bar.transform, "WaveProg", "Preparação", 18, ColText, TextAnchor.MiddleCenter);
            NoWrap(waveProgressText);
            var prt = waveProgressText.rectTransform;
            UIFactory.SetAnchors(prt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            prt.sizeDelta = new Vector2(440, 24);
            prt.anchoredPosition = new Vector2(0, -22);

            // Barra de progresso da onda
            var barBg = UIFactory.CreatePanel(bar.transform, "WaveBarBg", new Color(0f, 0f, 0f, 0.55f), null, false);
            waveBarBg = barBg.gameObject;
            var bgrt = barBg.rectTransform;
            UIFactory.SetAnchors(bgrt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            bgrt.sizeDelta = new Vector2(300, 9);
            bgrt.anchoredPosition = new Vector2(0, -42);
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(barBg.transform, false);
            fillGo.GetComponent<Image>().color = ColGold;
            waveBarFill = fillGo.GetComponent<RectTransform>();
            waveBarFill.anchorMin = new Vector2(0f, 0f);
            waveBarFill.anchorMax = new Vector2(0f, 1f);
            waveBarFill.pivot = new Vector2(0f, 0.5f);
            waveBarFill.offsetMin = Vector2.zero;
            waveBarFill.offsetMax = Vector2.zero;
            waveBarBg.SetActive(false);

            // Cluster direito: vidas, moedas, velocidade
            var rightGo = new GameObject("RightCluster", typeof(RectTransform));
            rightGo.transform.SetParent(bar.transform, false);
            var rrt = rightGo.GetComponent<RectTransform>();
            UIFactory.SetAnchors(rrt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
            rrt.sizeDelta = new Vector2(760, 70);
            rrt.anchoredPosition = new Vector2(-20, 0);
            var hlg = rightGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.spacing = 10;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            livesText = StatChip(rightGo.transform, heartSprite, "20", ColRed);
            coinsText = StatChip(rightGo.transform, coinSprite, "10", ColGold);

            pauseBtn  = SmallButton(rightGo.transform, "Pause", "II", ColBlue, OpenPause);
            speed1Btn = SmallButton(rightGo.transform, "Speed1", "1x", ColGreen, () => SetSpeed(1));
            speed2Btn = SmallButton(rightGo.transform, "Speed2", "2x", ColCard, () => SetSpeed(2));
        }

        private Text StatChip(Transform parent, Sprite icon, string initial, Color color)
        {
            // Pílula arredondada de vidro escuro com ícone + valor.
            var pill = Pill(parent, "Stat", new Color(0f, 0f, 0f, 0.35f), S12, 6, S8);
            var le = pill.gameObject.AddComponent<LayoutElement>(); le.minHeight = 60; le.minWidth = 120;

            if (icon != null)
            {
                var img = UIFactory.CreateIcon(pill.transform, "Icon", icon, 40);
                Fixed(img, 40, 40);
            }
            var txt = UIFactory.CreateText(pill.transform, "Val", initial, 28, color, TextAnchor.MiddleLeft);
            txt.fontStyle = FontStyle.Bold; AddOutline(txt);
            NoWrap(txt);
            var tle = txt.gameObject.AddComponent<LayoutElement>(); tle.minWidth = 46;
            return txt;
        }

        private Button SmallButton(Transform parent, string name, string label, Color color, UnityAction onClick)
        {
            var btn = UIFactory.CreateButton(parent, name, label, color, onClick, 20, buttonSprite);
            btn.transition = Selectable.Transition.None; // deixamos o tint por conta do UpdateSpeedButtons
            btn.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            var le = btn.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 64; le.preferredWidth = 64; le.minHeight = 60; le.preferredHeight = 60;
            return btn;
        }

        // ---------- Painel de itens (esquerda, pequeno) ----------
        private void BuildItemsPanel(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "ItemsPanel", ColPanel);
            AddShadow(panel);
            itemsPanel = panel.gameObject;
            var rt = panel.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            rt.sizeDelta = new Vector2(150, 300);
            rt.anchoredPosition = new Vector2(12, -112);

            Title(panel.transform, "ITENS");

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            var crt = content.GetComponent<RectTransform>();
            UIFactory.StretchFull(crt, 10f);
            crt.offsetMax = new Vector2(crt.offsetMax.x, -44);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            vlg.spacing = 6; vlg.childAlignment = TextAnchor.UpperCenter;
            itemsContainer = content.transform;
        }

        // ---------- Painel de sinergias (direita, compacto) ----------
        private void BuildSynergyPanel(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "SynergyPanel", ColGlass);
            AddShadow(panel);
            AddSheen(panel.transform, 0.07f);
            AddTopBorder(panel.transform, ColBorder, 3f);
            var rt = panel.rectTransform;
            synergyPanelRt = rt;
            UIFactory.SetAnchors(rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            rt.sizeDelta = new Vector2(290, 392);
            rt.anchoredPosition = new Vector2(-S16, -112);

            Title(panel.transform, "SINERGIAS");

            // Botão recolher (canto superior direito do painel)
            synergyToggleBtn = UIFactory.CreateButton(panel.transform, "SynToggle", "▼", ColCard, ToggleSynergy, 18);
            synergyToggleBtn.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            var stt = UIFactory.AsRect(synergyToggleBtn);
            UIFactory.SetAnchors(stt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            stt.sizeDelta = new Vector2(40, 34);
            stt.anchoredPosition = new Vector2(-8, -8);

            // Área de rolagem (caso haja muitas sinergias ativas, ela rola em vez de cortar).
            var scrollGo = new GameObject("SynScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrt = scrollGo.GetComponent<RectTransform>();
            UIFactory.StretchFull(scrt, 10f);
            scrt.offsetMax = new Vector2(scrt.offsetMax.x, -46);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            var vprt = viewport.GetComponent<RectTransform>();
            UIFactory.StretchFull(vprt);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            vlg.spacing = 5; vlg.childAlignment = TextAnchor.UpperLeft;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vprt;
            scroll.content = crt;
            synergyContainer = content.transform;
        }

        // ---------- Área inferior: loja + banco (recolhível) + INICIAR ----------
        private void BuildBottomArea(Transform root)
        {
            // Botão grande INICIAR ONDA (premium: moldura clara + verde gradiente + ▶)
            BuildStartButton(root);

            // Preview da próxima onda (acima do botão Iniciar) — painel de ameaça com ícones.
            var nwp = UIFactory.CreatePanel(root, "NextWavePanel", ColGlass);
            AddShadow(nwp);
            AddSheen(nwp.transform, 0.07f);
            AddTopBorder(nwp.transform, new Color(0.95f, 0.45f, 0.4f, 0.6f), 3f); // borda avermelhada (ameaça)
            nextWavePanel = nwp.gameObject;
            var nwrt = nwp.rectTransform;
            UIFactory.SetAnchors(nwrt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
            nwrt.sizeDelta = new Vector2(290, 168);
            nwrt.anchoredPosition = new Vector2(-S16, 210);
            var nwTitle = UIFactory.CreateText(nwp.transform, "Title", "AMEAÇA — PRÓXIMA ONDA", 16, new Color(1f, 0.7f, 0.6f), TextAnchor.UpperCenter);
            nwTitle.fontStyle = FontStyle.Bold; NoWrap(nwTitle);
            var ntrt = nwTitle.rectTransform;
            UIFactory.SetAnchors(ntrt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            ntrt.sizeDelta = new Vector2(-12, 26); ntrt.anchoredPosition = new Vector2(0, -8);

            var threatGo = new GameObject("Threats", typeof(RectTransform));
            threatGo.transform.SetParent(nwp.transform, false);
            var thrt = threatGo.GetComponent<RectTransform>();
            UIFactory.SetAnchors(thrt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f));
            thrt.offsetMin = new Vector2(10, 10); thrt.offsetMax = new Vector2(-10, -36);
            var tgrid = threatGo.AddComponent<GridLayoutGroup>();
            tgrid.cellSize = new Vector2(84, 56); tgrid.spacing = new Vector2(6, 6);
            tgrid.padding = new RectOffset(2, 2, 2, 2);
            tgrid.childAlignment = TextAnchor.UpperLeft;
            threatContainer = threatGo.transform;

            // Aba clara de recolher/abrir a loja (canto inferior esquerdo, como uma aba do painel)
            collapseBtn = UIFactory.CreateButton(root, "Collapse", "LOJA  ▼", ColGreen,
                ToggleCollapse, 20, buttonSprite);
            collapseBtn.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            var crt2 = UIFactory.AsRect(collapseBtn);
            UIFactory.SetAnchors(crt2, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
            crt2.sizeDelta = new Vector2(160, 48);
            crt2.anchoredPosition = new Vector2(16, 352);

            // Painel recolhível (loja em cima, banco embaixo) — vidro escuro roxo/azul.
            var panelImg = UIFactory.CreatePanel(root, "BottomPanel", ColGlassDeep);
            AddShadow(panelImg, 8f, 0.5f);
            bottomPanel = panelImg.gameObject;
            AddSheen(bottomPanel.transform, 0.08f);
            AddTopBorder(bottomPanel.transform, ColBorder, 3f);
            var brt = panelImg.rectTransform;
            UIFactory.SetAnchors(brt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
            brt.sizeDelta = new Vector2(1560, 330);
            brt.anchoredPosition = new Vector2(S16, S16);

            // Linha da LOJA
            var shopRow = Row(bottomPanel.transform, "ShopRow", 196, -8);
            Label(shopRow, "LOJA", ColGold, 70);
            shopContainer = shopRow;
            rerollButton = BuildRerollButton(shopRow);

            // Linha do BANCO (chips com largura fixa, alinhados à esquerda)
            var benchRow = Row(bottomPanel.transform, "BenchRow", 104, 10);
            benchRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            benchLabel = Label(benchRow, "BANCO 0/8", ColText, 130);
            benchContainer = benchRow;
        }

        private Transform Row(Transform parent, string name, float height, float yOff)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            UIFactory.SetAnchors(rt, new Vector2(0, yOff < 0 ? 1 : 0), new Vector2(1, yOff < 0 ? 1 : 0), new Vector2(0.5f, yOff < 0 ? 1 : 0));
            rt.sizeDelta = new Vector2(-24, height);
            rt.anchoredPosition = new Vector2(0, yOff);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(12, 12, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            return go.transform;
        }

        private Text Label(Transform parent, string text, Color color, float width)
        {
            var txt = UIFactory.CreateText(parent, "Label", text, 20, color, TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold;
            NoWrap(txt);
            var le = txt.gameObject.AddComponent<LayoutElement>();
            le.minWidth = width; le.preferredWidth = width; le.flexibleWidth = 0;
            return txt;
        }

        private void AddWidth(Component c, float width)
        {
            var le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = width; le.preferredWidth = width; le.flexibleWidth = 0;
        }

        /// <summary>Botão grande INICIAR ONDA: moldura clara + verde com brilho de vidro + ▶.</summary>
        private void BuildStartButton(Transform root)
        {
            var card = new GameObject("StartWave", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(root, false);
            var outer = card.GetComponent<Image>();
            outer.sprite = UISprites.Rounded; outer.type = Image.Type.Sliced;
            outer.color = new Color(0.62f, 0.95f, 0.62f, 1f); // moldura clara
            AddShadow(outer, 8f, 0.5f);
            startWaveButton = card.GetComponent<Button>();
            startWaveButton.onClick.AddListener(() => GameManager.Instance?.StartWave());
            var srt = card.GetComponent<RectTransform>();
            UIFactory.SetAnchors(srt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
            srt.sizeDelta = new Vector2(300, 170);
            srt.anchoredPosition = new Vector2(-S16, S16 + 4);

            var inner = UIFactory.CreatePanel(card.transform, "Fill", ColGreen);
            UIFactory.StretchFull(inner.rectTransform, 4f);
            inner.raycastTarget = false;
            AddSheen(inner.transform, 0.18f);

            var vlg = inner.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter; vlg.spacing = 2;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(6, 6, 10, 10);

            var play = UIFactory.CreateText(inner.transform, "Play", "▶", 44, Color.white, TextAnchor.MiddleCenter);
            play.fontStyle = FontStyle.Bold; AddMinHeight(play, 48);
            startPlayIcon = play.gameObject;
            startLabel = UIFactory.CreateText(inner.transform, "Label", "INICIAR ONDA", 26, Color.white, TextAnchor.MiddleCenter);
            startLabel.fontStyle = FontStyle.Bold; NoWrap(startLabel); AddMinHeight(startLabel, 30);

            startPulse = card.AddComponent<UIPulse>();
            startPulse.speed = 3.2f; startPulse.amount = 0.05f;
        }

        /// <summary>Botão "Atualizar" temático: moldura dourada, ícone de refresh, custo + moeda.</summary>
        private Button BuildRerollButton(Transform parent)
        {
            var card = new GameObject("Reroll", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);
            var outer = card.GetComponent<Image>();
            outer.sprite = UISprites.Rounded; outer.type = Image.Type.Sliced; outer.color = ColGold;
            AddShadow(outer, 4f, 0.4f);
            var btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() => ShopManager.Instance?.RerollShop());
            var le = card.AddComponent<LayoutElement>();
            le.minWidth = 116; le.preferredWidth = 116; le.flexibleWidth = 0;

            var inner = UIFactory.CreatePanel(card.transform, "Fill", new Color(0.20f, 0.28f, 0.50f, 1f));
            UIFactory.StretchFull(inner.rectTransform, 3f);
            inner.raycastTarget = false;
            var vlg = inner.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter; vlg.spacing = 2;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(4, 4, 6, 6);

            var refresh = UIFactory.CreateText(inner.transform, "Icon", "↻", 34, ColGold, TextAnchor.MiddleCenter);
            refresh.fontStyle = FontStyle.Bold; NoWrap(refresh); AddMinHeight(refresh, 34);

            var costRow = new GameObject("Cost", typeof(RectTransform));
            costRow.transform.SetParent(inner.transform, false);
            var h = costRow.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter; h.spacing = 4;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            var crle = costRow.AddComponent<LayoutElement>(); crle.minHeight = 22; crle.preferredHeight = 22;
            if (coinSprite != null)
            {
                var ci = UIFactory.CreateIcon(costRow.transform, "Coin", coinSprite, 18);
                Fixed(ci, 18, 18);
            }
            var cost = UIFactory.CreateText(costRow.transform, "N", GameBalance.RerollCost.ToString(), 18, ColText, TextAnchor.MiddleLeft);
            cost.fontStyle = FontStyle.Bold; NoWrap(cost);
            return btn;
        }

        // ---------- Painéis modais ----------
        private void BuildDetailPanel(Transform root)
        {
            // Moldura dourada limpa: retângulo dourado + interior roxo escuro.
            var outer = UIFactory.CreatePanel(root, "DetailPanel", ColGold);
            detailPanel = outer.gameObject;
            AddShadow(outer, 9f, 0.5f);
            Appear(detailPanel);
            var rt = outer.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(540, 720);

            var inner = UIFactory.CreatePanel(outer.transform, "Inner", new Color(0.13f, 0.11f, 0.24f, 0.99f));
            UIFactory.StretchFull(inner.rectTransform, 6f);

            // Conteúdo (preenche o interior, deixando espaço pros botões embaixo)
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(inner.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(0, 92); crt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(28, 28, 22, 10); vlg.spacing = 5;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            detailContent = content;

            // Botões fixos no rodapé (nunca cortam)
            var btnRow = new GameObject("Buttons", typeof(RectTransform));
            btnRow.transform.SetParent(inner.transform, false);
            var brt = btnRow.GetComponent<RectTransform>();
            UIFactory.SetAnchors(brt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            brt.sizeDelta = new Vector2(-36, 70);
            brt.anchoredPosition = new Vector2(0, 14);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

            detailSellBtn = DetailButton(btnRow.transform, "Vender", ColRed,
                () => { if (detailCat != null) PlacementManager.Instance?.SellCat(detailCat); });
            detailReturnBtn = DetailButton(btnRow.transform, "Mover p/ banco", ColBlue,
                () => { if (detailCat != null) PlacementManager.Instance?.ReturnCatToBench(detailCat); });
            DetailButton(btnRow.transform, "Fechar", ColCard,
                () => PlacementManager.Instance?.ClearFocus());

            detailPanel.SetActive(false);
        }

        private Button DetailButton(Transform parent, string label, Color color, UnityAction onClick)
        {
            var b = UIFactory.CreateButton(parent, "DBtn", label, color, onClick, 20);
            var t = b.GetComponentInChildren<Text>();
            t.fontStyle = FontStyle.Bold; AddOutline(t);
            return b;
        }

        private void BuildDraftPanel(Transform root)
        {
            draftPanel = UIFactory.CreatePanel(root, "DraftPanel", new Color(0, 0, 0, 0.85f), null, false).gameObject;
            UIFactory.StretchFull((RectTransform)draftPanel.transform);
            Appear(draftPanel, 1f);

            var t = UIFactory.CreateText(draftPanel.transform, "Title", "ESCOLHA UM ITEM!", 44, ColGold, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold; NoWrap(t); AddOutline(t);
            var trt = t.rectTransform;
            UIFactory.SetAnchors(trt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            trt.sizeDelta = new Vector2(900, 70);
            trt.anchoredPosition = new Vector2(0, -120);

            var row = new GameObject("Choices", typeof(RectTransform));
            row.transform.SetParent(draftPanel.transform, false);
            var rrt = row.GetComponent<RectTransform>();
            UIFactory.SetAnchors(rrt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rrt.sizeDelta = new Vector2(1020, 340);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 26; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            draftContainer = row.transform;

            draftPanel.SetActive(false);
        }

        private void BuildEndPanel(Transform root)
        {
            endPanel = UIFactory.CreatePanel(root, "EndPanel", new Color(0, 0, 0, 0.9f), null, false).gameObject;
            UIFactory.StretchFull((RectTransform)endPanel.transform);
            Appear(endPanel, 1f);

            endText = UIFactory.CreateText(endPanel.transform, "EndText", "", 38, ColGold, TextAnchor.UpperCenter);
            endText.fontStyle = FontStyle.Bold;
            var ert = endText.rectTransform;
            UIFactory.SetAnchors(ert, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            ert.sizeDelta = new Vector2(1100, 470);
            ert.anchoredPosition = new Vector2(0, -70);

            var btnRow = new GameObject("EndButtons", typeof(RectTransform));
            btnRow.transform.SetParent(endPanel.transform, false);
            var brt = btnRow.GetComponent<RectTransform>();
            UIFactory.SetAnchors(brt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            brt.sizeDelta = new Vector2(740, 96);
            brt.anchoredPosition = new Vector2(0, -180);
            var hlg2 = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg2.spacing = 24; hlg2.childForceExpandWidth = true; hlg2.childForceExpandHeight = true;

            var again = UIFactory.CreateButton(btnRow.transform, "Restart", "Jogar Novamente", ColGreen,
                () => GameManager.Instance?.NewGame(), 26, buttonSprite);
            again.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            var toMenu = UIFactory.CreateButton(btnRow.transform, "ToMenu", "Voltar ao Menu", ColCard,
                () => GameManager.Instance?.GoToMenu(), 26, buttonSprite);
            toMenu.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;

            endPanel.SetActive(false);
        }

        private void BuildMessage(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "MessageToast", new Color(0, 0, 0, 0.82f));
            AddShadow(panel, 5f, 0.5f);
            var rt = panel.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            rt.sizeDelta = new Vector2(760, 64);
            rt.anchoredPosition = new Vector2(0, -118);
            messageText = UIFactory.CreateText(panel.transform, "Text", "", 26, ColText);
            messageText.fontStyle = FontStyle.Bold;
            UIFactory.StretchFull(messageText.rectTransform, 10);
            panel.gameObject.SetActive(false);
        }

        // =========================================================
        //  HELPERS DE CRIAÇÃO
        // =========================================================
        private Text Title(Transform parent, string text)
        {
            var t = UIFactory.CreateText(parent, "Title", text, 22, ColGold, TextAnchor.UpperCenter);
            t.fontStyle = FontStyle.Bold;
            NoWrap(t);
            var rt = t.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            rt.sizeDelta = new Vector2(-10, 36);
            rt.anchoredPosition = new Vector2(0, -8);
            return t;
        }

        private static void NoWrap(Text t)
        {
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void AddOutline(Text t)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.7f);
            o.effectDistance = new Vector2(2f, -2f);
        }

        /// <summary>Sombra projetada suave atrás de um painel/botão (dá profundidade).</summary>
        private static void AddShadow(Graphic g, float dist = 6f, float alpha = 0.40f)
        {
            if (g == null) return;
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, alpha);
            s.effectDistance = new Vector2(dist, -dist);
        }

        /// <summary>Adiciona fade-in + leve "pop" ao mostrar a tela/modal.</summary>
        private static void Appear(GameObject go, float startScale = 0.96f)
        {
            if (go == null) return;
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
            var a = go.GetComponent<UIAppear>();
            if (a == null) a = go.AddComponent<UIAppear>();
            a.startScale = startScale;
        }

        // =========================================================
        //  DESIGN SYSTEM — componentes reutilizáveis
        // =========================================================

        /// <summary>Brilho de vidro: gradiente claro no topo, esmaece pra baixo. Inset pra não vazar nos cantos.</summary>
        private static void AddSheen(Transform panel, float alpha = 0.10f)
        {
            var go = new GameObject("Sheen", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel, false);
            var img = go.GetComponent<Image>();
            img.sprite = UISprites.Sheen;
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, alpha);
            img.raycastTarget = false;
            UIFactory.StretchFull(go.GetComponent<RectTransform>(), 5f);
            IgnoreLayout(go);
            go.transform.SetAsFirstSibling(); // fica atrás do conteúdo
        }

        /// <summary>Faz um elemento ser ignorado por layout groups (mantém ancoragem própria).</summary>
        private static void IgnoreLayout(GameObject go)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        /// <summary>Linha fina e iluminada na borda superior do painel (estilo vidro).</summary>
        private static void AddTopBorder(Transform panel, Color color, float height = 3f)
        {
            var go = new GameObject("TopBorder", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel, false);
            var img = go.GetComponent<Image>();
            img.sprite = UISprites.Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            IgnoreLayout(go);
            var rt = go.GetComponent<RectTransform>();
            UIFactory.SetAnchors(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            rt.sizeDelta = new Vector2(-24, height);
            rt.anchoredPosition = new Vector2(0, -2);
        }

        /// <summary>Card com moldura colorida: Image externa (cor da borda) + Image interna (fundo).
        /// Devolve a Image INTERNA (onde vai o conteúdo).</summary>
        private static Image BorderedCard(Transform parent, string name, Color border, Color fill, float thickness = 3f)
        {
            var outer = UIFactory.CreatePanel(parent, name, border);
            var inner = UIFactory.CreatePanel(outer.transform, "Fill", fill);
            UIFactory.StretchFull(inner.rectTransform, thickness);
            return inner;
        }

        /// <summary>Chip arredondado (pílula) com HorizontalLayoutGroup pronto pra ícone+texto.</summary>
        private static Image Pill(Transform parent, string name, Color bg, int padX = 10, int padY = 4, int spacing = 6)
        {
            var img = UIFactory.CreatePanel(parent, name, bg);
            var h = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter; h.spacing = spacing;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            h.padding = new RectOffset(padX, padX, padY, padY);
            return img;
        }

        /// <summary>Brilho radial atrás de um elemento (sinergia completa / seleção).</summary>
        private static Image GlowBehind(Transform parent, Color color, float padding = -10f)
        {
            var go = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = UISprites.Glow;
            img.color = color;
            img.raycastTarget = false;
            UIFactory.StretchFull(go.GetComponent<RectTransform>(), padding);
            IgnoreLayout(go);
            go.transform.SetAsFirstSibling();
            return img;
        }

        private static LayoutElement Fixed(Component c, float w, float h)
        {
            var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = w; le.preferredWidth = w; le.minHeight = h; le.preferredHeight = h; le.flexibleWidth = 0; le.flexibleHeight = 0;
            return le;
        }

        // =========================================================
        //  EVENTOS
        // =========================================================
        private void Subscribe()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnCoinsChanged += _ => UpdateCoins();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLivesChanged += _ => UpdateLives();
                GameManager.Instance.OnStateChanged += OnStateChanged;
            }
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveChanged += (_, __) => { UpdateWave(); UpdateNextWavePreview(); };
                WaveManager.Instance.OnWaveProgress += UpdateWaveProgress;
            }
            if (ShopManager.Instance != null) ShopManager.Instance.OnShopChanged += UpdateShop;
            if (BenchManager.Instance != null) BenchManager.Instance.OnBenchChanged += UpdateBench;
            if (SynergyManager.Instance != null) SynergyManager.Instance.OnSynergiesChanged += _ => UpdateSynergies();
            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.OnInventoryChanged += UpdateItems;
                ItemManager.Instance.OnSelectionChanged += UpdateItems;
            }
        }

        private void RefreshAll()
        {
            everActivated.Clear();
            prevActiveSynergies.Clear();
            UpdateCoins(); UpdateLives(); UpdateWave();
            UpdateShop(); UpdateBench(); UpdateSynergies(); UpdateItems();
            UpdateSpeedButtons(); UpdateNextWavePreview();
            // Garante que a HUD reflita o estado atual (defensivo contra ordem de Start).
            if (GameManager.Instance != null) OnStateChanged(GameManager.Instance.State);
        }

        private void OnStateChanged(GameState state)
        {
            bool prep = state == GameState.Preparation;
            if (startWaveButton != null) startWaveButton.interactable = prep;
            if (rerollButton != null) rerollButton.interactable = prep;
            if (startLabel != null) startLabel.text = prep ? "INICIAR ONDA" : "EM ANDAMENTO";
            if (startPlayIcon != null) startPlayIcon.SetActive(prep);
            if (startPulse != null) startPulse.active = prep;
            if (waveProgressText != null && prep) waveProgressText.text = "Preparação — posicione seus gatos";
            if (nextWavePanel != null) nextWavePanel.SetActive(prep);
            if (prep) UpdateNextWavePreview();

            // Auto: recolhe loja/banco e sinergias durante a onda; reabre na preparação.
            if (state == GameState.WaveInProgress) { SetBottomCollapsed(true); SetSynergyCollapsed(true); }
            else if (prep) { SetBottomCollapsed(false); SetSynergyCollapsed(false); }

            // Mantém a velocidade escolhida ao entrar em combate.
            if (state == GameState.WaveInProgress) Time.timeScale = gameSpeed;
            else if (prep) Time.timeScale = 1f;

            if (state == GameState.Victory) ShowVictoryScreen();
            else if (state == GameState.Defeat) ShowDefeatScreen();
        }

        // =========================================================
        //  VELOCIDADE / PAUSE
        // =========================================================
        private void SetSpeed(int s)
        {
            gameSpeed = s;
            if (Time.timeScale > 0f) Time.timeScale = s; // não tira do pause
            UpdateSpeedButtons();
            SFXManager.Play(SfxType.Click);
        }

        private void UpdateSpeedButtons()
        {
            Tint(pauseBtn, ColBlue);
            Tint(speed1Btn, gameSpeed == 1 ? ColGreen : ColCard);
            Tint(speed2Btn, gameSpeed == 2 ? ColGreen : ColCard);
        }

        private static void Tint(Button b, Color c)
        {
            if (b == null) return;
            var img = b.GetComponent<Image>();
            if (img != null) img.color = (img.sprite != null) ? Color.Lerp(Color.white, c, 0.5f) : c;
        }

        private void ToggleCollapse()
        {
            SetBottomCollapsed(!collapsed);
            SFXManager.Play(SfxType.Click);
        }

        private void SetBottomCollapsed(bool v)
        {
            collapsed = v;
            if (bottomPanel != null) bottomPanel.SetActive(!v);
            var lbl = collapseBtn != null ? collapseBtn.GetComponentInChildren<Text>() : null;
            if (lbl != null) lbl.text = v ? "LOJA  ▲" : "LOJA  ▼";
        }

        private void ToggleSynergy()
        {
            SetSynergyCollapsed(!synergyCollapsed);
            SFXManager.Play(SfxType.Click);
        }

        private void SetSynergyCollapsed(bool v)
        {
            synergyCollapsed = v;
            if (synergyContainer != null) synergyContainer.gameObject.SetActive(!v);
            if (synergyPanelRt != null) synergyPanelRt.sizeDelta = new Vector2(290, v ? 50 : 392);
            var lbl = synergyToggleBtn != null ? synergyToggleBtn.GetComponentInChildren<Text>() : null;
            if (lbl != null) lbl.text = v ? "▲" : "▼";
        }

        // =========================================================
        //  TEXTOS
        // =========================================================
        public void UpdateCoins()
        {
            if (coinsText != null && EconomyManager.Instance != null)
                coinsText.text = EconomyManager.Instance.Coins.ToString();
        }

        public void UpdateLives()
        {
            if (livesText != null && GameManager.Instance != null)
                livesText.text = GameManager.Instance.Lives.ToString();
        }

        public void UpdateWave()
        {
            if (waveText != null && WaveManager.Instance != null)
                waveText.text = "ONDA " + (WaveManager.Instance.CurrentWaveIndex + 1) + " / " + WaveManager.Instance.waves.Count;
        }

        public void UpdateWaveProgress()
        {
            if (waveProgressText == null || WaveManager.Instance == null) return;
            var wm = WaveManager.Instance;
            if (wm.IsWaveRunning)
            {
                waveProgressText.text = $"Inimigos: {wm.EnemiesRemaining} / {wm.EnemiesTotal}";
                if (waveBarBg != null) waveBarBg.SetActive(true);
                if (waveBarFill != null)
                {
                    float ratio = wm.EnemiesTotal > 0
                        ? (float)(wm.EnemiesTotal - wm.EnemiesRemaining) / wm.EnemiesTotal : 0f;
                    waveBarFill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
                }
            }
            else
            {
                waveProgressText.text = "Preparação — posicione seus gatos";
                if (waveBarBg != null) waveBarBg.SetActive(false);
            }
        }

        public void UpdateNextWavePreview()
        {
            if (threatContainer == null || WaveManager.Instance == null) return;
            ClearDynamic(threatContainer, "Threat");
            var w = WaveManager.Instance.CurrentWave;
            if (w == null) return;

            // Agrupa por tipo de inimigo, preservando a ordem de aparição.
            var counts = new Dictionary<EnemyData, int>();
            var order = new List<EnemyData>();
            foreach (var info in w.enemies)
            {
                if (info.enemy == null) continue;
                if (!counts.ContainsKey(info.enemy)) { counts[info.enemy] = 0; order.Add(info.enemy); }
                counts[info.enemy] += info.count;
            }
            foreach (var e in order) ThreatChip(e, counts[e]);
        }

        /// <summary>Chip de ameaça: ícone do inimigo + quantidade, com borda colorida pelo tipo.</summary>
        private void ThreatChip(EnemyData e, int count)
        {
            Color border = ThreatColor(e);
            var cell = BorderedCard(threatContainer, "Threat", border, new Color(0.10f, 0.10f, 0.16f, 0.95f), 2.5f);
            var hlg = cell.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = 3;
            hlg.padding = new RectOffset(4, 4, 2, 2);
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            if (e.icon != null)
            {
                var ic = UIFactory.CreateIcon(cell.transform, "Icon", e.icon, 34);
                Fixed(ic, 34, 34);
            }
            else
            {
                var sw = UIFactory.CreatePanel(cell.transform, "Sw", border);
                Fixed(sw, 30, 30);
            }
            var txt = UIFactory.CreateText(cell.transform, "N", "x" + count, 16, ColText, TextAnchor.MiddleLeft);
            txt.fontStyle = FontStyle.Bold; NoWrap(txt);
            if (e.isBoss) { txt.color = ColGold; txt.text = "★" + count; }
        }

        private static Color ThreatColor(EnemyData e)
        {
            if (e.isBoss) return new Color(1f, 0.8f, 0.3f);              // boss: dourado
            if (e.armor >= 30f) return new Color(0.78f, 0.82f, 0.90f);  // blindado: cinza
            if (e.magicResistance >= 30f) return new Color(0.72f, 0.5f, 1f); // místico: roxo
            if (e.moveSpeed >= 1.5f) return new Color(0.4f, 0.9f, 1f);   // veloz: ciano
            return new Color(0.85f, 0.5f, 0.5f);                         // comum: vermelho suave
        }

        // =========================================================
        //  LOJA (cartões com ícone do gato)
        // =========================================================
        public void UpdateShop()
        {
            if (shopContainer == null || ShopManager.Instance == null) return;
            ClearDynamic(shopContainer, "ShopCard");

            var options = ShopManager.Instance.currentShopOptions;
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                CatData cat = options[i];
                BuildCatCard(shopContainer, cat, () => ShopManager.Instance.BuyCat(index));
            }
        }

        private void BuildCatCard(Transform parent, CatData cat, UnityAction onClick)
        {
            var card = new GameObject("ShopCard", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);
            bool afford = cat != null && ShopManager.Instance != null && ShopManager.Instance.CanBuy(cat);

            // Moldura externa colorida pela raridade.
            Color rar = cat != null ? RarityColor(cat.cost) : new Color(0.28f, 0.28f, 0.34f, 1f);
            var outer = card.GetComponent<Image>();
            outer.sprite = UISprites.Rounded; outer.type = Image.Type.Sliced;
            outer.color = cat == null ? new Color(0.20f, 0.20f, 0.26f, 0.9f)
                                      : (afford ? rar : new Color(rar.r * 0.5f, rar.g * 0.5f, rar.b * 0.5f, 0.9f));
            AddShadow(outer, 5f, 0.4f);
            var btn = card.GetComponent<Button>();
            btn.interactable = cat != null; // mesmo sem moeda, deixa clicar p/ mostrar aviso
            if (cat != null && onClick != null) btn.onClick.AddListener(onClick);

            if (cat == null)
            {
                var empty = UIFactory.CreatePanel(card.transform, "Fill", new Color(0.12f, 0.12f, 0.17f, 0.95f));
                UIFactory.StretchFull(empty.rectTransform, 3f); empty.raycastTarget = false;
                var dash = UIFactory.CreateText(empty.transform, "Dash", "—", 26, ColDim);
                UIFactory.StretchFull(dash.rectTransform);
                return;
            }

            // Fundo interno escuro (gradiente via sheen) dentro da moldura.
            Color fill = afford ? new Color(0.16f, 0.15f, 0.28f, 1f) : new Color(0.11f, 0.10f, 0.16f, 0.97f);
            var inner = UIFactory.CreatePanel(card.transform, "Fill", fill);
            UIFactory.StretchFull(inner.rectTransform, 3f);
            inner.raycastTarget = false;
            AddSheen(inner.transform, 0.10f);

            var vlg = inner.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, S8, 6); vlg.spacing = 4;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Ícone grande do gato (esmaecido se não puder comprar)
            if (cat.icon != null)
            {
                var icon = UIFactory.CreateIcon(inner.transform, "Icon", cat.icon, 84);
                icon.color = afford ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.85f);
                AddMinHeight(icon, 84);
            }

            // Nome (alto contraste)
            var name = UIFactory.CreateText(inner.transform, "Name", cat.catName, 18,
                afford ? ColText : ColDim, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Bold; NoWrap(name); AddOutline(name); AddMinHeight(name, 22);

            // Custo: moeda + número dourado bem visível
            var costRow = new GameObject("CostRow", typeof(RectTransform));
            costRow.transform.SetParent(inner.transform, false);
            var crl = costRow.AddComponent<HorizontalLayoutGroup>();
            crl.childAlignment = TextAnchor.MiddleCenter; crl.spacing = 5;
            crl.childForceExpandWidth = false; crl.childForceExpandHeight = false;
            var crle = costRow.AddComponent<LayoutElement>(); crle.minHeight = 26; crle.preferredHeight = 26;
            if (coinSprite != null)
            {
                var ci = UIFactory.CreateIcon(costRow.transform, "Coin", coinSprite, 24);
                Fixed(ci, 24, 24);
            }
            var costTxt = UIFactory.CreateText(costRow.transform, "Cost", cat.cost.ToString(), 20, ColGold, TextAnchor.MiddleLeft);
            costTxt.fontStyle = FontStyle.Bold; NoWrap(costTxt);

            // Chips de sinergia (classe/origem)
            var chipRow = new GameObject("Chips", typeof(RectTransform));
            chipRow.transform.SetParent(inner.transform, false);
            var chl = chipRow.AddComponent<HorizontalLayoutGroup>();
            chl.childAlignment = TextAnchor.MiddleCenter; chl.spacing = 4;
            chl.childForceExpandWidth = false; chl.childForceExpandHeight = false;
            var chle = chipRow.AddComponent<LayoutElement>(); chle.minHeight = 24; chle.preferredHeight = 24;
            foreach (var t in cat.synergies) SynergyChip(chipRow.transform, t);
        }

        /// <summary>Chip pequeno (pílula) de uma sinergia, colorido pela cor da sinergia.</summary>
        private void SynergyChip(Transform parent, SynergyType t)
        {
            Color c = SynergyColor(t);
            var pill = Pill(parent, "Chip", new Color(c.r, c.g, c.b, 0.30f), 8, 2, 0);
            var txt = UIFactory.CreateText(pill.transform, "T", SynergyLabel(t), 12,
                Color.Lerp(c, Color.white, 0.45f), TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold; NoWrap(txt);
        }

        private static void AddMinHeight(Component c, float h)
        {
            var le = c.gameObject.AddComponent<LayoutElement>();
            le.minHeight = h; le.preferredHeight = h;
        }

        // =========================================================
        //  BANCO (chips com ícone)
        // =========================================================
        public void UpdateBench()
        {
            if (benchContainer == null || BenchManager.Instance == null) return;
            ClearDynamic(benchContainer, "BenchChip");

            int count = BenchManager.Instance.benchCats.Count;
            if (benchLabel != null) benchLabel.text = $"BANCO {count}/{GameBalance.BenchSize}";

            foreach (var cat in BenchManager.Instance.benchCats)
            {
                CatUnit c = cat;
                var chip = new GameObject("BenchChip", typeof(RectTransform), typeof(Image), typeof(Button));
                chip.transform.SetParent(benchContainer, false);
                var ci = chip.GetComponent<Image>();
                ci.sprite = UISprites.Rounded; ci.type = Image.Type.Sliced; ci.color = ColCard;
                AddShadow(ci, 3f, 0.3f);
                chip.GetComponent<Button>().onClick.AddListener(() => PlacementManager.Instance?.SelectBenchCatForPlacement(c));
                var chipLe = chip.AddComponent<LayoutElement>();
                chipLe.minWidth = 150; chipLe.preferredWidth = 162; chipLe.flexibleWidth = 0;

                var hlg = chip.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(S8, S8, 4, 4); hlg.spacing = S8;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

                if (c.Data.icon != null)
                {
                    var icon = UIFactory.CreateIcon(chip.transform, "Icon", c.Data.icon, 60);
                    Fixed(icon, 60, 60);
                }
                string txt = c.Data.catName + (c.Items.Count > 0 ? $"\n<color=#9fe89f>{c.Items.Count} itens</color>" : "");
                UIFactory.CreateText(chip.transform, "Name", txt, 14, ColText, TextAnchor.MiddleLeft);
            }

            // Slots vazios (visual de banco com espaços livres).
            int empties = Mathf.Max(0, GameBalance.BenchSize - count);
            for (int i = 0; i < empties; i++) BenchEmptySlot(benchContainer);
        }

        /// <summary>Slot vazio do banco: quadradinho arredondado discreto.</summary>
        private void BenchEmptySlot(Transform parent)
        {
            var slot = UIFactory.CreatePanel(parent, "BenchChip", ColSlot);
            var le = slot.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 60; le.preferredWidth = 60; le.flexibleWidth = 0;
            var dot = UIFactory.CreateText(slot.transform, "Dot", "+", 24, new Color(1f, 1f, 1f, 0.18f), TextAnchor.MiddleCenter);
            UIFactory.StretchFull(dot.rectTransform);
        }

        // =========================================================
        //  SINERGIAS (contador "Nome 2/3")
        // =========================================================
        public void UpdateSynergies()
        {
            if (synergyContainer == null || SynergyManager.Instance == null) return;
            HideTooltip(); // evita tooltip preso ao reconstruir as linhas
            ClearDynamic(synergyContainer, "SynRow");

            bool any = false;
            var nowActive = new HashSet<SynergyType>();
            foreach (var s in SynergyManager.Instance.GetActiveSynergies())
            {
                if (s.count <= 0) continue;
                any = true;

                if (s.IsActive)
                {
                    nowActive.Add(s.data.synergyType);
                    if (!prevActiveSynergies.Contains(s.data.synergyType))
                    {
                        everActivated.Add(s.data.synergyType);
                        ShowMessage($"SINERGIA ATIVADA: {DisplayName(s)}!   {s.activeTier.description}");
                        SFXManager.Play(SfxType.Synergy);
                        AchievementManager.Instance?.Report("synergiesActivated", 1);
                    }
                }

                BuildSynergyRow(s);
            }

            // Conquista de "X sinergias ativas na mesma partida".
            AchievementManager.Instance?.ReportMax("maxSynergiesInMatch", nowActive.Count);

            prevActiveSynergies.Clear();
            foreach (var t in nowActive) prevActiveSynergies.Add(t);

            if (!any)
            {
                var hint = UIFactory.CreateText(synergyContainer, "SynRow",
                    "Posicione gatos para\nativar sinergias!", 16, ColDim, TextAnchor.UpperLeft);
                var le = hint.gameObject.AddComponent<LayoutElement>(); le.minHeight = 50;
            }
        }

        /// <summary>Linha compacta de sinergia: badge de contagem + nome + nível, com glow/★ quando completa.</summary>
        private void BuildSynergyRow(SynergyStatus s)
        {
            int next = NextThreshold(s.data, s.count);
            Color c = s.data.uiColor;
            bool active = s.IsActive;

            // Fundo da linha (mais aceso quando ativa) — pílula arredondada.
            Color rowBg = active ? new Color(c.r, c.g, c.b, 0.22f) : ColSlot;
            var row = UIFactory.CreatePanel(synergyContainer, "SynRow", rowBg);
            var rle = row.gameObject.AddComponent<LayoutElement>(); rle.minHeight = 44; rle.preferredHeight = 44;
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft; hlg.spacing = S8;
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

            if (active) GlowBehind(row.transform, new Color(c.r, c.g, c.b, 0.45f), -6f);

            // Badge de contagem (quadrado arredondado colorido).
            var badge = UIFactory.CreatePanel(row.transform, "Badge",
                active ? c : new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 0.9f));
            Fixed(badge, 34, 34);
            var bnum = UIFactory.CreateText(badge.transform, "N", s.count.ToString(), 18, Color.white, TextAnchor.MiddleCenter);
            bnum.fontStyle = FontStyle.Bold; AddOutline(bnum);
            UIFactory.StretchFull(bnum.rectTransform);

            // Coluna central: nome + nível alvo.
            var colGo = new GameObject("Col", typeof(RectTransform));
            colGo.transform.SetParent(row.transform, false);
            var cle = colGo.AddComponent<LayoutElement>(); cle.flexibleWidth = 1;
            var cvl = colGo.AddComponent<VerticalLayoutGroup>();
            cvl.childAlignment = TextAnchor.MiddleLeft; cvl.spacing = 0;
            cvl.childForceExpandWidth = true; cvl.childForceExpandHeight = false;
            var nm = UIFactory.CreateText(colGo.transform, "Name", DisplayName(s), 18,
                active ? Color.Lerp(c, Color.white, 0.5f) : ColText, TextAnchor.MiddleLeft);
            nm.fontStyle = FontStyle.Bold; NoWrap(nm); AddMinHeight(nm, 22);
            var sub = UIFactory.CreateText(colGo.transform, "Sub",
                active && s.activeTier != null ? s.activeTier.description : $"{s.count} / {next}",
                12, active ? new Color(0.86f, 0.86f, 0.62f) : ColDim, TextAnchor.MiddleLeft);
            NoWrap(sub); AddMinHeight(sub, 16);

            // Estrela de conquista quando completa.
            if (active)
            {
                var star = UIFactory.CreateText(row.transform, "Star", "★", 22, ColGold, TextAnchor.MiddleCenter);
                AddOutline(star); Fixed(star, 24, 30);
            }

            // Tooltip ao passar/tocar.
            SynergyData data = s.data;
            var trigger = row.gameObject.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, _ => ShowSynergyTooltip(data));
            AddTrigger(trigger, EventTriggerType.PointerClick, _ =>
            {
                ShowSynergyTooltip(data);
                if (tooltipHideRoutine != null) StopCoroutine(tooltipHideRoutine);
                tooltipHideRoutine = StartCoroutine(HideTooltipAfter(3.5f));
            });
            AddTrigger(trigger, EventTriggerType.PointerExit, _ => HideTooltip());
        }

        private static string DisplayName(SynergyStatus s)
        {
            return string.IsNullOrEmpty(s.data.displayName) ? s.data.synergyType.ToString() : s.data.displayName;
        }

        private static int NextThreshold(SynergyData d, int count)
        {
            int top = count;
            foreach (var tier in d.tiers)
            {
                top = Mathf.Max(top, tier.requiredCount);
                if (count < tier.requiredCount) return tier.requiredCount;
            }
            return top; // já no nível máximo
        }

        // =========================================================
        //  ITENS (inventário)
        // =========================================================
        public void UpdateItems()
        {
            if (itemsContainer == null || ItemManager.Instance == null) return;
            ClearDynamic(itemsContainer, "ItemBtn");

            // Não deixa um painel grande e vazio: só aparece quando há itens.
            bool hasItems = ItemManager.Instance.inventory.Count > 0;
            if (itemsPanel != null) itemsPanel.SetActive(hasItems);
            if (!hasItems) return;

            foreach (var item in ItemManager.Instance.inventory)
            {
                ItemData it = item;
                bool selected = ItemManager.Instance.SelectedForEquip == it;

                var chip = new GameObject("ItemBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                chip.transform.SetParent(itemsContainer, false);
                chip.GetComponent<Image>().color = selected ? new Color(0.95f, 0.85f, 0.35f, 0.92f) : ColCard;
                chip.GetComponent<Button>().onClick.AddListener(() => ToggleItemSelection(it));
                var le = chip.AddComponent<LayoutElement>(); le.minHeight = 48; le.preferredHeight = 48;

                var hlg = chip.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(6, 6, 4, 4); hlg.spacing = 6;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

                if (it.icon != null)
                {
                    var ic = UIFactory.CreateIcon(chip.transform, "Icon", it.icon, 38);
                    var ile = ic.gameObject.AddComponent<LayoutElement>();
                    ile.minWidth = 38; ile.preferredWidth = 38; ile.minHeight = 38; ile.preferredHeight = 38;
                }
                UIFactory.CreateText(chip.transform, "Name", it.itemName, 12,
                    selected ? Color.black : ColText, TextAnchor.MiddleLeft);

                var trig = chip.AddComponent<EventTrigger>();
                // Desktop: hover mostra/esconde. Mobile: toque mostra (e some sozinho).
                AddTrigger(trig, EventTriggerType.PointerEnter, _ => ShowTextTooltip(ItemBonusText(it)));
                AddTrigger(trig, EventTriggerType.PointerExit, _ => HideTooltip());
                AddTrigger(trig, EventTriggerType.PointerClick, _ => ShowItemTooltip(it));
            }
        }

        /// <summary>Mostra os bônus do item perto do toque e some sozinho (amigável a touch).</summary>
        private void ShowItemTooltip(ItemData it)
        {
            ShowTextTooltip(ItemBonusText(it));
            if (tooltipHideRoutine != null) StopCoroutine(tooltipHideRoutine);
            tooltipHideRoutine = StartCoroutine(HideTooltipAfter(3.5f));
        }

        private IEnumerator HideTooltipAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            HideTooltip();
        }

        private void ToggleItemSelection(ItemData item)
        {
            if (ItemManager.Instance.SelectedForEquip == item)
            {
                ItemManager.Instance.ClearSelection();
                ShowMessage("Item cancelado.");
            }
            else
            {
                ItemManager.Instance.SelectForEquip(item);
                // Mostra o que o item faz já na seleção (importante no mobile, sem hover).
                ShowMessage($"{item.itemName}: {item.description}. Toque num gato para equipar!");
            }
        }

        // =========================================================
        //  DETALHE DO GATO
        // =========================================================
        public void ShowCatDetail(CatUnit cat)
        {
            if (cat == null || detailContent == null) return;
            detailCat = cat;

            foreach (Transform c in detailContent.transform) Destroy(c.gameObject);

            var name = DetailText(cat.Data.catName, 30, ColGold, TextAnchor.MiddleCenter, 40);
            name.fontStyle = FontStyle.Bold; NoWrap(name); AddOutline(name);

            DetailText($"{DamageName(cat.Data.damageType)}  •  {SynergyNames(new List<SynergyType>(cat.GetEffectiveSynergies()))}",
                15, ColDim, TextAnchor.MiddleCenter, 22);

            if (cat.Data.icon != null)
            {
                var ic = UIFactory.CreateIcon(detailContent.transform, "Portrait", cat.Data.icon, 82);
                var le = ic.gameObject.AddComponent<LayoutElement>(); le.minHeight = 82; le.preferredHeight = 82;
            }

            DetailHeader("ATRIBUTOS");
            StatRow("Dano", $"{cat.CurrentDamageDisplay:0.#}", ColText);
            StatRow("Tipo de dano", DamageName(cat.Data.damageType), DamageColor(cat.Data.damageType));
            StatRow("Vel. ataque", $"{cat.CurrentAttackSpeed:0.##}/s", ColText);
            StatRow("Alcance", $"{cat.CurrentRange:0.#}", ColText);
            StatRow("Crítico", $"{cat.CurrentCritChance:0}%", ColText);

            DetailHeader("SINERGIAS");
            DetailText(SynergyNames(new List<SynergyType>(cat.GetEffectiveSynergies())),
                16, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleCenter, 24);

            DetailHeader(cat.Items.Count > 0 ? "ITENS EQUIPADOS" : "ITENS");
            if (cat.Items.Count == 0) DetailText("nenhum", 15, ColDim, TextAnchor.MiddleCenter, 22);
            else foreach (var it in cat.Items) ItemDetailRow(it);

            detailReturnBtn.interactable = cat.IsPlaced;
            detailPanel.SetActive(true);
        }

        private Text DetailText(string txt, int size, Color color, TextAnchor anchor, float minH)
        {
            var t = UIFactory.CreateText(detailContent.transform, "DTxt", txt, size, color, anchor);
            var le = t.gameObject.AddComponent<LayoutElement>(); le.minHeight = minH; le.preferredHeight = minH;
            return t;
        }

        private void DetailHeader(string txt)
        {
            var t = UIFactory.CreateText(detailContent.transform, "DHdr", txt, 16, ColGold, TextAnchor.MiddleLeft);
            t.fontStyle = FontStyle.Bold; NoWrap(t);
            var le = t.gameObject.AddComponent<LayoutElement>(); le.minHeight = 28; le.preferredHeight = 28;
        }

        private void StatRow(string label, string value, Color valColor)
        {
            var row = new GameObject("StatRow", typeof(RectTransform));
            row.transform.SetParent(detailContent.transform, false);
            var le = row.AddComponent<LayoutElement>(); le.minHeight = 26; le.preferredHeight = 26;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true; hlg.childAlignment = TextAnchor.MiddleLeft;
            var l = UIFactory.CreateText(row.transform, "L", label, 16, ColDim, TextAnchor.MiddleLeft); NoWrap(l);
            var v = UIFactory.CreateText(row.transform, "V", value, 16, valColor, TextAnchor.MiddleRight); v.fontStyle = FontStyle.Bold; NoWrap(v);
        }

        private void ItemDetailRow(ItemData it)
        {
            var row = new GameObject("ItemRow", typeof(RectTransform));
            row.transform.SetParent(detailContent.transform, false);
            var le = row.AddComponent<LayoutElement>(); le.minHeight = 44; le.preferredHeight = 44;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8; hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false; hlg.childAlignment = TextAnchor.MiddleLeft;
            if (it.icon != null)
            {
                var ic = UIFactory.CreateIcon(row.transform, "Icon", it.icon, 40);
                var ile = ic.gameObject.AddComponent<LayoutElement>();
                ile.minWidth = 40; ile.preferredWidth = 40; ile.minHeight = 40; ile.preferredHeight = 40;
            }
            var txt = UIFactory.CreateText(row.transform, "Txt",
                $"<b>{it.itemName}</b>   <color=#9fe89f>{it.description}</color>", 14, ColText, TextAnchor.MiddleLeft);
            var tle = txt.gameObject.AddComponent<LayoutElement>(); tle.minWidth = 380; tle.flexibleWidth = 1;
        }

        public void HideCatDetail()
        {
            detailCat = null;
            if (detailPanel != null) detailPanel.SetActive(false);
        }

        // =========================================================
        //  ROLETA DE ITENS
        // =========================================================
        public void ShowItemDraft(List<ItemData> choices)
        {
            if (draftPanel == null) return;
            ClearDynamic(draftContainer, "DraftBtn");

            foreach (var item in choices)
            {
                ItemData it = item;
                BuildItemDraftCard(draftContainer, it, () => PickDraftItem(it));
            }
            draftPanel.SetActive(true);
        }

        private void BuildItemDraftCard(Transform parent, ItemData it, UnityAction onClick)
        {
            var card = new GameObject("DraftBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);
            var dimg = card.GetComponent<Image>();
            dimg.sprite = UISprites.Rounded;
            dimg.type = Image.Type.Sliced;
            dimg.color = new Color(0.16f, 0.13f, 0.27f, 0.98f);
            AddShadow(dimg, 6f, 0.4f);
            card.GetComponent<Button>().onClick.AddListener(onClick);

            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(14, 14, 16, 16); vlg.spacing = 8;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Ícone (ou amostra de cor se ainda não houver arte)
            if (it.icon != null)
            {
                var ic = UIFactory.CreateIcon(card.transform, "Icon", it.icon, 100);
                AddMinHeight(ic, 100);
            }
            else
            {
                var sw = UIFactory.CreatePanel(card.transform, "Swatch", it.uiColor);
                var sle = sw.gameObject.AddComponent<LayoutElement>();
                sle.minHeight = 100; sle.preferredHeight = 100;
            }

            var nm = UIFactory.CreateText(card.transform, "Name", it.itemName, 24, ColGold, TextAnchor.MiddleCenter);
            nm.fontStyle = FontStyle.Bold; NoWrap(nm); AddMinHeight(nm, 32);

            var ds = UIFactory.CreateText(card.transform, "Desc", it.description, 17, ColText, TextAnchor.UpperCenter);
            var dle = ds.gameObject.AddComponent<LayoutElement>();
            dle.minHeight = 70; dle.flexibleHeight = 1;
        }

        private void PickDraftItem(ItemData item)
        {
            ItemManager.Instance?.AddToInventory(item);
            draftPanel.SetActive(false);
            ShowMessage($"Você ganhou: {item.itemName}! Toque nele (à esquerda) e depois num gato.");
        }

        // =========================================================
        //  VITÓRIA / DERROTA
        // =========================================================
        public void ShowVictoryScreen()
        {
            if (endPanel == null) return;
            AchievementManager.Instance?.Report("wins", 1);
            int waves = WaveManager.Instance != null ? WaveManager.Instance.waves.Count : 10;
            int defeated = GameManager.Instance != null ? GameManager.Instance.EnemiesDefeated : 0;
            int coins = EconomyManager.Instance != null ? EconomyManager.Instance.TotalEarned : 0;
            endText.text =
                $"<size=78><b>VITÓRIA!</b></size>\n\n" +
                $"Ondas: {waves}/{waves}\n" +
                $"Inimigos derrotados: {defeated}\n" +
                $"Moedas ganhas: {coins}\n" +
                $"Sinergias ativadas: {everActivated.Count}" +
                RankingText();
            endText.color = ColGold;
            endPanel.SetActive(true);
        }

        public void ShowDefeatScreen()
        {
            if (endPanel == null) return;
            int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveIndex + 1 : 1;
            int total = WaveManager.Instance != null ? WaveManager.Instance.waves.Count : 10;
            int defeated = GameManager.Instance != null ? GameManager.Instance.EnemiesDefeated : 0;
            endText.text =
                $"<size=78><b>DERROTA</b></size>\n\n" +
                $"Você chegou até a onda {wave}/{total}\n" +
                $"Inimigos derrotados: {defeated}" +
                RankingText();
            endText.color = ColRed;
            endPanel.SetActive(true);
        }

        private string RankingText()
        {
            if (PlacementManager.Instance == null) return "";
            var list = new List<CatUnit>(PlacementManager.Instance.PlacedCats);
            list.RemoveAll(c => c == null);
            list.Sort((a, b) => b.DamageDealt.CompareTo(a.DamageDealt));
            int n = Mathf.Min(3, list.Count);
            if (n == 0) return "";
            var sb = new StringBuilder();
            sb.Append("\n\n<size=30><b>Top dano:</b></size>");
            for (int i = 0; i < n; i++)
                sb.Append($"\n{i + 1}. {list[i].Data.catName} — {list[i].DamageDealt:0}");
            return sb.ToString();
        }

        // =========================================================
        //  MENSAGENS (toast)
        // =========================================================
        public void ShowMessage(string msg)
        {
            if (messageText == null) return;
            messageText.text = msg;
            messageText.transform.parent.gameObject.SetActive(true);
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(HideMessageAfter(2.2f));
        }

        private IEnumerator HideMessageAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (messageText != null) messageText.transform.parent.gameObject.SetActive(false);
        }

        // =========================================================
        //  BANNER DE FIM DE ONDA
        // =========================================================
        public void ShowWaveSummary(int wave, int reward, int lives)
        {
            if (waveBanner == null) return;
            waveBannerText.text =
                $"<size=30><b>Onda {wave} concluída!</b></size>\n+{reward} moedas    •    Vidas: {lives}";
            waveBanner.SetActive(true);
            if (waveBannerRoutine != null) StopCoroutine(waveBannerRoutine);
            waveBannerRoutine = StartCoroutine(HideWaveBannerAfter(2.6f));
        }

        private IEnumerator HideWaveBannerAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (waveBanner != null) waveBanner.SetActive(false);
        }

        // =========================================================
        //  TOOLTIP DE SINERGIA
        // =========================================================
        private void ShowSynergyTooltip(SynergyData data)
        {
            if (data == null) return;
            var sb = new StringBuilder();
            sb.AppendLine($"<b>{DisplayName(data)}</b>");
            foreach (var tier in data.tiers)
                sb.AppendLine($"{tier.requiredCount}: {tier.description}");
            ShowTextTooltip(sb.ToString().TrimEnd());
        }

        private void ShowTextTooltip(string text)
        {
            if (tooltipPanel == null) return;
            tooltipText.text = text;

            // Mantém o tooltip dentro da área segura (não vaza da tela nem entra no notch).
            var rt = (RectTransform)tooltipPanel.transform;
            var canvas = tooltipPanel.GetComponentInParent<Canvas>();
            float sf = canvas != null ? canvas.scaleFactor : 1f;
            Vector2 size = rt.rect.size * sf; // pixels (pivot 1,0 = canto inferior direito)
            Rect sa = Screen.safeArea;
            const float m = 8f;
            Vector3 p = Input.mousePosition;
            p.x = Mathf.Clamp(p.x, sa.xMin + size.x + m, sa.xMax - m);
            p.y = Mathf.Clamp(p.y, sa.yMin + m, sa.yMax - size.y - m);
            rt.position = p;
            tooltipPanel.SetActive(true);
        }

        private static string ItemBonusText(ItemData it)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<b>{it.itemName}</b>");
            foreach (var e in it.statEffects) sb.AppendLine(StatLabel(e.stat, e.value));
            if (it.bonusTrueDamagePerHit > 0f) sb.AppendLine($"+{it.bonusTrueDamagePerHit:0} dano verdadeiro/ataque");
            if (it.grantsSlow) sb.AppendLine("Ataques deixam inimigos lentos");
            if (it.grantsArea) sb.AppendLine("Ataques causam dano em área");
            foreach (var s in it.grantedSynergies) sb.AppendLine($"Conta como {s}");
            return sb.ToString().TrimEnd();
        }

        private static string StatLabel(BonusStat s, float v)
        {
            switch (s)
            {
                case BonusStat.AttackSpeedPercent:   return $"+{v:0}% vel. ataque";
                case BonusStat.CritChancePercent:    return $"+{v:0}% crítico";
                case BonusStat.ArmorPenetrationFlat: return $"+{v:0} pen. armadura";
                case BonusStat.MagicPenetrationFlat: return $"+{v:0} pen. mágica";
                case BonusStat.DamagePercent:        return $"+{v:0}% dano";
                case BonusStat.RangePercent:         return $"+{v:0}% alcance";
                default: return "";
            }
        }

        private void HideTooltip()
        {
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> cb)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(cb);
            trigger.triggers.Add(entry);
        }

        private static string DisplayName(SynergyData d)
        {
            return string.IsNullOrEmpty(d.displayName) ? d.synergyType.ToString() : d.displayName;
        }

        private static Color RarityColor(int cost)
        {
            if (cost >= 4) return new Color(0.75f, 0.45f, 1f);   // épico (roxo)
            if (cost == 3) return new Color(0.40f, 0.70f, 1f);   // raro (azul)
            return new Color(0.70f, 0.80f, 0.75f);               // comum (cinza-esverdeado)
        }

        // ---- Lookup de sinergia (nome/cor) a partir do SynergyType ----
        private static SynergyData FindSynergy(SynergyType t)
        {
            var m = SynergyManager.Instance;
            if (m == null) return null;
            foreach (var s in m.allSynergies)
                if (s != null && s.synergyType == t) return s;
            return null;
        }

        private static string SynergyLabel(SynergyType t)
        {
            var d = FindSynergy(t);
            return (d != null && !string.IsNullOrEmpty(d.displayName)) ? d.displayName : t.ToString();
        }

        private static Color SynergyColor(SynergyType t)
        {
            var d = FindSynergy(t);
            return d != null ? d.uiColor : new Color(0.6f, 0.62f, 0.7f);
        }

        // =========================================================
        //  HELPERS
        // =========================================================
        private void ClearDynamic(Transform container, string prefixToRemove)
        {
            if (container == null) return;
            var toRemove = new List<GameObject>();
            foreach (Transform child in container)
                if (child.name.StartsWith(prefixToRemove)) toRemove.Add(child.gameObject);
            foreach (var go in toRemove) Destroy(go);
        }

        private static Color DamageColor(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return new Color(0.98f, 0.66f, 0.30f);
                case DamageType.Magical:  return new Color(0.66f, 0.50f, 0.98f);
                case DamageType.True:     return new Color(0.98f, 0.92f, 0.72f);
                default: return Color.gray;
            }
        }

        private static string DamageName(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return "Físico";
                case DamageType.Magical:  return "Mágico";
                case DamageType.True:     return "Verdadeiro";
                default: return "?";
            }
        }

        private static string SynergyNames(List<SynergyType> synergies)
        {
            if (synergies == null || synergies.Count == 0) return "-";
            return string.Join(" / ", synergies);
        }

        private static string ItemNames(List<ItemData> items)
        {
            var names = new List<string>();
            foreach (var it in items) names.Add(it.itemName);
            return string.Join(", ", names);
        }
    }
}
