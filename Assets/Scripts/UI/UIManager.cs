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
        private static readonly Color ColCard   = new Color(0.20f, 0.17f, 0.32f, 1f);
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
        private Transform shopContainer, benchContainer, synergyContainer, itemsContainer;
        private Text benchLabel, startLabel;
        private Button startWaveButton, rerollButton;
        private Button pauseBtn, speed1Btn, speed2Btn;
        private UIPulse startPulse;

        private GameObject itemsPanel, bottomPanel;
        private Button collapseBtn;
        private bool collapsed;

        private readonly HashSet<SynergyType> prevActiveSynergies = new HashSet<SynergyType>();

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
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // ---------- Top bar ----------
        private void BuildTopBar(Transform root)
        {
            var bar = UIFactory.CreatePanel(root, "TopBar", ColDark, panelSprite);
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

            waveProgressText = UIFactory.CreateText(bar.transform, "WaveProg", "Preparação", 17, ColDim, TextAnchor.MiddleCenter);
            NoWrap(waveProgressText);
            var prt = waveProgressText.rectTransform;
            UIFactory.SetAnchors(prt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            prt.sizeDelta = new Vector2(440, 26);
            prt.anchoredPosition = new Vector2(0, -24);

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
            var panel = UIFactory.CreatePanel(root, "ItemsPanel", ColPanel, panelSprite);
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
            var panel = UIFactory.CreatePanel(root, "SynergyPanel", ColPanel, panelSprite);
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

            // Aba de recolher loja/banco (central, na borda de cima do painel)
            collapseBtn = UIFactory.CreateButton(root, "Collapse", "▼ Recolher loja", ColCard,
                ToggleCollapse, 16, buttonSprite);
            collapseBtn.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            var crt2 = UIFactory.AsRect(collapseBtn);
            UIFactory.SetAnchors(crt2, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            crt2.sizeDelta = new Vector2(210, 40);
            crt2.anchoredPosition = new Vector2(0, 300);

            // Painel recolhível (loja em cima, banco embaixo)
            var panelImg = UIFactory.CreatePanel(root, "BottomPanel", ColDark, panelSprite);
            bottomPanel = panelImg.gameObject;
            var brt = panelImg.rectTransform;
            UIFactory.SetAnchors(brt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
            brt.sizeDelta = new Vector2(1560, 280);
            brt.anchoredPosition = new Vector2(20, 14);

            // Linha da LOJA
            var shopRow = Row(bottomPanel.transform, "ShopRow", 150, -8);
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

            endText = UIFactory.CreateText(endPanel.transform, "EndText", "", 60, ColGold, TextAnchor.MiddleCenter);
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
        }

        private void TogglePause()
        {
            Time.timeScale = Time.timeScale > 0f ? 0f : gameSpeed;
            UpdateSpeedButtons();
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
            if (lbl != null) lbl.text = collapsed ? "▲ Abrir loja" : "▼ Recolher loja";
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
            waveProgressText.text = wm.IsWaveRunning
                ? $"Inimigos restantes: {wm.EnemiesRemaining} / {wm.EnemiesTotal}"
                : "Preparação — posicione seus gatos";
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
            vlg.padding = new RectOffset(6, 6, 6, 6); vlg.spacing = 1;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            if (cat == null)
            {
                var dash = UIFactory.CreateText(card.transform, "Dash", "—", 26, ColDim);
                AddMinHeight(dash, 80);
                return;
            }

            // Ícone do gato (esmaecido se não puder comprar)
            if (cat.icon != null)
            {
                var icon = UIFactory.CreateIcon(card.transform, "Icon", cat.icon, 80);
                icon.color = afford ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.85f);
                AddMinHeight(icon, 80);
            }
            // Nome
            var name = UIFactory.CreateText(card.transform, "Name", cat.catName, 17, ColText, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Bold; NoWrap(name); AddMinHeight(name, 22);
            // Custo + tipo
            var cost = UIFactory.CreateText(card.transform, "Cost",
                $"Custo {cat.cost}  •  {DamageName(cat.damageType)}", 13, ColGold, TextAnchor.MiddleCenter);
            NoWrap(cost); AddMinHeight(cost, 18);
            // Sinergias
            var syn = UIFactory.CreateText(card.transform, "Syn", SynergyNames(cat.synergies), 12, DamageColor(cat.damageType), TextAnchor.MiddleCenter);
            NoWrap(syn); AddMinHeight(syn, 18);
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
                        ShowMessage($"SINERGIA ATIVADA: {DisplayName(s)}!   {s.activeTier.description}");
                }

                int next = NextThreshold(s.data, s.count);
                Color col = s.IsActive ? s.data.uiColor : ColDim;
                string mark = s.IsActive ? "  ★" : "";
                var txt = UIFactory.CreateText(synergyContainer, "SynRow",
                    $"•  {DisplayName(s)}   {s.count}/{next}{mark}", 21, col, TextAnchor.MiddleLeft);
                if (s.IsActive) { txt.fontStyle = FontStyle.Bold; AddOutline(txt); }
                NoWrap(txt);
                var le = txt.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 30;
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

        private static string DisplayName(SynergyManager.SynergyStatus s)
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
            endText.text = "VITÓRIA!\n\nVocê defendeu os gatos\ncontra os pesadelos!";
            endText.color = ColGold;
            endPanel.SetActive(true);
        }

        public void ShowDefeatScreen()
        {
            if (endPanel == null) return;
            endText.text = "DERROTA\n\nOs pesadelos invadiram a base...\nTente outra composição!";
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
