using System;
using RamenTycoon.Core;
using RamenTycoon.Data;
using UnityEngine;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 코드로 uGUI를 만드는 도우미. 이미지가 null이면 색 네모로 대신 그린다.
    /// 모든 UI는 1920x1080 기준 좌표로 만든다 (CanvasScaler가 화면에 맞춰 줌).
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color Ink = new(0.18f, 0.11f, 0.07f);
        public static readonly Color Paper = new(0.98f, 0.94f, 0.84f);
        public static readonly Color Panel = new(0.16f, 0.1f, 0.07f, 0.92f);
        public static readonly Color Accent = new(0.85f, 0.25f, 0.18f);

        static Font builtinFont;

        public static Font Font
        {
            get
            {
                var art = GameDatabase.Art;
                if (art != null && art.uiFont != null) return art.uiFont;
                if (builtinFont == null) builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return builtinFont;
            }
        }

        // ───────── RectTransform ─────────

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>부모 기준 0~1 영역 (왼쪽 위가 0,0)</summary>
        public static void AnchorTopLeft(RectTransform rt, Rect r)
        {
            rt.anchorMin = new Vector2(r.xMin, 1f - r.yMax);
            rt.anchorMax = new Vector2(r.xMax, 1f - r.yMin);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // ───────── 기본 요소 ─────────

        public static Image Image(Transform parent, string name, Sprite sprite, Color fallback, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            SetSprite(img, sprite, fallback);
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>이미지가 있으면 원본색, 없으면 fallback 색 네모. 9-slice 테두리가 있으면 Sliced로.</summary>
        public static void SetSprite(Image img, Sprite sprite, Color fallback)
        {
            img.sprite = sprite;
            img.color = sprite != null ? Color.white : fallback;
            img.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        }

        public static Text Text(Transform parent, string name, string text, int size, Color color, TextAnchor align)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string name, string label, Action onClick, Sprite sprite, Vector2 size, int fontSize = 30)
        {
            var img = Image(parent, name, sprite, Accent, raycast: true);
            img.rectTransform.sizeDelta = size;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            // 글자가 비어 있어도 Label은 항상 만든다 (나중에 코드에서 글자를 넣는 버튼이 있음)
            var t = Text(img.transform, "Label", label ?? "", fontSize, sprite != null ? Ink : Color.white, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            return btn;
        }

        /// <summary>투명한 클릭 영역 (배경 이미지 위의 고명 통 등). 마우스를 올리면 살짝 밝아진다.</summary>
        public static Button HotSpot(Transform parent, string name, Action onClick)
        {
            var img = Image(parent, name, null, Color.white, raycast: true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.22f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.38f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        // ───────── 재료 아이콘 ─────────

        /// <summary>
        /// 재료 아이콘 칸. 이미지가 없으면 재료 색 네모 + 첫 글자.
        /// count가 2 이상이면 오른쪽 아래에 ×N.
        /// </summary>
        public static RectTransform IngredientChip(Transform parent, IngredientData data, int count, float size)
        {
            var root = Rect(parent, data != null ? data.displayName : "Chip");
            root.sizeDelta = new Vector2(size, size);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;

            var icon = Image(root, "Icon", data != null ? data.icon : null, data != null ? data.placeholderColor : Color.gray);
            icon.preserveAspect = true;
            Stretch(icon.rectTransform);

            if (data != null && data.icon == null)
            {
                string letter = string.IsNullOrEmpty(data.displayName) ? "?" : data.displayName.Substring(0, 1);
                var t = Text(icon.transform, "Letter", letter, Mathf.RoundToInt(size * 0.5f), Ink, TextAnchor.MiddleCenter);
                Stretch(t.rectTransform);
            }

            if (count > 1)
            {
                var badge = Text(root, "Count", "×" + count, Mathf.RoundToInt(size * 0.42f), Accent, TextAnchor.LowerRight);
                Stretch(badge.rectTransform);
                badge.rectTransform.offsetMin = new Vector2(0f, -size * 0.18f);
                badge.rectTransform.offsetMax = new Vector2(size * 0.25f, 0f);
                var outline = badge.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
            }
            return root;
        }

        public static RectTransform TextChip(Transform parent, string text, int fontSize, Color color)
        {
            var t = Text(parent, "TextChip", text, fontSize, color, TextAnchor.MiddleCenter);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = t.preferredWidth + 6f;
            le.preferredHeight = fontSize + 8f;
            return t.rectTransform;
        }

        public static void ClearChildren(Transform t)
        {
            // 먼저 떼어내야 같은 프레임의 레이아웃 계산에 지운 칸이 섞이지 않는다
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i);
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        // ───────── 월드 → UI 좌표 ─────────

        /// <summary>
        /// 아래쪽 가운데가 기준(pivot 0.5, 0)인 UI가 화면 위쪽(HUD 바 아래)과 좌우 밖으로 나가지 않게 위치를 제한한다.
        /// </summary>
        public static Vector2 ClampInside(RectTransform space, Vector2 local, Vector2 size, float topMargin, float sideMargin = 10f)
        {
            Rect r = space.rect;
            float maxY = r.yMax - topMargin - size.y;
            float halfW = size.x * 0.5f;
            local.y = Mathf.Min(local.y, maxY);
            local.x = Mathf.Clamp(local.x, r.xMin + sideMargin + halfW, r.xMax - sideMargin - halfW);
            return local;
        }

        /// <summary>상단 HUD 바가 차지하는 높이 (말풍선이 이 아래에서 멈춤)</summary>
        public const float HudTopMargin = 110f;

        public static bool WorldToLocal(RectTransform space, Camera cam, Vector3 world, out Vector2 local)
        {
            local = default;
            if (cam == null) return false;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(space, sp, null, out local);
        }
    }
}