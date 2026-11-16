using System.Collections.Generic;
using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Kitchen;
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
    public class ToppingScreen : MonoBehaviour
    {
        public static ToppingScreen Instance { get; private set; }

        [SerializeField] float slowTimeScale = 0.5f;
        [Tooltip("올라간 고명 크기 (그릇 폭 대비)")]
        [SerializeField] float toppingSizeRatio = 0.26f;

        // 그릇 안 고명 자리 (0~1, 그릇 이미지 기준). 가운데 1 + 바깥 8
        static readonly Vector2[] Slots = BuildSlots();

        RectTransform root;
        Image bowlImage;
        AspectRatioFitter bowlAspect;
        RectTransform toppingLayer;
        Image cursor;
        Text messageText;
        float messageTimer;

        Bowl bowl;
        IngredientData holding;
        readonly Dictionary<IngredientData, int> placed = new();
        readonly List<(Image image, IngredientData data, int slot)> placedImages = new();
        readonly bool[] slotUsed = new bool[Slots.Length];
        float savedTimeScale = 1f;

        public bool IsOpen { get; private set; }

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

        void Awake()
        {
            Instance = this;
            Build();
            root.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ───────── 화면 구성 ─────────

        void Build()
        {
            var art = GameDatabase.Art;
            var db = GameDatabase.Instance;

            root = UIFactory.Rect(UIRoot.Instance.ScreenLayer, "ToppingScreen");
            UIFactory.Stretch(root);

            // 배경
            var bg = UIFactory.Image(root, "Background", art != null ? art.toppingBackground : null, new Color(0.5f, 0.31f, 0.18f), raycast: true);
            UIFactory.Stretch(bg.rectTransform);
            bool hasBackground = art != null && art.toppingBackground != null;
            if (!hasBackground)
            {
                var shelf = UIFactory.Image(root, "Shelf", null, new Color(0.78f, 0.55f, 0.33f));
                UIFactory.AnchorTopLeft(shelf.rectTransform, new Rect(0f, 0f, 1f, 0.45f));
            }

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
                    var bin = UIFactory.Image(root, $"Bin_{data.id}", null, new Color(0.45f, 0.45f, 0.48f));
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

                var captured = data;
                var hot = UIFactory.HotSpot(root, $"BinButton_{data.id}", () => PickTopping(captured));
                UIFactory.AnchorTopLeft((RectTransform)hot.transform, r);
            }

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
            var bowlButton = bowlImage.gameObject.AddComponent<Button>();
            bowlButton.transition = Selectable.Transition.None;
            bowlButton.onClick.AddListener(OnBowlClicked);

            toppingLayer = UIFactory.Rect(bowlImage.transform, "Toppings");
            UIFactory.Stretch(toppingLayer);

            // 빌지
            Rect memoRect = art != null ? art.memoRect : new Rect(0.06f, 0.48f, 0.2f, 0.46f);
            OrderMemoView.Create(root, new Vector2(memoRect.x, memoRect.y), memoRect.width * 1920f, "Memo");

            // 완성 벨 / 다시
            var bell = UIFactory.Button(root, "Bell", art != null && art.bellSprite != null ? "" : "완성 (Enter)",
                () => Close(true), art != null ? art.bellSprite : null, new Vector2(230f, 120f), 32);
            UIFactory.Place((RectTransform)bell.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-90f, 90f), new Vector2(230f, 120f));
            if (art != null && art.bellSprite != null) ((Image)bell.targetGraphic).preserveAspect = true;

            var reset = UIFactory.Button(root, "Reset", art != null && art.trashSprite != null ? "" : "다시 (R)",
                ResetToppings, art != null ? art.trashSprite : null, new Vector2(170f, 90f), 28);
            UIFactory.Place((RectTransform)reset.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-340f, 105f), new Vector2(170f, 90f));
            if (art != null && art.trashSprite != null) ((Image)reset.targetGraphic).preserveAspect = true;

            // 안내 문구
            var help = UIFactory.Text(root, "Help", "고명 통을 눌러 집고 그릇을 눌러 올려요   ·   우클릭 내려놓기   ·   Esc 취소", 26, Color.white, TextAnchor.MiddleCenter);
            UIFactory.Place(help.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1400f, 40f));
            help.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);

            messageText = UIFactory.Text(root, "Message", "", 34, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleCenter);
            UIFactory.Place(messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(900f, 50f));
            messageText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);

            // 커서에 붙는 고명 (맨 위)
            cursor = UIFactory.Image(root, "Cursor", null, Color.white);
            cursor.preserveAspect = true;
            cursor.rectTransform.sizeDelta = new Vector2(110f, 110f);
            cursor.color = new Color(1f, 1f, 1f, 0.85f);
            cursor.gameObject.SetActive(false);
        }

        // ───────── 열기 / 닫기 ─────────

        public void Open(Bowl target)
        {
            if (target == null || IsOpen) return;
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
                if (kb.escapeKey.wasPressedThisFrame) { Close(false); return; }
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
            holding = data;
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
                    return;
                }
                AddTopping(holding, slot);
            }
            else
            {
                RemoveNearest(norm);
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
            Destroy(p.image.gameObject);
            placedImages.RemoveAt(best);
        }

        void ResetToppings()
        {
            ClearPlaced();
            DropHolding();
        }

        void ClearPlaced()
        {
            foreach (var p in placedImages)
            {
                if (p.image != null) Destroy(p.image.gameObject);
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