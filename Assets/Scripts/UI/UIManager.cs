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
        private Button collapseBtn;
        private bool collapsed;

        private readonly HashSet<SynergyType> prevActiveSynergies = new HashSet<SynergyType>();
        private readonly HashSet<SynergyType> everActivated = new HashSet<SynergyType>();

        private GameObject waveBanner, tooltipPanel;
        private Text waveBannerText, tooltipText;
        private Coroutine waveBannerRoutine;

        private GameObject detailPanel;
        private Text detailText;
        private Button detailSellBtn, detailReturnBtn;
        private CatUnit detailCat;

        private GameObject draftPanel;
        private Transform draftContainer;

        private GameObject endPanel;
        private Text endText;

        private Coroutine messageRoutine;
        private int gameSpeed = 1;

        private void Awake() => Instance = this;

        private void Start()
        {
            BuildUI();
            Subscribe();
            RefreshAll();
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

            pauseBtn  = SmallButton(rightGo.transform, "Pause", "II", ColBlue, TogglePause);
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
            rt.sizeDelta = new Vector2(280, 470);
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
            rt.sizeDelta = new Vector2(560, 440);

            detailText = UIFactory.CreateText(detailPanel.transform, "Info", "", 22, ColText, TextAnchor.UpperLeft);
            var drt = detailText.rectTransform;
            UIFactory.SetAnchors(drt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            drt.sizeDelta = new Vector2(-50, 300);
            drt.anchoredPosition = new Vector2(0, -24);

            var btnRow = new GameObject("Buttons", typeof(RectTransform));
            btnRow.transform.SetParent(detailPanel.transform, false);
            var brt = btnRow.GetComponent<RectTransform>();
            UIFactory.SetAnchors(brt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            brt.sizeDelta = new Vector2(-40, 110);
            brt.anchoredPosition = new Vector2(0, 24);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

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

            var t = UIFactory.CreateText(draftPanel.transform, "Title", "ESCOLHA UM ITEM!", 42, ColGold, TextAnchor.UpperCenter);
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.anchoredPosition = new Vector2(0, -120);

            var row = new GameObject("Choices", typeof(RectTransform));
            row.transform.SetParent(draftPanel.transform, false);
            var rrt = row.GetComponent<RectTransform>();
            UIFactory.SetAnchors(rrt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rrt.sizeDelta = new Vector2(1100, 320);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            draftContainer = row.transform;

            draftPanel.SetActive(false);
        }

        private void BuildEndPanel(Transform root)
        {
            endPanel = UIFactory.CreatePanel(root, "EndPanel", new Color(0, 0, 0, 0.9f)).gameObject;
            UIFactory.StretchFull((RectTransform)endPanel.transform);

            endText = UIFactory.CreateText(endPanel.transform, "EndText", "", 40, ColGold, TextAnchor.MiddleCenter);
            endText.fontStyle = FontStyle.Bold;
            var ert = endText.rectTransform;
            UIFactory.SetAnchors(ert, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            ert.sizeDelta = new Vector2(1200, 400);
            ert.anchoredPosition = new Vector2(0, 80);

            var btn = UIFactory.CreateButton(endPanel.transform, "Restart", "Jogar Novamente", ColGreen,
                () => GameManager.Instance?.Restart(), 28, buttonSprite);
            var rt = UIFactory.AsRect(btn);
            UIFactory.SetAnchors(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(360, 120);
            rt.anchoredPosition = new Vector2(0, -160);

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
                WaveManager.Instance.OnWaveChanged += (_, __) => UpdateWave();
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
            UpdateSpeedButtons();
        }

        private void OnStateChanged(GameState state)
        {
            bool prep = state == GameState.Preparation;
            if (startWaveButton != null) startWaveButton.interactable = prep;
            if (rerollButton != null) rerollButton.interactable = prep;
            if (startLabel != null) startLabel.text = prep ? "INICIAR\nONDA  ▶" : "ONDA EM\nANDAMENTO";
            if (startPulse != null) startPulse.active = prep;
            if (waveProgressText != null && prep) waveProgressText.text = "Preparação — posicione seus gatos";

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

        private void TogglePause()
        {
            Time.timeScale = Time.timeScale > 0f ? 0f : gameSpeed;
            UpdateSpeedButtons();
            SFXManager.Play(SfxType.Click);
        }

        private void UpdateSpeedButtons()
        {
            bool paused = Time.timeScale == 0f;
            Tint(pauseBtn, paused ? ColGold : ColBlue);
            Tint(speed1Btn, (!paused && gameSpeed == 1) ? ColGreen : ColCard);
            Tint(speed2Btn, (!paused && gameSpeed == 2) ? ColGreen : ColCard);
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
                        "     " + s.activeTier.description, 14, new Color(0.86f, 0.86f, 0.62f), TextAnchor.MiddleLeft);
                    NoWrap(bonus);
                    var ble = bonus.gameObject.AddComponent<LayoutElement>();
                    ble.minHeight = 20;
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
                Color color = it.uiColor * (selected ? 1.2f : 0.7f); color.a = 1f;
                var btn = UIFactory.CreateButton(itemsContainer, "ItemBtn",
                    (selected ? "▶ " : "") + it.itemName, color, () => ToggleItemSelection(it), 14);
                var le = btn.gameObject.AddComponent<LayoutElement>(); le.minHeight = 54;
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

            var sb = new StringBuilder();
            sb.AppendLine($"<b>{cat.Data.catName}</b>");
            sb.AppendLine(cat.Data.description);
            sb.AppendLine();
            sb.AppendLine($"Dano: {cat.CurrentDamage:0.#} ({DamageName(cat.Data.damageType)})");
            sb.AppendLine($"Vel. ataque: {cat.CurrentAttackSpeed:0.##}/s");
            sb.AppendLine($"Alcance: {cat.CurrentRange:0.#}");
            sb.AppendLine($"Crítico: {cat.CurrentCritChance:0}%");
            sb.AppendLine($"Sinergias: {SynergyNames(new List<SynergyType>(cat.GetEffectiveSynergies()))}");
            sb.Append("Itens: ");
            sb.AppendLine(cat.Items.Count == 0 ? "nenhum" : ItemNames(cat.Items));

            detailText.text = sb.ToString();
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
                string label = $"<b>{it.itemName}</b>\n\n{it.description}";
                var color = it.uiColor * 0.85f; color.a = 1f;
                UIFactory.CreateButton(draftContainer, "DraftBtn", label, color, () => PickDraftItem(it), 18);
            }
            draftPanel.SetActive(true);
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
                $"Sinergias ativadas: {everActivated.Count}";
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
                $"Inimigos derrotados: {defeated}\n\n" +
                $"Tente combinar mais sinergias!";
            endText.color = ColRed;
            endPanel.SetActive(true);
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
            if (tooltipPanel == null || data == null) return;
            var sb = new StringBuilder();
            sb.AppendLine($"<b>{DisplayName(data)}</b>");
            foreach (var tier in data.tiers)
                sb.AppendLine($"{tier.requiredCount}: {tier.description}");
            tooltipText.text = sb.ToString().TrimEnd();
            tooltipPanel.transform.position = Input.mousePosition;
            tooltipPanel.SetActive(true);
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
