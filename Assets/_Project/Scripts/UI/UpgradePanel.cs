using RamenTycoon.Audio;
using System.Collections.Generic;
using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Kitchen;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 업그레이드 창 (태블릿 안에 왼쪽 탭 + 가로 카드 목록, 좋은 피자 위대한 피자 상점 구도).
    /// - HUD 오른쪽 아래 태블릿 아이콘 / U 키로 열기 (영업 중에만, 열려 있는 동안 게임 멈춤)
    /// - 탭 = UpgradeData의 Category, 카드 = UpgradeData 한 개 (그림 · 이름 · 단계 · 설명 · 가격 버튼)
    /// - 카드/탭 모양은 씬의 CardTemplate / TabTemplate(꺼져 있음)을 고치면 모든 카드에 반영된다
    /// - 산 즉시 적용, 쓴 돈은 그날 정산에 "업그레이드"로 표시
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class UpgradePanel : BakedView
    {
        public static bool IsOpen { get; private set; }

        [Header("업그레이드 항목 (카드 순서대로)")]
        [Tooltip("비워 두면 기본값(설비 탭 · 화구 추가 70C / 140C)을 사용")]
        [SerializeField] List<UpgradeData> upgrades = new();

        [Tooltip("업그레이드 창을 여닫는 키 (None이면 버튼으로만)")]
        [SerializeField] Key openKey = Key.U;

        [Tooltip("태블릿 검은 테두리 두께 배율 (1 = 그림 그대로, 0.5 = 절반). 씬의 Tablet Image > Pixels Per Unit Multiplier와 같은 값")]
        [SerializeField, Range(0.25f, 3f)] float bezelThickness = 1f;

        [Header("색")]
        [SerializeField] Color tabColor = new(0.96f, 0.88f, 0.74f);
        [SerializeField] Color tabSelectedColor = Color.white;
        [SerializeField] Color buyColor = new(0.56f, 0.78f, 0.47f);
        [SerializeField] Color disabledColor = new(0.75f, 0.73f, 0.72f);
        [SerializeField] Color priceColor = new(0.17f, 0.1f, 0.07f);
        [SerializeField] Color priceShortColor = new(0.75f, 0.2f, 0.15f);
        [Tooltip("가격 버튼에 그림을 넣었을 때, 돈이 모자라면 곱해지는 색")]
        [SerializeField] Color disabledTint = new(0.6f, 0.6f, 0.6f);

        [Header("씬에 만들어 둔 UI (빌더가 연결, 지우지 마세요)")]
        [SerializeField] Button openButton;
        [SerializeField] RectTransform root;
        [SerializeField] TextMeshProUGUI coinsText;
        [SerializeField] Button closeButton;
        [SerializeField] RectTransform tabList;
        [SerializeField] Button tabTemplate;
        [SerializeField] ScrollRect cardScroll;
        [SerializeField] RectTransform cardContent;
        [SerializeField] UpgradeCardView cardTemplate;
        [SerializeField] TextMeshProUGUI messageText;

        bool pausedByMe;
        float messageTimer;
        string selectedCategory;
        readonly List<UpgradeData> runtimeDefaults = new();

        // 기본 색 (그림이 없을 때)
        static readonly Color ScreenColor = new(0.99f, 0.95f, 0.88f); // upgrade_panel 그림의 화면 색과 같게
        static readonly Color BezelColor = new(0.24f, 0.22f, 0.26f);
        static readonly Color Cream = new(0.99f, 0.94f, 0.84f);
        static readonly Color Brown = new(0.42f, 0.23f, 0.13f);
        static readonly Color Ribbon = new(0.6f, 0.34f, 0.2f);
        static readonly Color Ink = new(0.17f, 0.1f, 0.07f);

        protected override bool IsIntact() =>
            openButton != null && root != null && coinsText != null && closeButton != null && tabList != null && tabTemplate != null &&
            cardScroll != null && cardContent != null && cardTemplate != null && messageText != null;

        protected override void Awake()
        {
            base.Awake();
            root.gameObject.SetActive(false);
            tabTemplate.gameObject.SetActive(false);
            cardTemplate.gameObject.SetActive(false);
        }

        protected override void Bind()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
        }

        void OnDestroy()
        {
            if (IsOpen) IsOpen = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsOpen = false;

        // ───────── 만들기 (씬 빌더가 에디터에서 부름) ─────────

        protected override void Build()
        {
            var art = GameDatabase.Art;

            // HUD 오른쪽 아래 태블릿 아이콘
            Sprite iconSprite = art != null ? art.upgradeIcon : null;
            openButton = Track(UIFactory.Button(Root.HudLayer, "UpgradeButton (업그레이드 열기)", iconSprite != null ? "" : "업그레이드",
                null, iconSprite, new Vector2(120f, 90f), 24));
            UIFactory.Place((RectTransform)openButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 56f), new Vector2(120f, 90f));
            ((Image)openButton.targetGraphic).preserveAspect = true;
            UIArtSlot.Attach((Image)openButton.targetGraphic, nameof(art.upgradeIcon), UIFactory.Accent,
                iconSprite != null ? null : openButton.GetComponentInChildren<TextMeshProUGUI>().gameObject);

            // 어두운 배경 + 태블릿
            var dim = Track(UIFactory.Image(Root.PanelLayer, "UpgradeDim (업그레이드 창)", null, new Color(0f, 0f, 0f, 0.55f), raycast: true));
            root = dim.rectTransform;
            UIFactory.Stretch(root);

            Sprite frame = art != null ? art.upgradePanel : null;
            // 9-slice: 모서리·테두리 두께는 그대로, 가운데 화면만 늘어남 (upgrade_panel 그림의 Border = 24)
            var tablet = UIFactory.Image(root, "Tablet", frame, BezelColor, raycast: true);
            tablet.type = Image.Type.Sliced;
            tablet.pixelsPerUnitMultiplier = bezelThickness > 0.01f ? 1f / bezelThickness : 1f;
            UIFactory.Place(tablet.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1440f, 800f));
            GameObject fallbackScreen = null;
            if (frame == null)
            {
                var screen = UIFactory.Image(tablet.transform, "Screen (그림 없을 때만)", null, ScreenColor);
                UIFactory.Stretch(screen.rectTransform);
                screen.rectTransform.offsetMin = new Vector2(24f, 24f);
                screen.rectTransform.offsetMax = new Vector2(-24f, -24f);
                fallbackScreen = screen.gameObject;
            }
            UIArtSlot.Attach(tablet, nameof(art.upgradePanel), BezelColor, fallbackScreen);

            // 화면 안쪽 (테두리 24 + 여백 26)
            var content = UIFactory.Rect(tablet.transform, "Content");
            UIFactory.Stretch(content);
            content.offsetMin = new Vector2(50f, 40f);
            content.offsetMax = new Vector2(-50f, -40f);

            // 위쪽 줄: 보유 코인 + 닫기
            coinsText = UIFactory.Text(content, "Coins", "보유 0 C", 36, Ink, TextAnchor.MiddleRight);
            coinsText.fontStyle = FontStyles.Bold;
            UIFactory.Place(coinsText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-80f, 0f), new Vector2(400f, 60f));
            closeButton = UIFactory.Button(content, "Close", "×", null, null, new Vector2(60f, 60f), 46);
            UIFactory.Place((RectTransform)closeButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(60f, 60f));

            // 왼쪽 탭 목록
            tabList = UIFactory.Rect(content, "Tabs");
            tabList.anchorMin = new Vector2(0f, 0f);
            tabList.anchorMax = new Vector2(0f, 1f);
            tabList.pivot = new Vector2(0f, 1f);
            tabList.offsetMin = new Vector2(0f, 60f);
            tabList.offsetMax = new Vector2(240f, -76f);
            var tv = tabList.gameObject.AddComponent<VerticalLayoutGroup>();
            tv.spacing = 14f;
            tv.childAlignment = TextAnchor.UpperLeft;
            tv.childControlWidth = true;
            tv.childControlHeight = false;
            tv.childForceExpandWidth = true;
            tv.childForceExpandHeight = false;

            tabTemplate = MakeTabTemplate(content);

            // 오른쪽 카드 목록 (가로 스크롤)
            var scrollGo = UIFactory.Rect(content, "Cards");
            scrollGo.anchorMin = Vector2.zero;
            scrollGo.anchorMax = Vector2.one;
            scrollGo.offsetMin = new Vector2(264f, 60f);
            scrollGo.offsetMax = new Vector2(0f, -76f);
            var scrollBg = scrollGo.gameObject.AddComponent<Image>();
            scrollBg.color = new Color(0.93f, 0.85f, 0.72f);
            cardScroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            cardScroll.horizontal = true;
            cardScroll.vertical = false;
            cardScroll.movementType = ScrollRect.MovementType.Clamped;
            cardScroll.scrollSensitivity = 30f;

            var viewport = UIFactory.Rect(scrollGo, "Viewport");
            UIFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            cardScroll.viewport = viewport;

            cardContent = UIFactory.Rect(viewport, "Content");
            cardContent.anchorMin = new Vector2(0f, 0f);
            cardContent.anchorMax = new Vector2(0f, 1f);
            cardContent.pivot = new Vector2(0f, 0.5f);
            cardContent.offsetMin = cardContent.offsetMax = Vector2.zero;
            var ch = cardContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            ch.padding = new RectOffset(22, 22, 22, 22);
            ch.spacing = 22f;
            ch.childAlignment = TextAnchor.MiddleLeft;
            ch.childControlWidth = false;
            ch.childControlHeight = false;
            ch.childForceExpandWidth = false;
            ch.childForceExpandHeight = false;
            var fit = cardContent.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            cardScroll.content = cardContent;

            cardTemplate = MakeCardTemplate(scrollGo);

            // 아래 안내
            messageText = UIFactory.Text(content, "Message", "", 26, new Color(Ink.r, Ink.g, Ink.b, 0.65f), TextAnchor.MiddleCenter);
            UIFactory.Place(messageText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(130f, 4f), new Vector2(1000f, 44f));
            ShowMessage(null);

            root.gameObject.SetActive(false);
        }

        Button MakeTabTemplate(RectTransform parent)
        {
            var img = UIFactory.Image(parent, "TabTemplate (탭 모양 · 복사해서 씀)", null, tabColor, raycast: true);
            img.rectTransform.sizeDelta = new Vector2(240f, 76f);
            img.gameObject.AddComponent<Outline>().effectColor = Brown;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var label = UIFactory.Text(img.transform, "Label", "설비", 32, Ink, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyles.Bold;
            UIFactory.Stretch(label.rectTransform);
            img.gameObject.SetActive(false);
            return btn;
        }

        UpgradeCardView MakeCardTemplate(RectTransform parent)
        {
            var card = UIFactory.Image(parent, "CardTemplate (카드 모양 · 복사해서 씀)", null, Cream);
            card.rectTransform.sizeDelta = new Vector2(270f, 470f);
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = Brown;
            outline.effectDistance = new Vector2(3f, -3f);
            var view = card.gameObject.AddComponent<UpgradeCardView>();

            // 그림
            // 미리보기용으로 화구 그림을 넣어 둔다 (Play 때 카드마다 UpgradeData의 Icon으로 바뀜)
            var previewIcon = GameDatabase.Art != null ? GameDatabase.Art.stove : null;
            view.picture = UIFactory.Image(card.transform, "Picture", previewIcon, previewIcon != null ? Color.white : new Color(0.8f, 0.78f, 0.76f));
            view.picture.preserveAspect = true;
            UIFactory.AnchorTopLeft(view.picture.rectTransform, new Rect(0.1f, 0.04f, 0.8f, 0.34f));

            // 이름 띠
            var ribbon = UIFactory.Image(card.transform, "NameRibbon", null, Ribbon);
            UIFactory.AnchorTopLeft(ribbon.rectTransform, new Rect(0.03f, 0.41f, 0.94f, 0.1f));
            view.title = UIFactory.Text(ribbon.transform, "Name", "화구 추가", 30, Cream, TextAnchor.MiddleCenter);
            view.title.fontStyle = FontStyles.Bold;
            UIFactory.Stretch(view.title.rectTransform);

            // 단계
            view.level = UIFactory.Text(card.transform, "Level", "1 / 3", 24, new Color(Ink.r, Ink.g, Ink.b, 0.7f), TextAnchor.MiddleCenter);
            UIFactory.AnchorTopLeft(view.level.rectTransform, new Rect(0.05f, 0.52f, 0.9f, 0.07f));

            // 설명
            view.description = UIFactory.Text(card.transform, "Description", "화구를 하나 더 놓아서\n면을 동시에 삶을 수 있어요.", 24, Ink, TextAnchor.UpperCenter);
            view.description.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.AnchorTopLeft(view.description.rectTransform, new Rect(0.07f, 0.6f, 0.86f, 0.2f));

            // 가격 버튼
            view.buyImage = UIFactory.Image(card.transform, "Buy", null, buyColor, raycast: true);
            UIFactory.AnchorTopLeft(view.buyImage.rectTransform, new Rect(0.12f, 0.83f, 0.76f, 0.12f));
            view.buyImage.gameObject.AddComponent<Outline>().effectColor = Brown;
            view.buyButton = view.buyImage.gameObject.AddComponent<Button>();
            view.buyButton.targetGraphic = view.buyImage;
            view.buyLabel = UIFactory.Text(view.buyImage.transform, "Label", "70 C", 34, priceColor, TextAnchor.MiddleCenter);
            view.buyLabel.fontStyle = FontStyles.Bold;
            UIFactory.Stretch(view.buyLabel.rectTransform);

            card.gameObject.SetActive(false);
            return view;
        }

        // ───────── 열기 / 닫기 ─────────

        bool CanOpen()
        {
            if (IsOpen || SettingsPanel.IsOpen) return false;
            if (ToppingScreen.Instance != null && ToppingScreen.Instance.IsOpen) return false;
            var dm = DayManager.Instance;
            return dm == null || dm.Phase == DayPhase.Business || dm.Phase == DayPhase.Closing;
        }

        public void Open()
        {
            if (!CanOpen()) return;
            IsOpen = true;
            pausedByMe = !InputGate.GameplayBlocked;
            if (pausedByMe)
            {
                InputGate.GameplayBlocked = true;
                if (GameClock.Instance != null) GameClock.Instance.Paused = true;
            }
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            AudioManager.Play("ui_open");
            ShowMessage(null);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            root.gameObject.SetActive(false);
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            if (pausedByMe)
            {
                pausedByMe = false;
                InputGate.GameplayBlocked = false;
                if (GameClock.Instance != null) GameClock.Instance.Paused = false;
            }
        }

        void Update()
        {
            // 영업 중이 아니면 아이콘 숨김 (DAY 전환·정산 화면 위에 남지 않게)
            var dm = DayManager.Instance;
            bool showIcon = dm == null || dm.Phase == DayPhase.Business || dm.Phase == DayPhase.Closing;
            if (openButton.gameObject.activeSelf != showIcon) openButton.gameObject.SetActive(showIcon);

            var kb = Keyboard.current;
            if (!IsOpen)
            {
                if (kb != null && openKey != Key.None && kb[openKey].wasPressedThisFrame) Open();
                return;
            }

            var eco = Economy.Instance;
            coinsText.text = $"보유 {(eco != null ? eco.Coins : 0)} C";

            bool closeKey = kb != null && openKey != Key.None && kb[openKey].wasPressedThisFrame;
            if (closeKey || (kb != null && kb.escapeKey.wasPressedThisFrame && UIFactory.EscConsumedFrame != Time.frameCount))
            {
                UIFactory.EscConsumedFrame = Time.frameCount;
                Close();
                return;
            }

            if (messageTimer > 0f)
            {
                messageTimer -= Time.unscaledDeltaTime;
                if (messageTimer <= 0f) ShowMessage(null);
            }

            if (dm != null && (dm.Phase == DayPhase.Settlement || dm.Phase == DayPhase.Ending)) Close();
        }

        // ───────── 탭 / 카드 ─────────

        List<UpgradeData> Items()
        {
            var list = new List<UpgradeData>();
            foreach (var u in upgrades)
                if (u != null && UpgradeSlots.Find(u.id) != null) list.Add(u);
            if (list.Count > 0 || upgrades.Exists(u => u != null)) return list;

            if (runtimeDefaults.Count == 0)
            {
                var stove = ScriptableObject.CreateInstance<UpgradeData>(); // 기본값: 화구 추가 70 / 140
                stove.name = "UPG_Stove (기본값)";
                runtimeDefaults.Add(stove);
            }
            foreach (var u in runtimeDefaults)
                if (UpgradeSlots.Find(u.id) != null) list.Add(u);
            return list;
        }

        void Refresh()
        {
            var items = Items();

            // 탭: 카테고리 순서대로 (처음 나온 순서)
            var categories = new List<string>();
            foreach (var u in items)
            {
                string c = string.IsNullOrEmpty(u.category) ? "설비" : u.category;
                if (!categories.Contains(c)) categories.Add(c);
            }
            if (selectedCategory == null || !categories.Contains(selectedCategory))
                selectedCategory = categories.Count > 0 ? categories[0] : null;

            UIFactory.ClearChildren(tabList);
            foreach (var c in categories)
            {
                var tab = Instantiate(tabTemplate, tabList);
                tab.gameObject.name = "Tab_" + c;
                tab.gameObject.SetActive(true);
                var label = tab.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = c;
                bool selected = c == selectedCategory;
                if (tab.targetGraphic != null) tab.targetGraphic.color = selected ? tabSelectedColor : tabColor;
                string captured = c;
                tab.onClick.AddListener(() => { selectedCategory = captured; Refresh(); });
            }

            // 카드
            UIFactory.ClearChildren(cardContent);
            foreach (var u in items)
            {
                string c = string.IsNullOrEmpty(u.category) ? "설비" : u.category;
                if (c != selectedCategory) continue;
                CreateCard(u, UpgradeSlots.Find(u.id));
            }
            if (cardContent.childCount == 0)
            {
                var t = UIFactory.Text(cardContent, "Empty", "지금은 살 수 있는 업그레이드가 없어요", 30, Ink, TextAnchor.MiddleCenter);
                t.rectTransform.sizeDelta = new Vector2(700f, 100f);
            }
            cardScroll.horizontalNormalizedPosition = 0f;
        }

        void CreateCard(UpgradeData data, UpgradeSlots slots)
        {
            var art = GameDatabase.Art;
            var card = Instantiate(cardTemplate, cardContent);
            card.gameObject.name = "Card_" + data.id;
            card.gameObject.SetActive(true);

            // 그림: UpgradeData의 Icon → 없으면 설비 그림(GameArtConfig) → 그것도 없으면 템플릿에 넣어 둔 그림
            Sprite icon = data.icon;
            if (icon == null && art != null) icon = data.id == "table" ? art.table : data.id == "sink" ? art.sink : art.stove;
            if (icon == null && cardTemplate.picture != null) icon = cardTemplate.picture.sprite;
            if (card.picture != null)
            {
                card.picture.sprite = icon;
                card.picture.preserveAspect = true;
                card.picture.color = icon != null ? Color.white : new Color(0.8f, 0.78f, 0.76f);
                if (icon == null) Debug.LogWarning($"[UpgradePanel] '{data.displayName}' 카드에 넣을 그림이 없어요. {data.name} 에셋의 Icon 칸에 그림을 넣어 주세요.");
            }

            int maxLevel = Mathf.Min(slots.MaxLevel, data.MaxLevel + 1);
            if (card.title != null) card.title.text = data.displayName;
            if (card.level != null) card.level.text = $"{slots.Level} / {maxLevel}";
            if (card.description != null) card.description.text = data.description;

            int cost = slots.Level < maxLevel ? data.CostFor(slots.Level) : -1;
            var eco = Economy.Instance;
            bool canBuy = cost >= 0 && eco != null && eco.Coins >= cost;

            if (card.buyLabel != null)
            {
                card.buyLabel.text = cost < 0 ? "MAX" : $"{cost} C";
                card.buyLabel.color = cost >= 0 && !canBuy ? priceShortColor : priceColor;
            }
            if (card.buyImage != null)
            {
                // 버튼 그림을 넣었으면 그림 색 그대로(살 수 없을 땐 어둡게), 없으면 Buy Color / Disabled Color로 칠함
                if (card.buyImage.sprite != null) card.buyImage.color = canBuy ? Color.white : disabledTint;
                else card.buyImage.color = canBuy ? buyColor : disabledColor;
            }
            if (card.buyButton != null)
            {
                card.buyButton.interactable = cost >= 0; // 돈이 모자라도 눌러서 얼마 모자란지 볼 수 있게
                card.buyButton.onClick.AddListener(() => Buy(data, slots));
            }
        }

        void Buy(UpgradeData data, UpgradeSlots slots)
        {
            int maxLevel = Mathf.Min(slots.MaxLevel, data.MaxLevel + 1);
            if (slots.Level >= maxLevel) return;
            int cost = data.CostFor(slots.Level);
            var eco = Economy.Instance;
            if (cost < 0 || eco == null) return;
            if (!eco.TrySpendUpgrade(cost))
            {
                ShowMessage($"돈이 {cost - eco.Coins} C 모자라요");
                AudioManager.Play("error");
                return;
            }
            slots.TryUpgrade();
            AudioManager.Play("upgrade");
            ShowMessage($"{data.displayName} 완료!  ({slots.Level} / {maxLevel})");
            Refresh();
        }

        void ShowMessage(string msg)
        {
            messageText.text = string.IsNullOrEmpty(msg) ? "구매하면 바로 적용돼요   ·   Esc 닫기" : msg;
            messageTimer = string.IsNullOrEmpty(msg) ? 0f : 2.5f;
        }
    }
}