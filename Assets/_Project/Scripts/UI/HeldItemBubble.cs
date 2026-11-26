using System.Text;
using RamenTycoon.Core;
using RamenTycoon.Kitchen;
using RamenTycoon.Player;
using UnityEngine;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 플레이어 머리 위 말풍선. (카톡 결정사항) 오버쿡드처럼 지금 그릇에 뭐가 들어갔는지 보여준다.
    /// 예: [매운 육수][면][차슈][계란×2]  T2
    /// 이미지: GameArtConfig.speechBubble (9-slice 권장), 재료 아이콘은 IngredientData.icon
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class HeldItemBubble : BakedView
    {
        [Tooltip("머리 위로 얼마나 띄울지 (월드 유닛)")]
        [SerializeField] float headOffset = 1.75f;
        [Tooltip("말풍선 안 재료 아이콘 크기")]
        [SerializeField] float chipSize = 46f;

        [Header("씬에 만들어 둔 UI (빌더가 연결, 지우지 마세요). 말풍선 그림·여백은 이 오브젝트에서 바꾸면 됨")]
        [SerializeField] RectTransform bubble;
        [SerializeField] RectTransform content;

        PlayerHand hand;
        string lastSignature;
        Camera cam;

        protected override void Awake()
        {
            base.Awake();
            bubble.gameObject.SetActive(false);
        }

        protected override bool IsIntact() => bubble != null && content != null;

        protected override void Build()
        {
            var art = GameDatabase.Art;
            var img = Track(UIFactory.Image(Root.WorldLayer, "HeldBubble (손에 든 재료 말풍선)", art != null ? art.speechBubble : null, new Color(1f, 1f, 1f, 0.94f)));
            UIArtSlot.Attach(img, nameof(art.speechBubble), new Color(1f, 1f, 1f, 0.94f));
            bubble = img.rectTransform;
            bubble.pivot = new Vector2(0.5f, 0f);

            var layout = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 14);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = img.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 아이콘이 들어가는 칸 (말풍선 꼬리와 따로 두어야 내용을 비울 때 꼬리가 안 지워짐)
            content = UIFactory.Rect(bubble, "Content");
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 6f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            UIFactory.BubbleTail(bubble);

            bubble.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (hand == null) hand = FindAnyObjectByType<PlayerHand>();
            if (cam == null) cam = Camera.main;

            // 빈 그릇·더러운 그릇은 손에 든 그릇 그림만으로 알 수 있으니 말풍선을 띄우지 않음
            bool show = hand != null && !hand.IsEmpty && !InputGate.GameplayBlocked
                        && !(hand.Held is Bowl held && (held.IsEmpty || held.IsDirty));
            if (bubble.gameObject.activeSelf != show) bubble.gameObject.SetActive(show);
            if (!show)
            {
                LastBubbleTop = float.NaN;
                return;
            }

            string sig = Signature(hand.Held);
            if (sig != lastSignature)
            {
                lastSignature = sig;
                Rebuild(hand.Held);
            }

            if (UIFactory.WorldToLocal(Root.WorldLayer, cam, HeadPosition(), out var local))
            {
                Vector2 pos = UIFactory.ClampInside(Root.WorldLayer, local, bubble.rect.size, UIFactory.HudTopMargin);
                bubble.anchoredPosition = pos;
                LastBubbleTop = pos.y + bubble.rect.height;
            }
        }

        /// <summary>플레이어 몸 그림의 머리 위 (캐릭터 크기가 바뀌어도 맞게)</summary>
        Vector3 HeadPosition()
        {
            if (bodyRenderer == null)
            {
                var body = hand.transform.Find("Body");
                if (body != null) bodyRenderer = body.GetComponent<SpriteRenderer>();
            }
            if (bodyRenderer != null && bodyRenderer.sprite != null)
                return new Vector3(hand.transform.position.x, bodyRenderer.bounds.max.y + 0.15f, 0f);
            return hand.transform.position + Vector3.up * headOffset;
        }

        SpriteRenderer bodyRenderer;

        /// <summary>이번 프레임 말풍선 윗변 (안내 문구가 그 위에 뜨도록)</summary>
        public static float LastBubbleTop { get; private set; } = float.NaN;

        static string Signature(HoldableItem item)
        {
            if (item is IngredientItem ing) return "I:" + (ing.Data != null ? ing.Data.name : "");
            if (item is Bowl b)
            {
                var sb = new StringBuilder("B:");
                sb.Append(b.IsDirty).Append(b.Broth != null ? b.Broth.name : "-").Append(b.HasNoodle).Append(b.AssignedTable);
                foreach (var kv in b.Toppings) sb.Append(kv.Key.name).Append(kv.Value);
                return sb.ToString();
            }
            return item != null ? item.name : "";
        }

        void Rebuild(HoldableItem item)
        {
            UIFactory.ClearChildren(content);
            var db = GameDatabase.Instance;

            if (item is IngredientItem ing)
            {
                UIFactory.IngredientChip(content, ing.Data, 1, chipSize);
                return;
            }

            if (item is Bowl b)
            {
                if (b.IsDirty || b.IsEmpty) return; // 말풍선 자체를 안 띄움 (LateUpdate)

                if (b.Broth != null) UIFactory.IngredientChip(content, b.Broth, 1, chipSize);
                if (b.HasNoodle && db != null) UIFactory.IngredientChip(content, db.noodle, 1, chipSize);

                if (db != null && db.toppings != null)
                {
                    foreach (var t in db.toppings)
                    {
                        if (t != null && b.Toppings.TryGetValue(t, out int n) && n > 0) UIFactory.IngredientChip(content, t, n, chipSize);
                    }
                }

            }
        }

        void IconChip(Sprite sprite)
        {
            var img = UIFactory.Image(content, "Icon", sprite, Color.white);
            img.preserveAspect = true;
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = chipSize;
            le.preferredHeight = chipSize;
        }
    }
}