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
    /// 고명 데코 화면 (1인칭, 좋은 피자 위대한 피자 구도).
    /// - 위쪽 고명 통 클릭 → 집기 (커서에 붙음, 계속 들고 있어서 여러 번 올릴 수 있음)
    /// - 그릇 클릭 → 가장 가까운 빈 자리에 올림 / 빈손으로 올린 고명 클릭 → 빼기
    /// - 우클릭: 내려놓기 / R: 다시 / Enter·벨: 완성 / Esc: 취소
    /// - 왼쪽: 빌지(모든 테이블 주문, 완성된 주문 취소선)
    /// - 열려 있는 동안 가게 시간은 0.5배속
    ///
    /// 이미지: GameArtConfig의 toppingBackground / decoBowl* / bellSprite / trashSprite / memoPaper,
    ///         고명은 IngredientData.decoSprite(없으면 icon). 위치는 GameArtConfig의 *Rect 값으로 조절.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class ToppingScreen : BakedView
    {
        public static ToppingScreen Instance { get; private set; }

        [Tooltip("고명 화면이 열려 있는 동안 가게 시간 배속 (0.5 = 절반 속도)")]
        [SerializeField] float slowTimeScale = 0.5f;
        [Tooltip("올라간 고명 크기 (그릇 폭 대비)")]
        [SerializeField] float toppingSizeRatio = 0.26f;
        [Tooltip("집은 고명이 커서에 붙어 보이는 크기")]
        [SerializeField] float cursorSize = 110f;

        [Header("씬에 만들어 둔 UI (빌더가 연결, 지우지 마세요)")]
        [SerializeField] RectTransform root;
        [Tooltip("고명 통 클릭 영역. 순서 = GameDatabase의 Toppings 순서 (차슈, 김, 파, 계란)")]
        [SerializeField] Button[] binButtons;
        [SerializeField] Image bowlImage;
        [SerializeField] AspectRatioFitter bowlAspect;
        [SerializeField] Button bowlButton;
        [SerializeField] RectTransform toppingLayer;
        [SerializeField] Button bellButton;
        [SerializeField] Button resetButton;
        [SerializeField] Image cursor;
        [SerializeField] TextMeshProUGUI messageText;

        // 그릇 안 고명 자리 (0~1, 그릇 이미지 기준). 가운데 1 + 바깥 8
        static readonly Vector2[] Slots = BuildSlots();

        float messageTimer;

        Bowl bowl;
        IngredientData holding;
        readonly Dictionary<IngredientData, int> placed = new();
        readonly List<(Image image, IngredientData data, int slot)> placedImages = new();
        readonly bool[] slotUsed = new bool[Slots.Length];
        float savedTimeScale = 1f;

        public bool IsOpen { get; private set; }

        // 튜토리얼 화살표용
        public Bowl CurrentBowl => bowl;
        public IngredientData Holding => holding;
        public IReadOnlyDictionary<IngredientData, int> Placed => placed;
        public RectTransform BowlRect => bowlImage != null ? bowlImage.rectTransform : null;
        public RectTransform BellRect => bellButton != null ? (RectTransform)bellButton.transform : null;

        public RectTransform BinRect(IngredientData data)
        {
            var db = GameDatabase.Instance;
            if (data == null || db == null || db.toppings == null || binButtons == null) return null;
            int i = System.Array.IndexOf(db.toppings, data);
            return i >= 0 && i < binButtons.Length && binButtons[i] != null ? (RectTransform)binButtons[i].transform : null;
        }

        static Vector2[] BuildSlots()
        {
            var list = new List<Vector2> { new Vector2(0.5f, 0.5f) };
            for (int i = 0; i < 8; i++)
            {
                float a = (90f - i * 45f) * Mathf.Deg2Rad;
                list.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.25f, 0.5f + Mathf.Sin(a) * 0.25f));
            }
            return list.ToArray();
        }

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
            root.gameObject.SetActive(false);
        }

        protected override bool IsIntact() =>
            root != null && binButtons != null && bowlImage != null && bowlAspect != null && bowlButton != null &&
            toppingLayer != null && bellButton != null && resetButton != null && cursor != null && messageText != null;

        protected override void Bind()
        {
            var db = GameDatabase.Instance;
            for (int i = 0; i < binButtons.Length; i++)
            {
                if (binButtons[i] == null) continue;
                int index = i;
                binButtons[i].onClick.AddListener(() =>
                {
                    if (db != null && db.toppings != null && index < db.toppings.Length) PickTopping(db.toppings[index]);
                });
            }
            bowlButton.onClick.AddListener(OnBowlClicked);
            bellButton.onClick.AddListener(() => Close(true));
            resetButton.onClick.AddListener(ResetToppings);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ───────── 화면 구성 ─────────

        protected override void Build()
        {
            var art = GameDatabase.Art;
            var db = GameDatabase.Current;

            root = Track(UIFactory.Rect(Root.ScreenLayer, "ToppingScreen"));
            UIFactory.Stretch(root);

            // 배경 (그림이 없을 때만 보이는 선반·통 그림은 fallbackOnly로 묶어서 그림이 들어오면 자동으로 숨김)
            var fallbackOnly = new List<GameObject>();
            var bgColor = new Color(0.5f, 0.31f, 0.18f);
            var bg = UIFactory.Image(root, "Background", art != null ? art.toppingBackground : null, bgColor, raycast: true);
            UIFactory.Stretch(bg.rectTransform);
            bool hasBackground = art != null && art.toppingBackground != null;
            if (!hasBackground)
            {
                var shelf = UIFactory.Image(root, "Shelf (그림 없을 때만)", null, new Color(0.78f, 0.55f, 0.33f));
                UIFactory.AnchorTopLeft(shelf.rectTransform, new Rect(0f, 0f, 1f, 0.45f));
                fallbackOnly.Add(shelf.gameObject);
            }
            var bins = new List<Button>();

            // 고명 통 (배경 위 클릭 영역)
            var toppings = db != null ? db.toppings : null;
            int count = toppings != null ? toppings.Length : 0;
            for (int i = 0; i < count; i++)
            {
                var data = toppings[i];
                if (data == null) continue;
                Rect r = art != null && art.toppingBinRects != null && i < art.toppingBinRects.Length
                    ? art.toppingBinRects[i]
                    : new Rect(0.2f + i * 0.15f, 0.19f, 0.13f, 0.22f);

                if (!hasBackground)
                {
                    // 배경 이미지가 없으면 통을 직접 그린다
                    var bin = UIFactory.Image(root, $"Bin_{data.id} (그림 없을 때만)", null, new Color(0.45f, 0.45f, 0.48f));
                    fallbackOnly.Add(bin.gameObject);
                    UIFactory.AnchorTopLeft(bin.rectTransform, r);
                    var icon = UIFactory.Image(bin.transform, "Icon", data.DecoOrIcon, data.placeholderColor);
                    icon.preserveAspect = true;
                    UIFactory.Stretch(icon.rectTransform);
                    icon.rectTransform.offsetMin = new Vector2(18f, 40f);
                    icon.rectTransform.offsetMax = new Vector2(-18f, -14f);
                    var label = UIFactory.Text(bin.transform, "Label", data.displayName, 28, Color.white, TextAnchor.LowerCenter);
                    UIFactory.Stretch(label.rectTransform);
                    label.rectTransform.offsetMin = new Vector2(0f, 6f);
                }

                var hot = UIFactory.HotSpot(root, $"BinButton_{i + 1}_{data.id}", null);
                UIFactory.AnchorTopLeft((RectTransform)hot.transform, r);
                while (bins.Count < i) bins.Add(null);
                bins.Add(hot);
            }
            binButtons = bins.ToArray();
            UIArtSlot.Attach(bg, nameof(art.toppingBackground), bgColor, fallbackOnly.ToArray());

            // 그릇
            Rect bowlRect = art != null ? art.decoBowlRect : new Rect(0.4f, 0.47f, 0.29f, 0.5f);
            var bowlArea = UIFactory.Rect(root, "BowlArea");
            UIFactory.AnchorTopLeft(bowlArea, bowlRect);
            bowlImage = UIFactory.Image(bowlArea, "Bowl", null, new Color(0.95f, 0.92f, 0.86f), raycast: true);
            UIFactory.Stretch(bowlImage.rectTransform);
            // 이미지 비율에 맞춰 영역을 줄여서, 클릭 위치와 그릇 그림이 정확히 일치하게 한다
            bowlAspect = bowlImage.gameObject.AddComponent<AspectRatioFitter>();
            bowlAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            bowlAspect.aspectRatio = 1f;
            bowlButton = bowlImage.gameObject.AddComponent<Button>();
            bowlButton.transition = Selectable.Transition.None;

            toppingLayer = UIFactory.Rect(bowlImage.transform, "Toppings");
            UIFactory.Stretch(toppingLayer);

            // 빌지
            Rect memoRect = art != null ? art.memoRect : new Rect(0.06f, 0.48f, 0.2f, 0.46f);
            OrderMemoView.Create(root, new Vector2(memoRect.x, memoRect.y), memoRect.width * 1920f, "Memo");

            // 완성 벨 / 다시
            bellButton = UIFactory.Button(root, "Bell", art != null && art.bellSprite != null ? "" : "완성 (Enter)",
                null, art != null ? art.bellSprite : null, new Vector2(230f, 120f), 32);
            UIFactory.Place((RectTransform)bellButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-90f, 90f), new Vector2(230f, 120f));
            ((Image)bellButton.targetGraphic).preserveAspect = true;
            UIArtSlot.Attach((Image)bellButton.targetGraphic, nameof(art.bellSprite), UIFactory.Accent,
                art != null && art.bellSprite != null ? null : bellButton.GetComponentInChildren<TextMeshProUGUI>().gameObject);

            resetButton = UIFactory.Button(root, "Reset", art != null && art.trashSprite != null ? "" : "다시 (R)",
                null, art != null ? art.trashSprite : null, new Vector2(170f, 90f), 28);
            UIFactory.Place((RectTransform)resetButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-340f, 105f), new Vector2(170f, 90f));
            ((Image)resetButton.targetGraphic).preserveAspect = true;
            UIArtSlot.Attach((Image)resetButton.targetGraphic, nameof(art.trashSprite), UIFactory.Accent,
                art != null && art.trashSprite != null ? null : resetButton.GetComponentInChildren<TextMeshProUGUI>().gameObject);

            // 안내 문구
            var help = UIFactory.Text(root, "Help", "고명 통을 눌러 집고 그릇을 눌러 올려요   ·   우클릭 내려놓기   ·   Esc 취소", 26, Color.white, TextAnchor.MiddleCenter);
            UIFactory.Place(help.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1400f, 40f));
            UIFactory.SetOutline(help, new Color(0f, 0f, 0f, 0.85f));

            messageText = UIFactory.Text(root, "Message", "", 34, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleCenter);
            UIFactory.Place(messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(900f, 50f));
            UIFactory.SetOutline(messageText, new Color(0f, 0f, 0f, 0.85f));

            // 커서에 붙는 고명 (맨 위)
            cursor = UIFactory.Image(root, "Cursor", null, Color.white);
            cursor.preserveAspect = true;
            cursor.rectTransform.sizeDelta = new Vector2(cursorSize, cursorSize);
            cursor.color = new Color(1f, 1f, 1f, 0.85f);
            cursor.gameObject.SetActive(false);
            root.gameObject.SetActive(false); // 씬에서 편집할 때는 Hierarchy에서 ToppingScreen을 켜서 보면 됨
        }

        // ───────── 열기 / 닫기 ─────────

        public void Open(Bowl target)
        {
            if (target == null || IsOpen) return;
            AudioManager.Play("ui_open");
            bowl = target;
            holding = null;
            cursor.gameObject.SetActive(false);

            var art = GameDatabase.Art;
            var db = GameDatabase.Instance;
            bool spicy = db != null && target.Broth == db.spicy;
            Sprite bowlSprite = null;
            if (art != null)
            {
                bowlSprite = spicy ? art.decoBowlSpicy : art.decoBowlTonkotsu;
                if (bowlSprite == null) bowlSprite = spicy ? art.decoBowlTonkotsu : art.decoBowlSpicy; // 한쪽만 있으면 임시로 사용
            }
            UIFactory.SetSprite(bowlImage, bowlSprite, target.Broth != null ? target.Broth.placeholderColor : Color.white);
            bowlAspect.aspectRatio = bowlSprite != null && bowlSprite.rect.height > 0f ? bowlSprite.rect.width / bowlSprite.rect.height : 1f;

            root.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases(); // 그릇 크기를 먼저 계산해야 고명 크기를 맞출 수 있다

            ClearPlaced();
            foreach (var kv in target.Toppings)
            {
                for (int i = 0; i < kv.Value; i++) AddTopping(kv.Key, FirstFreeSlot());
            }

            IsOpen = true;
            InputGate.GameplayBlocked = true;
            if (GameClock.Instance != null)
            {
                savedTimeScale = GameClock.Instance.TimeScale;
                GameClock.Instance.TimeScale = savedTimeScale * slowTimeScale;
            }
        }

        public void Close(bool apply)
        {
            if (!IsOpen) return;
            if (apply && !TutorialState.ToppingsMatch(placed))
            {
                ShowMessage("주문서대로 토핑을 다 올려야 종을 울릴 수 있어요");
                AudioManager.Play("error");
                return;
            }
            AudioManager.Play(apply ? "bell" : "ui_close");

            if (apply && bowl != null)
            {
                bowl.SetToppings(placed);
                var ticket = OrderBoard.Instance != null ? OrderBoard.Instance.AssignBestMatch(bowl) : null;
                if (ticket == null && OrderBoard.Instance != null) OrderBoard.Instance.Unassign(bowl);
            }

            bowl = null;
            holding = null;
            IsOpen = false;
            root.gameObject.SetActive(false);
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

            if (GameClock.Instance != null) GameClock.Instance.TimeScale = savedTimeScale;
            var dm = DayManager.Instance;
            bool playing = dm == null || dm.Phase == DayPhase.Business || dm.Phase == DayPhase.Closing;
            if (playing) InputGate.GameplayBlocked = false;
        }

        // ───────── 입력 ─────────

        void Update()
        {
            if (!IsOpen) return;

            // 그릇이 사라졌거나 하루가 끝나면 닫는다
            var dm = DayManager.Instance;
            if (bowl == null || (dm != null && (dm.Phase == DayPhase.Settlement || dm.Phase == DayPhase.Ending)))
            {
                Close(false);
                return;
            }

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame && UIFactory.EscConsumedFrame != Time.frameCount)
                {
                    UIFactory.EscConsumedFrame = Time.frameCount;
                    Close(false);
                    return;
                }
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { Close(true); return; }
                if (kb.rKey.wasPressedThisFrame) ResetToppings();

                // 숫자키 1~4로도 고명 집기
                var db = GameDatabase.Instance;
                if (db != null && db.toppings != null)
                {
                    if (kb.digit1Key.wasPressedThisFrame && db.toppings.Length > 0) PickTopping(db.toppings[0]);
                    if (kb.digit2Key.wasPressedThisFrame && db.toppings.Length > 1) PickTopping(db.toppings[1]);
                    if (kb.digit3Key.wasPressedThisFrame && db.toppings.Length > 2) PickTopping(db.toppings[2]);
                    if (kb.digit4Key.wasPressedThisFrame && db.toppings.Length > 3) PickTopping(db.toppings[3]);
                }
            }

            if (mouse != null)
            {
                if (mouse.rightButton.wasPressedThisFrame) DropHolding();

                if (holding != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(root, mouse.position.ReadValue(), null, out var local))
                    cursor.rectTransform.anchoredPosition = local;
            }

            if (messageTimer > 0f)
            {
                messageTimer -= Time.unscaledDeltaTime;
                if (messageTimer <= 0f) messageText.text = "";
            }
        }

        void PickTopping(IngredientData data)
        {
            if (data == null) return;
            var need = TutorialState.RequiredToppings;
            if (need != null)
            {
                need.TryGetValue(data, out int want);
                placed.TryGetValue(data, out int have);
                if (want <= 0 || have >= want)
                {
                    ShowMessage(want <= 0 ? "주문서에 없는 토핑이에요" : "이 토핑은 다 올렸어요");
                    AudioManager.Play("error");
                    return;
                }
            }
            holding = data;
            AudioManager.Play("pickup", 0.7f);
            UIFactory.SetSprite(cursor, data.DecoOrIcon, data.placeholderColor);
            cursor.color = new Color(cursor.color.r, cursor.color.g, cursor.color.b, 0.85f);
            cursor.preserveAspect = true;
            cursor.gameObject.SetActive(true);
            cursor.transform.SetAsLastSibling();
        }

        void DropHolding()
        {
            holding = null;
            cursor.gameObject.SetActive(false);
        }

        void OnBowlClicked()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var rt = bowlImage.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, mouse.position.ReadValue(), null, out var local)) return;
            Rect r = rt.rect;
            var norm = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);

            if (holding != null)
            {
                int slot = NearestFreeSlot(norm);
                if (slot < 0)
                {
                    ShowMessage("그릇이 가득 찼어요");
                    AudioManager.Play("error");
                    return;
                }
                var need = TutorialState.RequiredToppings;
                if (need != null)
                {
                    need.TryGetValue(holding, out int want);
                    placed.TryGetValue(holding, out int have);
                    if (have >= want)
                    {
                        ShowMessage("이 토핑은 다 올렸어요");
                        AudioManager.Play("error");
                        DropHolding();
                        return;
                    }
                }
                AddTopping(holding, slot);
                AudioManager.Play("topping");
                // 튜토리얼: 필요한 개수를 다 올리면 손에서 내려놓아 다음 토핑으로 넘어가게
                if (need != null && placed.TryGetValue(holding, out int now) && need.TryGetValue(holding, out int max) && now >= max)
                    DropHolding();
            }
            else
            {
                RemoveNearest(norm);
                AudioManager.Play("pickup", 0.6f);
            }
        }

        // ───────── 고명 배치 ─────────

        void AddTopping(IngredientData data, int slot)
        {
            if (data == null || slot < 0) return;

            placed.TryGetValue(data, out int n);
            placed[data] = n + 1;
            slotUsed[slot] = true;

            var img = UIFactory.Image(toppingLayer, data.displayName, data.DecoOrIcon, data.placeholderColor);
            img.preserveAspect = true;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Slots[slot];
            rt.pivot = new Vector2(0.5f, 0.5f);
            float size = bowlImage.rectTransform.rect.width * toppingSizeRatio;
            if (size <= 1f) size = 120f;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));

            if (data.DecoOrIcon == null)
            {
                var t = UIFactory.Text(img.transform, "Letter", data.displayName, 26, UIFactory.Ink, TextAnchor.MiddleCenter);
                UIFactory.Stretch(t.rectTransform);
            }

            placedImages.Add((img, data, slot));
        }

        void RemoveNearest(Vector2 norm)
        {
            int best = -1;
            float bestDist = 0.12f * 0.12f;
            for (int i = 0; i < placedImages.Count; i++)
            {
                float d = (Slots[placedImages[i].slot] - norm).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            if (best < 0) return;

            var p = placedImages[best];
            if (placed.TryGetValue(p.data, out int n))
            {
                if (n <= 1) placed.Remove(p.data);
                else placed[p.data] = n - 1;
            }
            slotUsed[p.slot] = false;
            UIFactory.DestroySafe(p.image.gameObject);
            placedImages.RemoveAt(best);
        }

        void ResetToppings()
        {
            AudioManager.Play("trash", 0.7f);
            ClearPlaced();
            DropHolding();
        }

        void ClearPlaced()
        {
            foreach (var p in placedImages)
            {
                if (p.image != null) UIFactory.DestroySafe(p.image.gameObject);
            }
            placedImages.Clear();
            placed.Clear();
            for (int i = 0; i < slotUsed.Length; i++) slotUsed[i] = false;
        }

        int NearestFreeSlot(Vector2 norm)
        {
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < Slots.Length; i++)
            {
                if (slotUsed[i]) continue;
                float d = (Slots[i] - norm).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }

        int FirstFreeSlot()
        {
            for (int i = 0; i < slotUsed.Length; i++)
            {
                if (!slotUsed[i]) return i;
            }
            return -1;
        }

        void ShowMessage(string msg)
        {
            messageText.text = msg;
            messageTimer = 1.5f;
        }
    }
}