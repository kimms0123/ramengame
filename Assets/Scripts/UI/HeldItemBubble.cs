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
    public class HeldItemBubble : MonoBehaviour
    {
        [SerializeField] float headOffset = 1.75f;
        [SerializeField] float chipSize = 46f;

        PlayerHand hand;
        RectTransform bubble;
        RectTransform content;
        string lastSignature;
        Camera cam;

        void Awake()
        {
            var art = GameDatabase.Art;
            var img = UIFactory.Image(UIRoot.Instance.WorldLayer, "HeldBubble", art != null ? art.speechBubble : null, new Color(1f, 1f, 1f, 0.94f));
            bubble = img.rectTransform;
            bubble.pivot = new Vector2(0.5f, 0f);

            var layout = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = img.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            content = bubble;
            bubble.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (hand == null) hand = FindAnyObjectByType<PlayerHand>();
            if (cam == null) cam = Camera.main;

            bool show = hand != null && !hand.IsEmpty && !InputGate.GameplayBlocked;
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

            if (UIFactory.WorldToLocal(UIRoot.Instance.WorldLayer, cam, HeadPosition(), out var local))
            {
                Vector2 pos = UIFactory.ClampInside(UIRoot.Instance.WorldLayer, local, bubble.rect.size, UIFactory.HudTopMargin);
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
            var art = GameDatabase.Art;

            if (item is IngredientItem ing)
            {
                UIFactory.IngredientChip(content, ing.Data, 1, chipSize);
                UIFactory.TextChip(content, ing.Data != null ? ing.Data.displayName : "?", 26, UIFactory.Ink);
                return;
            }

            if (item is Bowl b)
            {
                if (b.IsDirty)
                {
                    if (art != null && art.dirtyIcon != null) IconChip(art.dirtyIcon);
                    else UIFactory.TextChip(content, "더러운 그릇", 26, UIFactory.Ink);
                    return;
                }
                if (b.IsEmpty)
                {
                    UIFactory.TextChip(content, "빈 그릇", 26, UIFactory.Ink);
                    return;
                }

                if (b.Broth != null) UIFactory.IngredientChip(content, b.Broth, 1, chipSize);
                if (b.HasNoodle && db != null) UIFactory.IngredientChip(content, db.noodle, 1, chipSize);

                if (db != null && db.toppings != null)
                {
                    foreach (var t in db.toppings)
                    {
                        if (t != null && b.Toppings.TryGetValue(t, out int n) && n > 0) UIFactory.IngredientChip(content, t, n, chipSize);
                    }
                }

                if (b.AssignedTable > 0) UIFactory.TextChip(content, $"→T{b.AssignedTable}", 28, UIFactory.Accent);
                else if (b.IsRamen && !b.Decorated) UIFactory.TextChip(content, "고명 전", 22, new Color(0.4f, 0.3f, 0.25f));
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