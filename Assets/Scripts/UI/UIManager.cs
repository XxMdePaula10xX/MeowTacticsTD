using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MeowTactics.Core;
using MeowTactics.Data;
using MeowTactics.Cats;
using MeowTactics.Managers;

namespace MeowTactics.UI
{
    /// <summary>
    /// Constrói TODA a interface por código e a mantém sincronizada com o jogo.
    /// Basta existir 1 GameObject com este componente na cena.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        // ---- Paleta (PRD seção 21) ----
        private static readonly Color ColWood   = new Color(0.85f, 0.72f, 0.50f);
        private static readonly Color ColDark   = new Color(0.12f, 0.10f, 0.20f, 0.92f);
        private static readonly Color ColPanel  = new Color(0.18f, 0.15f, 0.28f, 0.95f);
        private static readonly Color ColGold   = new Color(1f, 0.84f, 0.30f);
        private static readonly Color ColGreen  = new Color(0.35f, 0.75f, 0.40f);
        private static readonly Color ColRed    = new Color(0.80f, 0.30f, 0.30f);
        private static readonly Color ColBlue   = new Color(0.30f, 0.55f, 0.85f);

        // ---- Referências de runtime ----
        private Text waveText, livesText, coinsText, phaseText, messageText;
        private Transform shopContainer, benchContainer, synergyContainer, itemsContainer;
        private Button startWaveButton, rerollButton;

        private GameObject detailPanel;
        private Text detailText;
        private Button detailSellBtn, detailReturnBtn;
        private CatUnit detailCat;

        private GameObject draftPanel;
        private Transform draftContainer;

        private GameObject endPanel;
        private Text endText;

