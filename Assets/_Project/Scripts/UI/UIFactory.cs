using System;
using RamenTycoon.Core;
using RamenTycoon.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 코드로 uGUI를 만드는 도우미. 이미지가 null이면 색 네모로 대신 그린다.
    /// 글자는 모두 TextMeshPro (TextMeshProUGUI). 폰트는 GameArtConfig.uiFontAsset 한 곳에서 정한다.
    /// 모든 UI는 1920x1080 기준 좌표로 만든다 (CanvasScaler가 화면에 맞춰 줌).
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color Ink = new(0.18f, 0.11f, 0.07f);
        public static readonly Color Paper = new(0.98f, 0.94f, 0.84f);
        public static readonly Color Panel = new(0.16f, 0.1f, 0.07f, 0.92f);
        public static readonly Color Accent = new(0.85f, 0.25f, 0.18f);

        static bool warnedFont;

        /// <summary>이번 프레임에 Esc를 이미 처리한 UI가 있으면 그 프레임 번호 (Esc 한 번에 창이 두 번 반응하지 않게)</summary>
        public static int EscConsumedFrame = -1;

        /// <summary>
        /// UI 폰트 (TMP 폰트 에셋). GameArtConfig에 없으면 TMP 기본 폰트 — 기본 폰트에는 한글이 없어서 네모로 나온다.
        /// </summary>
        public static TMP_FontAsset Font
        {
            get
            {
                var art = GameDatabase.Art;
                if (art != null && art.uiFontAsset != null)
                {
                    if (preparedFont != art.uiFontAsset) PrepareFont(art.uiFontAsset);
                    return art.uiFontAsset;
                }
                if (!warnedFont)
                {
                    warnedFont = true;
                    Debug.LogWarning("[UI] GameArtConfig의 Ui Font Asset이 비어 있어요. 한글 TMP 폰트 에셋을 넣지 않으면 한글이 네모로 보입니다.");
                }
                return null; // null이면 TMP가 자기 기본 폰트(TMP Settings)를 씀
            }
        }

        static TMP_FontAsset preparedFont;

        /// <summary>
        /// 한글이 ㅁ로 깨지지 않게 폰트 에셋을 정리한다.
        /// - Dynamic: 쓰는 글자를 그때그때 아틀라스에 추가
        /// - Multi Atlas Textures: 아틀라스가 꽉 차면 새 장을 추가 (한글이 많으면 한 장으로 부족)
        /// - 굵은 글씨용 폰트(Font Weights > Bold)에 없는 글자는 기본 폰트에서 가져오도록 Fallback 연결
        /// </summary>
        public static void PrepareFont(TMP_FontAsset font)
        {
            preparedFont = font;
            if (font == null) return;
            MakeDynamic(font);

            var table = font.fontWeightTable;
            if (table == null) return;
            foreach (var pair in table)
            {
                foreach (var weighted in new[] { pair.regularTypeface, pair.italicTypeface })
                {
                    if (weighted == null || weighted == font) continue;
                    MakeDynamic(weighted);
                    if (weighted.fallbackFontAssetTable == null) weighted.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                    if (!weighted.fallbackFontAssetTable.Contains(font)) weighted.fallbackFontAssetTable.Add(font);
                }
            }
        }

        static void MakeDynamic(TMP_FontAsset f)
        {
            if (f.sourceFontFile == null) return; // 원본 .ttf 연결이 없으면 동적으로 글자를 못 만든다
            if (f.atlasPopulationMode != AtlasPopulationMode.Dynamic) f.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            if (!f.isMultiAtlasTexturesEnabled) f.isMultiAtlasTexturesEnabled = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            preparedFont = null;
            warnedFont = false;
            EscConsumedFrame = -1;
        }

        // ───────── 캔버스 ─────────

        /// <summary>1920x1080 기준으로 화면에 맞춰지는 Screen Space 캔버스</summary>
        public static RectTransform CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
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

        public static TextMeshProUGUI Text(Transform parent, string name, string text, int size, Color color, TextAnchor align)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = Font;
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = ToTmp(align);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = true;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>TMP 글자 외곽선 (TMP는 uGUI Outline 컴포넌트가 아니라 머티리얼 외곽선을 쓴다)</summary>
        public static void SetOutline(TMP_Text t, Color color, float width = 0.2f)
        {
            if (t == null) return;
            t.outlineColor = color;
            t.outlineWidth = width;
        }

        public static TextAlignmentOptions ToTmp(TextAnchor a) => a switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center
        };

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
                SetOutline(badge, Color.white, 0.25f);
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
                DestroySafe(child.gameObject);
            }
        }

        /// <summary>
        /// 말풍선 아래 가운데 꼬리. 그림이 없으면 투명(나중에 GameArtConfig에 그림이 들어오면 자동으로 보임).
        /// 레이아웃에서 빠지도록 ignoreLayout.
        /// </summary>
        public static Image BubbleTail(RectTransform bubble)
        {
            var art = GameDatabase.Art;
            Sprite tail = art != null ? art.speechBubbleTail : null;
            var img = Image(bubble, "Tail", tail, Color.clear);
            img.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, 3f); // 말풍선 아래 테두리 한 줄을 덮어서 이어 보이게
            rt.sizeDelta = tail != null ? new Vector2(tail.rect.width, tail.rect.height) : new Vector2(21f, 12f);
            UIArtSlot.Attach(img, "speechBubbleTail", Color.clear);
            return img;
        }

        /// <summary>Play 중이면 Destroy, 에디터(씬 만들기)에서는 DestroyImmediate</summary>
        public static void DestroySafe(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
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