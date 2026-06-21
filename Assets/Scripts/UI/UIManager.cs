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

        // ---- Paleta (mais contraste) ----
        private static readonly Color ColDark   = new Color(0.08f, 0.07f, 0.14f, 0.96f);
        private static readonly Color ColPanel  = new Color(0.15f, 0.12f, 0.25f, 0.97f);
        private static readonly Color ColCard   = new Color(0.27f, 0.23f, 0.42f, 1f);
        private static readonly Color ColGold   = new Color(1f, 0.86f, 0.35f);
        private static readonly Color ColText   = new Color(0.97f, 0.96f, 1f);
        private static readonly Color ColGreen  = new Color(0.32f, 0.80f, 0.42f);
        private static readonly Color ColRed    = new Color(0.92f, 0.38f, 0.40f);
        private static readonly Color ColBlue   = new Color(0.36f, 0.62f, 0.92f);
        private static readonly Color ColDim    = new Color(0.55f, 0.55f, 0.62f);

        // ---- Sprites de UI (ligados pelo MeowSetup) ----
        public Sprite panelSprite;
        public Sprite buttonSprite;
        public Sprite coinSprite;
        public Sprite heartSprite;

        // ---- Referências de runtime ----
        private Text waveText, waveProgressText, livesText, coinsText, messageText;
        private GameObject waveBarBg;
        private RectTransform waveBarFill;
        private Transform shopContainer, benchContainer, synergyContainer, itemsContainer;
        private Text benchLabel, startLabel;
        private Button startWaveButton, rerollButton;
        private Button pauseBtn, speed1Btn, speed2Btn;
        private UIPulse startPulse;

        private GameObject itemsPanel, bottomPanel;
        private GameObject nextWavePanel;
        private Text nextWaveText;
        private Button collapseBtn;
        private bool collapsed;

        private readonly HashSet<SynergyType> prevActiveSynergies = new HashSet<SynergyType>();
        private readonly HashSet<SynergyType> everActivated = new HashSet<SynergyType>();

        private GameObject waveBanner, tooltipPanel;
        private Text waveBannerText, tooltipText;
        private Coroutine waveBannerRoutine;

        private GameObject detailPanel;
        private Image detailIcon;
        private Text detailNameText, detailText;
        private Button detailSellBtn, detailReturnBtn;
        private CatUnit detailCat;

        private GameObject draftPanel;
        private Transform draftContainer;

        private GameObject endPanel;
        private Text endText;

        private GameObject mainMenuPanel, pausePanel, settingsPanel;
        private float musicVolume = 0.6f;

        private GameObject tutorialPanel, mapSelectPanel;
        private Text tutorialText;
        private int tutorialStep;
        private Transform detailItemsRow;
        private static readonly string[] TutorialSteps =
        {
            "1/6 — Compre um gato na LOJA (embaixo). Ele vai para o BANCO.",
            "2/6 — Clique num gato do BANCO e toque no gramado para posicioná-lo.",
            "3/6 — Clique em INICIAR ONDA (canto direito) para começar a luta.",
            "4/6 — Derrote inimigos para ganhar moedas e comprar mais gatos.",
            "5/6 — Junte gatos do mesmo tipo para ativar SINERGIAS (painel à direita).",
            "6/6 — A cada 3 ondas você escolhe um ITEM — equipe-o tocando num gato!"
        };

        private Coroutine messageRoutine;
        private int gameSpeed = 1;

        private void Awake() => Instance = this;

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

            BuildTopBar(root);
            BuildItemsPanel(root);
            BuildSynergyPanel(root);
            BuildBottomArea(root);
            BuildDetailPanel(root);
            BuildDraftPanel(root);
            BuildEndPanel(root);
            BuildMessage(root);
            BuildWaveBanner(root);
            BuildTooltip(root);
            BuildMainMenu(root);
            BuildPauseMenu(root);
            BuildSettings(root);
            BuildMapSelect(root);
            BuildTutorial(root);
        }

        // =========================================================
        //  SELEÇÃO DE MAPA
        // =========================================================
        private void BuildMapSelect(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "MapSelect", new Color(0.06f, 0.05f, 0.12f, 0.98f));
            mapSelectPanel = panel.gameObject;
            UIFactory.StretchFull(panel.rectTransform);

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

            MapCard(row.transform, "bosque", "Bosque Fantasma", "Fácil", "Mapa noturno com dois caminhos.", true);
            MapCard(row.transform, "jardim", "Jardim Místico", "Médio", "Em breve.", false);
            MapCard(row.transform, "ruinas", "Ruínas Lunares", "Difícil", "Em breve.", false);

            var back = UIFactory.CreateButton(panel.transform, "Back", "Voltar", ColGreen, HideMapSelect, 26, buttonSprite);
            back.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            var brt = UIFactory.AsRect(back);
            UIFactory.SetAnchors(brt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            brt.sizeDelta = new Vector2(260, 64); brt.anchoredPosition = new Vector2(0, 60);

            mapSelectPanel.SetActive(false);
        }

        private void MapCard(Transform parent, string mapId, string name, string difficulty, string desc, bool playable)
        {
            var card = UIFactory.CreatePanel(parent, "MapCard", new Color(0.16f, 0.13f, 0.27f, 0.98f));
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
            GameManager.LoadOnStart = true;
            GameManager.Instance?.NewGame();
        }

        // =========================================================
        //  TUTORIAL (passos guiados, pulável)
        // =========================================================
        private void BuildTutorial(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "Tutorial", new Color(0.09f, 0.07f, 0.17f, 0.97f));
            tutorialPanel = panel.gameObject;
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
            var panel = UIFactory.CreatePanel(root, "MainMenu", new Color(0.06f, 0.05f, 0.12f, 0.97f));
            mainMenuPanel = panel.gameObject;
            UIFactory.StretchFull(panel.rectTransform);

            var title = UIFactory.CreateText(panel.transform, "Title", "MEOW TACTICS TD", 72, ColGold, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold; NoWrap(title); AddOutline(title);
            var trt = title.rectTransform;
            UIFactory.SetAnchors(trt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            trt.sizeDelta = new Vector2(1200, 100); trt.anchoredPosition = new Vector2(0, -170);

            var col = MenuColumn(panel.transform, 460);
            MenuButton(col, "Novo Jogo", ColGreen, () => GameManager.Instance?.NewGame());
            bool hasSave = SaveSystem.HasSave();
            var cont = MenuButton(col, "Continuar", hasSave ? ColGreen : ColCard, ContinueGame);
            cont.interactable = hasSave;
            MenuButton(col, "Escolher Mapa", ColBlue, ShowMapSelect);
            MenuButton(col, "Configurações", ColBlue, ShowSettings);
            MenuButton(col, "Sair", ColRed, () => Application.Quit());

            mainMenuPanel.SetActive(false);
        }

        private void BuildPauseMenu(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "PauseMenu", new Color(0f, 0f, 0f, 0.84f));
            pausePanel = panel.gameObject;
            UIFactory.StretchFull(panel.rectTransform);

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
            var panel = UIFactory.CreatePanel(root, "Settings", new Color(0.06f, 0.05f, 0.12f, 0.98f));
            settingsPanel = panel.gameObject;
            UIFactory.StretchFull(panel.rectTransform);

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

            MenuButton(col, "Resetar Progresso", ColRed, () => { PlayerPrefs.DeleteAll(); ShowMessage("Progresso resetado."); });
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
            var b = UIFactory.CreateButton(parent, "MenuBtn", label, color, onClick, 28, buttonSprite);
            b.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 66; le.preferredHeight = 66;
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
        public void ShowSettings() { if (settingsPanel != null) settingsPanel.SetActive(true); SFXManager.Play(SfxType.Click); }
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
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // ---------- Top bar ----------
        private void BuildTopBar(Transform root)
        {
            var bar = UIFactory.CreatePanel(root, "TopBar", ColDark);
            var rt = bar.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            rt.sizeDelta = new Vector2(0, 100);
            rt.anchoredPosition = Vector2.zero;

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
            var barBg = UIFactory.CreatePanel(bar.transform, "WaveBarBg", new Color(0f, 0f, 0f, 0.55f));
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
            var go = new GameObject("Stat", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter; h.spacing = 6;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            var le = go.AddComponent<LayoutElement>(); le.minWidth = 130;

            if (icon != null)
            {
                var img = UIFactory.CreateIcon(go.transform, "Icon", icon, 46);
                var ile = img.gameObject.AddComponent<LayoutElement>();
                ile.minWidth = 46; ile.preferredWidth = 46; ile.minHeight = 46; ile.preferredHeight = 46;
            }
            var txt = UIFactory.CreateText(go.transform, "Val", initial, 30, color, TextAnchor.MiddleLeft);
            txt.fontStyle = FontStyle.Bold;
            NoWrap(txt);
            var tle = txt.gameObject.AddComponent<LayoutElement>(); tle.minWidth = 70;
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
            var panel = UIFactory.CreatePanel(root, "SynergyPanel", ColPanel);
            var rt = panel.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            rt.sizeDelta = new Vector2(300, 470);
            rt.anchoredPosition = new Vector2(-12, -112);

            Title(panel.transform, "SINERGIAS");

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            var crt = content.GetComponent<RectTransform>();
            UIFactory.StretchFull(crt, 12f);
            crt.offsetMax = new Vector2(crt.offsetMax.x, -46);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            vlg.spacing = 5; vlg.childAlignment = TextAnchor.UpperLeft;
            synergyContainer = content.transform;
        }

        // ---------- Área inferior: loja + banco (recolhível) + INICIAR ----------
        private void BuildBottomArea(Transform root)
        {
            // Botão grande INICIAR ONDA (sempre visível, canto inferior direito)
            startWaveButton = UIFactory.CreateButton(root, "StartWave", "INICIAR\nONDA  ▶", ColGreen,
                () => GameManager.Instance?.StartWave(), 30, buttonSprite);
            startLabel = startWaveButton.GetComponentInChildren<Text>();
            startLabel.fontStyle = FontStyle.Bold;
            AddOutline(startLabel);
            var srt = UIFactory.AsRect(startWaveButton);
            UIFactory.SetAnchors(srt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
            srt.sizeDelta = new Vector2(300, 170);
            srt.anchoredPosition = new Vector2(-20, 20);
            startPulse = startWaveButton.gameObject.AddComponent<UIPulse>();
            startPulse.speed = 3.2f; startPulse.amount = 0.05f;

            // Preview da próxima onda (acima do botão Iniciar)
            var nwp = UIFactory.CreatePanel(root, "NextWavePanel", new Color(0.10f, 0.08f, 0.18f, 0.92f));
            nextWavePanel = nwp.gameObject;
            var nwrt = nwp.rectTransform;
            UIFactory.SetAnchors(nwrt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
            nwrt.sizeDelta = new Vector2(290, 150);
            nwrt.anchoredPosition = new Vector2(-20, 200);
            var nwTitle = UIFactory.CreateText(nwp.transform, "Title", "PRÓXIMA ONDA", 18, ColGold, TextAnchor.UpperCenter);
            nwTitle.fontStyle = FontStyle.Bold; NoWrap(nwTitle);
            var ntrt = nwTitle.rectTransform;
            UIFactory.SetAnchors(ntrt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            ntrt.sizeDelta = new Vector2(-12, 26); ntrt.anchoredPosition = new Vector2(0, -6);
            nextWaveText = UIFactory.CreateText(nwp.transform, "Text", "", 16, ColText, TextAnchor.UpperLeft);
            var nxrt = nextWaveText.rectTransform;
            UIFactory.SetAnchors(nxrt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f));
            nxrt.offsetMin = new Vector2(14, 10); nxrt.offsetMax = new Vector2(-14, -34);

            // Aba clara de recolher/abrir a loja (canto inferior esquerdo, como uma aba do painel)
            collapseBtn = UIFactory.CreateButton(root, "Collapse", "LOJA  ▼", ColGreen,
                ToggleCollapse, 20, buttonSprite);
            collapseBtn.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            var crt2 = UIFactory.AsRect(collapseBtn);
            UIFactory.SetAnchors(crt2, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
            crt2.sizeDelta = new Vector2(160, 48);
            crt2.anchoredPosition = new Vector2(16, 320);

            // Painel recolhível (loja em cima, banco embaixo)
            var panelImg = UIFactory.CreatePanel(root, "BottomPanel", ColDark);
            bottomPanel = panelImg.gameObject;
            var brt = panelImg.rectTransform;
            UIFactory.SetAnchors(brt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
            brt.sizeDelta = new Vector2(1560, 300);
            brt.anchoredPosition = new Vector2(20, 14);

            // Linha da LOJA
            var shopRow = Row(bottomPanel.transform, "ShopRow", 168, -8);
            Label(shopRow, "LOJA", ColGold, 70);
            rerollButton = UIFactory.CreateButton(shopRow, "Reroll", "Atualizar\n(2 moedas)", ColBlue,
                () => ShopManager.Instance?.RerollShop(), 17, buttonSprite);
            AddWidth(rerollButton, 130);
            shopContainer = shopRow;

            // Linha do BANCO (chips com largura fixa, alinhados à esquerda)
            var benchRow = Row(bottomPanel.transform, "BenchRow", 108, 10);
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

        // ---------- Painéis modais ----------
        private void BuildDetailPanel(Transform root)
        {
            detailPanel = UIFactory.CreatePanel(root, "DetailPanel", ColPanel, panelSprite).gameObject;
            var rt = (RectTransform)detailPanel.transform;
            UIFactory.SetAnchors(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(580, 600);

            // Retrato do gato (topo)
            detailIcon = UIFactory.CreateIcon(detailPanel.transform, "Portrait", null, 110);
            detailIcon.enabled = false;
            var irt = detailIcon.rectTransform;
            UIFactory.SetAnchors(irt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            irt.anchoredPosition = new Vector2(0, -54);

            // Nome
            detailNameText = UIFactory.CreateText(detailPanel.transform, "Name", "", 28, ColGold, TextAnchor.MiddleCenter);
            detailNameText.fontStyle = FontStyle.Bold; NoWrap(detailNameText); AddOutline(detailNameText);
            var nrt = detailNameText.rectTransform;
            UIFactory.SetAnchors(nrt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            nrt.sizeDelta = new Vector2(480, 40);
            nrt.anchoredPosition = new Vector2(0, -176);

            // Estatísticas
            detailText = UIFactory.CreateText(detailPanel.transform, "Info", "", 20, ColText, TextAnchor.UpperLeft);
            var drt = detailText.rectTransform;
            UIFactory.SetAnchors(drt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            drt.sizeDelta = new Vector2(-110, 190);
            drt.anchoredPosition = new Vector2(0, -210);

            // Fileira de ícones dos itens equipados
            var itemsRowGo = new GameObject("DetailItems", typeof(RectTransform));
            itemsRowGo.transform.SetParent(detailPanel.transform, false);
            var irrt = itemsRowGo.GetComponent<RectTransform>();
            UIFactory.SetAnchors(irrt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            irrt.sizeDelta = new Vector2(-120, 46);
            irrt.anchoredPosition = new Vector2(0, -414);
            var ihlg = itemsRowGo.AddComponent<HorizontalLayoutGroup>();
            ihlg.spacing = 8; ihlg.childAlignment = TextAnchor.MiddleCenter;
            ihlg.childForceExpandWidth = false; ihlg.childForceExpandHeight = false;
            detailItemsRow = itemsRowGo.transform;

            var btnRow = new GameObject("Buttons", typeof(RectTransform));
            btnRow.transform.SetParent(detailPanel.transform, false);
            var brt = btnRow.GetComponent<RectTransform>();
            UIFactory.SetAnchors(brt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            brt.sizeDelta = new Vector2(-90, 84);
            brt.anchoredPosition = new Vector2(0, 38);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

            detailSellBtn = UIFactory.CreateButton(btnRow.transform, "Sell", "Vender", ColRed,
                () => { if (detailCat != null) PlacementManager.Instance?.SellCat(detailCat); }, 20, buttonSprite);
            detailReturnBtn = UIFactory.CreateButton(btnRow.transform, "Return", "Voltar p/ banco", ColBlue,
                () => { if (detailCat != null) PlacementManager.Instance?.ReturnCatToBench(detailCat); }, 18, buttonSprite);
            UIFactory.CreateButton(btnRow.transform, "Close", "Fechar", ColCard,
                () => PlacementManager.Instance?.ClearFocus(), 20, buttonSprite);

            detailPanel.SetActive(false);
        }

        private void BuildDraftPanel(Transform root)
        {
            draftPanel = UIFactory.CreatePanel(root, "DraftPanel", new Color(0, 0, 0, 0.85f)).gameObject;
            UIFactory.StretchFull((RectTransform)draftPanel.transform);

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
            endPanel = UIFactory.CreatePanel(root, "EndPanel", new Color(0, 0, 0, 0.9f)).gameObject;
            UIFactory.StretchFull((RectTransform)endPanel.transform);

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
        }

        private void OnStateChanged(GameState state)
        {
            bool prep = state == GameState.Preparation;
            if (startWaveButton != null) startWaveButton.interactable = prep;
            if (rerollButton != null) rerollButton.interactable = prep;
            if (startLabel != null) startLabel.text = prep ? "INICIAR\nONDA  ▶" : "ONDA EM\nANDAMENTO";
            if (startPulse != null) startPulse.active = prep;
            if (waveProgressText != null && prep) waveProgressText.text = "Preparação — posicione seus gatos";
            if (nextWavePanel != null) nextWavePanel.SetActive(prep);
            if (prep) UpdateNextWavePreview();

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
            collapsed = !collapsed;
            if (bottomPanel != null) bottomPanel.SetActive(!collapsed);
            var lbl = collapseBtn != null ? collapseBtn.GetComponentInChildren<Text>() : null;
            if (lbl != null) lbl.text = collapsed ? "LOJA  ▲" : "LOJA  ▼";
            SFXManager.Play(SfxType.Click);
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
            if (nextWaveText == null || WaveManager.Instance == null) return;
            var w = WaveManager.Instance.CurrentWave;
            if (w == null) { nextWaveText.text = ""; return; }

            var counts = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (var info in w.enemies)
            {
                if (info.enemy == null) continue;
                string nm = info.enemy.enemyName;
                if (!counts.ContainsKey(nm)) { counts[nm] = 0; order.Add(nm); }
                counts[nm] += info.count;
            }
            var sb = new StringBuilder();
            foreach (var nm in order) sb.AppendLine($"{counts[nm]}x {nm}");
            if (w.isBossWave) sb.Append("<color=#ff7777><b>★ BOSS!</b></color>");
            nextWaveText.text = sb.ToString().TrimEnd();
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
            var img = card.GetComponent<Image>();
            img.color = cat == null
                ? new Color(0.15f, 0.15f, 0.18f, 0.9f)
                : (afford ? ColCard : new Color(0.13f, 0.11f, 0.17f, 0.95f));
            var btn = card.GetComponent<Button>();
            btn.interactable = cat != null; // mesmo sem moeda, deixa clicar p/ mostrar aviso
            if (cat != null && onClick != null) btn.onClick.AddListener(onClick);

            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(5, 5, 4, 4); vlg.spacing = 0;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            if (cat == null)
            {
                var dash = UIFactory.CreateText(card.transform, "Dash", "—", 26, ColDim);
                AddMinHeight(dash, 80);
                return;
            }

            // Faixa de raridade (topo do card)
            Color rar = RarityColor(cat.cost);
            var strip = UIFactory.CreatePanel(card.transform, "Rarity", rar);
            var sle = strip.gameObject.AddComponent<LayoutElement>();
            sle.minHeight = 4; sle.preferredHeight = 4;

            // Ícone grande do gato (esmaecido se não puder comprar)
            if (cat.icon != null)
            {
                var icon = UIFactory.CreateIcon(card.transform, "Icon", cat.icon, 80);
                icon.color = afford ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.85f);
                AddMinHeight(icon, 80);
            }
            // Nome (cor por raridade)
            var name = UIFactory.CreateText(card.transform, "Name", cat.catName, 18, rar, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Bold; NoWrap(name); AddMinHeight(name, 22);

            // Linha de custo: [moeda] custo  •  tipo
            var costRow = new GameObject("CostRow", typeof(RectTransform));
            costRow.transform.SetParent(card.transform, false);
            var crl = costRow.AddComponent<HorizontalLayoutGroup>();
            crl.childAlignment = TextAnchor.MiddleCenter; crl.spacing = 4;
            crl.childForceExpandWidth = false; crl.childForceExpandHeight = false;
            var crle = costRow.AddComponent<LayoutElement>(); crle.minHeight = 22; crle.preferredHeight = 22;
            if (coinSprite != null)
            {
                var ci = UIFactory.CreateIcon(costRow.transform, "Coin", coinSprite, 20);
                var cile = ci.gameObject.AddComponent<LayoutElement>();
                cile.minWidth = 20; cile.preferredWidth = 20; cile.minHeight = 20; cile.preferredHeight = 20;
            }
            var costTxt = UIFactory.CreateText(costRow.transform, "Cost",
                $"{cat.cost}   {DamageName(cat.damageType)}", 15, ColGold, TextAnchor.MiddleLeft);
            costTxt.fontStyle = FontStyle.Bold; NoWrap(costTxt);

            // Sinergias
            var syn = UIFactory.CreateText(card.transform, "Syn", SynergyNames(cat.synergies), 13, DamageColor(cat.damageType), TextAnchor.MiddleCenter);
            NoWrap(syn); AddMinHeight(syn, 16);
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
                chip.GetComponent<Image>().color = ColCard;
                chip.GetComponent<Button>().onClick.AddListener(() => PlacementManager.Instance?.SelectBenchCatForPlacement(c));
                var chipLe = chip.AddComponent<LayoutElement>();
                chipLe.minWidth = 170; chipLe.preferredWidth = 190; chipLe.flexibleWidth = 0;

                var hlg = chip.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(6, 6, 4, 4); hlg.spacing = 6;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

                if (c.Data.icon != null)
                {
                    var icon = UIFactory.CreateIcon(chip.transform, "Icon", c.Data.icon, 64);
                    var ile = icon.gameObject.AddComponent<LayoutElement>();
                    ile.minWidth = 64; ile.minHeight = 64; ile.preferredWidth = 64; ile.preferredHeight = 64;
                }
                string txt = c.Data.catName + (c.Items.Count > 0 ? $"\n[{c.Items.Count} itens]" : "");
                UIFactory.CreateText(chip.transform, "Name", txt, 14, ColText, TextAnchor.MiddleLeft);
            }
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
                    }
                }

                int next = NextThreshold(s.data, s.count);
                Color col = s.IsActive ? Color.Lerp(s.data.uiColor, ColGold, 0.35f) : ColDim;
                string mark = s.IsActive ? "  ★" : "";
                var txt = UIFactory.CreateText(synergyContainer, "SynRow",
                    $"•  {DisplayName(s)}   {s.count}/{next}{mark}", 21, col, TextAnchor.MiddleLeft);
                if (s.IsActive) { txt.fontStyle = FontStyle.Bold; AddOutline(txt); }
                NoWrap(txt);
                var le = txt.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 28;

                // Bônus resumido logo abaixo (só quando a sinergia está ativa).
                if (s.IsActive && s.activeTier != null && !string.IsNullOrEmpty(s.activeTier.description))
                {
                    var bonus = UIFactory.CreateText(synergyContainer, "SynRow",
                        "   " + s.activeTier.description, 14, new Color(0.86f, 0.86f, 0.62f), TextAnchor.UpperLeft);
                    var ble = bonus.gameObject.AddComponent<LayoutElement>();
                    ble.minHeight = 20; // pode crescer se quebrar em 2 linhas
                }

                // Tooltip ao passar/tocar (mostra os bônus dos níveis).
                txt.raycastTarget = true;
                SynergyData data = s.data;
                var trigger = txt.gameObject.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerEnter, _ => ShowSynergyTooltip(data));
                AddTrigger(trigger, EventTriggerType.PointerClick, _ => ShowSynergyTooltip(data));
                AddTrigger(trigger, EventTriggerType.PointerExit, _ => HideTooltip());
            }

            prevActiveSynergies.Clear();
            foreach (var t in nowActive) prevActiveSynergies.Add(t);

            if (!any)
            {
                var hint = UIFactory.CreateText(synergyContainer, "SynRow",
                    "Posicione gatos para\nativar sinergias!", 16, ColDim, TextAnchor.UpperLeft);
                var le = hint.gameObject.AddComponent<LayoutElement>(); le.minHeight = 50;
            }
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
                AddTrigger(trig, EventTriggerType.PointerEnter, _ => ShowTextTooltip(ItemBonusText(it)));
                AddTrigger(trig, EventTriggerType.PointerExit, _ => HideTooltip());
            }
        }

        private void ToggleItemSelection(ItemData item)
        {
            if (ItemManager.Instance.SelectedForEquip == item)
            {
                ItemManager.Instance.ClearSelection();
                ShowMessage("Item deselecionado.");
            }
            else
            {
                ItemManager.Instance.SelectForEquip(item);
                ShowMessage("Agora toque em um gato no mapa para equipar!");
            }
        }

        // =========================================================
        //  DETALHE DO GATO
        // =========================================================
        public void ShowCatDetail(CatUnit cat)
        {
            if (cat == null) return;
            detailCat = cat;

            if (detailIcon != null)
            {
                detailIcon.sprite = cat.Data.icon;
                detailIcon.enabled = cat.Data.icon != null;
            }
            if (detailNameText != null) detailNameText.text = cat.Data.catName;

            var sb = new StringBuilder();
            sb.AppendLine($"<i>{cat.Data.description}</i>");
            sb.AppendLine();
            sb.AppendLine($"<b>Dano:</b> {cat.CurrentDamage:0.#}  ({DamageName(cat.Data.damageType)})");
            sb.AppendLine($"<b>Vel. ataque:</b> {cat.CurrentAttackSpeed:0.##}/s");
            sb.AppendLine($"<b>Alcance:</b> {cat.CurrentRange:0.#}");
            sb.AppendLine($"<b>Crítico:</b> {cat.CurrentCritChance:0}%");
            sb.AppendLine($"<b>Sinergias:</b> {SynergyNames(new List<SynergyType>(cat.GetEffectiveSynergies()))}");
            sb.Append($"<b>Itens:</b> ");
            sb.Append(cat.Items.Count == 0 ? "nenhum" : ItemNames(cat.Items));

            detailText.text = sb.ToString();

            if (detailItemsRow != null)
            {
                foreach (Transform c in detailItemsRow) Destroy(c.gameObject);
                foreach (var it in cat.Items)
                {
                    LayoutElement le;
                    if (it.icon != null)
                    {
                        var ic = UIFactory.CreateIcon(detailItemsRow, "Icon", it.icon, 42);
                        le = ic.gameObject.AddComponent<LayoutElement>();
                    }
                    else
                    {
                        var sw = UIFactory.CreatePanel(detailItemsRow, "Sw", it.uiColor);
                        le = sw.gameObject.AddComponent<LayoutElement>();
                    }
                    le.minWidth = 42; le.preferredWidth = 42; le.minHeight = 42; le.preferredHeight = 42;
                }
            }

            detailReturnBtn.interactable = cat.IsPlaced;
            detailPanel.SetActive(true);
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
            card.GetComponent<Image>().color = new Color(0.16f, 0.13f, 0.27f, 0.98f);
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
            tooltipPanel.transform.position = Input.mousePosition;
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