        private Coroutine messageRoutine;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BuildUI();
            Subscribe();
            RefreshAll();
        }

        // =========================================================
        //  CONSTRUÇÃO DA UI
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
            BuildSynergyPanel(root);
            BuildItemsBar(root);
            BuildBottomBar(root);
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

        private void BuildTopBar(Transform root)
        {
            var bar = UIFactory.CreatePanel(root, "TopBar", ColDark);
            var rt = bar.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            rt.sizeDelta = new Vector2(0, 90);
            rt.anchoredPosition = Vector2.zero;

            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(30, 30, 10, 10);

            phaseText = UIFactory.CreateText(bar.transform, "Phase", "Meow Tactics TD", 30, ColGold, TextAnchor.MiddleLeft);
            waveText  = UIFactory.CreateText(bar.transform, "Wave", "Onda 1/10", 30, Color.white);
            livesText = UIFactory.CreateText(bar.transform, "Lives", "Vidas: 20", 30, ColRed);
            coinsText = UIFactory.CreateText(bar.transform, "Coins", "Moedas: 10", 30, ColGold, TextAnchor.MiddleRight);
        }

        private void BuildSynergyPanel(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "SynergyPanel", ColPanel);
            var rt = panel.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f));
            rt.sizeDelta = new Vector2(330, -(90 + 270)); // entre top bar e bottom bar
            rt.anchoredPosition = new Vector2(0, (270 - 90) / 2f);

            UIFactory.CreateText(panel.transform, "Title", "SINERGIAS", 24, ColGold, TextAnchor.UpperCenter)
                .rectTransform.anchoredPosition = new Vector2(0, -8);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            var crt = content.GetComponent<RectTransform>();
            UIFactory.StretchFull(crt, 10f);
            crt.offsetMax = new Vector2(crt.offsetMax.x, -40);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 6;
            vlg.childAlignment = TextAnchor.UpperCenter;
            synergyContainer = content.transform;
        }

        private void BuildItemsBar(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "ItemsPanel", ColPanel);
            var rt = panel.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f));
            rt.sizeDelta = new Vector2(150, -(90 + 270));
            rt.anchoredPosition = new Vector2(0, (270 - 90) / 2f);

            UIFactory.CreateText(panel.transform, "Title", "ITENS", 22, ColGold, TextAnchor.UpperCenter)
                .rectTransform.anchoredPosition = new Vector2(0, -8);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            var crt = content.GetComponent<RectTransform>();
            UIFactory.StretchFull(crt, 8f);
            crt.offsetMax = new Vector2(crt.offsetMax.x, -36);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 6;
            vlg.childAlignment = TextAnchor.UpperCenter;
            itemsContainer = content.transform;
        }

        private void BuildBottomBar(Transform root)
        {
            var bar = UIFactory.CreatePanel(root, "BottomBar", ColDark);
            var rt = bar.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            rt.sizeDelta = new Vector2(0, 270);
            rt.anchoredPosition = Vector2.zero;

            // ---- Linha da loja ----
            var shopRow = new GameObject("ShopRow", typeof(RectTransform));
            shopRow.transform.SetParent(bar.transform, false);
            var srt = shopRow.GetComponent<RectTransform>();
            UIFactory.SetAnchors(srt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            srt.sizeDelta = new Vector2(-30, 120);
            srt.anchoredPosition = new Vector2(0, -15);
            var shlg = shopRow.AddComponent<HorizontalLayoutGroup>();
            shlg.spacing = 10; shlg.childForceExpandWidth = true; shlg.childForceExpandHeight = true;
            shlg.padding = new RectOffset(15, 15, 0, 0);

            rerollButton = UIFactory.CreateButton(shopRow.transform, "Reroll",
                "Atualizar\n(2 moedas)", ColBlue, () => ShopManager.Instance?.RerollShop(), 18);
            AddFixedWidth(rerollButton, 130);

            shopContainer = shopRow.transform;

            // ---- Linha do banco ----
            var benchRow = new GameObject("BenchRow", typeof(RectTransform));
            benchRow.transform.SetParent(bar.transform, false);
            var brt = benchRow.GetComponent<RectTransform>();
            UIFactory.SetAnchors(brt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            brt.sizeDelta = new Vector2(-30, 120);
            brt.anchoredPosition = new Vector2(0, 15);
            var bhlg = benchRow.AddComponent<HorizontalLayoutGroup>();
            bhlg.spacing = 8; bhlg.childForceExpandWidth = true; bhlg.childForceExpandHeight = true;
            bhlg.padding = new RectOffset(15, 15, 0, 0);

            UIFactory.CreateText(benchRow.transform, "BenchLabel", "BANCO:", 18, ColWood, TextAnchor.MiddleCenter);

            benchContainer = benchRow.transform;

            startWaveButton = UIFactory.CreateButton(benchRow.transform, "StartWave",
                "INICIAR\nONDA >", ColGreen, () => GameManager.Instance?.StartWave(), 20);
            AddFixedWidth(startWaveButton, 160);
        }

        private void AddFixedWidth(Component c, float width)
        {
            var le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = width; le.preferredWidth = width; le.flexibleWidth = 0;
        }

        private void BuildDetailPanel(Transform root)
        {
            detailPanel = UIFactory.CreatePanel(root, "DetailPanel", ColPanel).gameObject;
            var rt = (RectTransform)detailPanel.transform;
            UIFactory.SetAnchors(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(560, 420);

            detailText = UIFactory.CreateText(detailPanel.transform, "Info", "", 22, Color.white, TextAnchor.UpperLeft);
            var drt = detailText.rectTransform;
            UIFactory.SetAnchors(drt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            drt.sizeDelta = new Vector2(-40, 280);
            drt.anchoredPosition = new Vector2(0, -20);

            var btnRow = new GameObject("Buttons", typeof(RectTransform));
            btnRow.transform.SetParent(detailPanel.transform, false);
            var brt = btnRow.GetComponent<RectTransform>();
            UIFactory.SetAnchors(brt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            brt.sizeDelta = new Vector2(-40, 110);
            brt.anchoredPosition = new Vector2(0, 20);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

            detailSellBtn = UIFactory.CreateButton(btnRow.transform, "Sell", "Vender", ColRed,
                () => { if (detailCat != null) PlacementManager.Instance?.SellCat(detailCat); }, 20);
            detailReturnBtn = UIFactory.CreateButton(btnRow.transform, "Return", "Voltar p/ banco", ColBlue,
                () => { if (detailCat != null) PlacementManager.Instance?.ReturnCatToBench(detailCat); }, 18);
            UIFactory.CreateButton(btnRow.transform, "Close", "Fechar", ColWood,
                () => PlacementManager.Instance?.ClearFocus(), 20);

            detailPanel.SetActive(false);
        }

        private void BuildDraftPanel(Transform root)
        {
            draftPanel = UIFactory.CreatePanel(root, "DraftPanel", new Color(0, 0, 0, 0.85f)).gameObject;
            UIFactory.StretchFull((RectTransform)draftPanel.transform);

            UIFactory.CreateText(draftPanel.transform, "Title", "ESCOLHA UM ITEM!", 40, ColGold, TextAnchor.UpperCenter)
                .rectTransform.anchoredPosition = new Vector2(0, -120);

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
            var ert = endText.rectTransform;
            UIFactory.SetAnchors(ert, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            ert.sizeDelta = new Vector2(1200, 400);
            ert.anchoredPosition = new Vector2(0, 80);

            var btn = UIFactory.CreateButton(endPanel.transform, "Restart", "Jogar Novamente", ColGreen,
                () => GameManager.Instance?.Restart(), 28);
            var rt = UIFactory.AsRect(btn);
            UIFactory.SetAnchors(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(360, 90);
            rt.anchoredPosition = new Vector2(0, -160);

            endPanel.SetActive(false);
        }

        private void BuildMessage(Transform root)
        {
            var panel = UIFactory.CreatePanel(root, "MessageToast", new Color(0, 0, 0, 0.8f));
            var rt = panel.rectTransform;
            UIFactory.SetAnchors(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            rt.sizeDelta = new Vector2(700, 70);
            rt.anchoredPosition = new Vector2(0, -110);
            messageText = UIFactory.CreateText(panel.transform, "Text", "", 26, Color.white);
            UIFactory.StretchFull(messageText.rectTransform, 10);
            panel.gameObject.SetActive(false);
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
            if (WaveManager.Instance != null) WaveManager.Instance.OnWaveChanged += (_, __) => UpdateWave();
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
        }

        private void OnStateChanged(GameState state)
        {
            if (startWaveButton != null)
                startWaveButton.interactable = (state == GameState.Preparation);
            if (rerollButton != null)
                rerollButton.interactable = (state == GameState.Preparation);

            if (state == GameState.Victory) ShowVictoryScreen();
            else if (state == GameState.Defeat) ShowDefeatScreen();
        }

        // =========================================================
        //  ATUALIZAÇÕES DE TEXTO
        // =========================================================
        public void UpdateCoins()
        {
            if (coinsText != null && EconomyManager.Instance != null)
                coinsText.text = "Moedas: " + EconomyManager.Instance.Coins;
        }

        public void UpdateLives()
        {
            if (livesText != null && GameManager.Instance != null)
                livesText.text = "Vidas: " + GameManager.Instance.Lives;
        }

        public void UpdateWave()
        {
            if (waveText != null && WaveManager.Instance != null)
                waveText.text = "Onda " + (WaveManager.Instance.CurrentWaveIndex + 1) + "/" + WaveManager.Instance.waves.Count;
        }

        // =========================================================
        //  LOJA
        // =========================================================
        public void UpdateShop()
        {
            if (shopContainer == null || ShopManager.Instance == null) return;
            ClearDynamic(shopContainer, "ShopBtn");

            var options = ShopManager.Instance.currentShopOptions;
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                CatData cat = options[i];
                string label = cat == null ? "—" : BuildShopLabel(cat);
                Color color = cat == null ? new Color(0.2f, 0.2f, 0.2f) : DamageColor(cat.damageType) * 0.8f;
                var btn = UIFactory.CreateButton(shopContainer, "ShopBtn", label, color,
                    () => ShopManager.Instance.BuyCat(index), 16);
                btn.interactable = cat != null;
            }
        }

        private string BuildShopLabel(CatData cat)
        {
            return $"{cat.catName}\nCusto {cat.cost}\n{DamageName(cat.damageType)}\n{SynergyNames(cat.synergies)}";
        }

        // =========================================================
        //  BANCO
        // =========================================================
        public void UpdateBench()
        {
            if (benchContainer == null || BenchManager.Instance == null) return;
            ClearDynamic(benchContainer, "BenchBtn");

            foreach (var cat in BenchManager.Instance.benchCats)
            {
                CatUnit c = cat;
                string label = c.Data.catName + (c.Items.Count > 0 ? $"\n[{c.Items.Count} itens]" : "");
                var btn = UIFactory.CreateButton(benchContainer, "BenchBtn", label,
                    DamageColor(c.Data.damageType) * 0.7f,
                    () => PlacementManager.Instance?.SelectBenchCatForPlacement(c), 15);
                // Mantém o botão de iniciar onda no fim: reordenado abaixo.
                btn.transform.SetSiblingIndex(benchContainer.childCount - 2);
            }
        }

        // =========================================================
        //  SINERGIAS
        // =========================================================
        public void UpdateSynergies()
        {
            if (synergyContainer == null || SynergyManager.Instance == null) return;
            ClearDynamic(synergyContainer, "SynRow");

            foreach (var s in SynergyManager.Instance.GetActiveSynergies())
            {
                if (s.count <= 0) continue;
                string check = s.IsActive ? " ✓" : "";
                string name = string.IsNullOrEmpty(s.data.displayName) ? s.data.synergyType.ToString() : s.data.displayName;
                var txt = UIFactory.CreateText(synergyContainer, "SynRow",
                    $"{name}  {s.count}{check}", 22,
                    s.IsActive ? s.data.uiColor : new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleLeft);
                var le = txt.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 30;
            }
        }

        // =========================================================
        //  ITENS (inventário)
        // =========================================================
        public void UpdateItems()
        {
            if (itemsContainer == null || ItemManager.Instance == null) return;
            ClearDynamic(itemsContainer, "ItemBtn");

            foreach (var item in ItemManager.Instance.inventory)
            {
                ItemData it = item;
                bool selected = ItemManager.Instance.SelectedForEquip == it;
                Color color = it.uiColor * (selected ? 1.2f : 0.7f);
                color.a = 1f;
                var btn = UIFactory.CreateButton(itemsContainer, "ItemBtn",
                    (selected ? "▶ " : "") + it.itemName, color,
                    () => ToggleItemSelection(it), 14);
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 60;
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
        //  PAINEL DE DETALHES DO GATO
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
                var color = it.uiColor * 0.8f; color.a = 1f;
                UIFactory.CreateButton(draftContainer, "DraftBtn", label, color,
                    () => PickDraftItem(it), 18);
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
                case DamageType.Physical: return new Color(0.95f, 0.6f, 0.2f); // laranja
                case DamageType.Magical:  return new Color(0.55f, 0.4f, 0.9f); // roxo
                case DamageType.True:     return new Color(0.95f, 0.9f, 0.7f); // dourado claro
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
