using System;
using RamenTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 설정 창 (배경음 / 효과음 / 전체 화면 + 기본값 · 적용 · 닫기).
    /// 그래픽 시안(1383x778) 좌표에 부품 이미지를 하나씩 배치하고, 글자는 모두 TMP로 얹는다.
    /// 부품 이미지가 비어 있으면 색 네모로 대신 그려서 그래픽 없이도 동작한다.
    /// - 슬라이더를 움직이면 바로 미리보기, [적용]을 눌러야 저장
    /// - [닫기] / X / Esc: 저장하지 않은 변경은 버리고 닫기
    /// - [기본값]: 70% / 80% / 켜짐 (적용을 눌러야 저장)
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        // ── 시안 픽셀 좌표 (1383x778, 왼쪽 위 기준) ──
        const float W = 1383f, H = 778f, PanelWidth = 1100f;

        static readonly Rect TitleIcon = Rect.MinMaxRect(130, 58, 215, 142);
        static readonly Rect TitleText = Rect.MinMaxRect(400, 55, 983, 150);
        static readonly Rect BtnX = Rect.MinMaxRect(1186, 60, 1274, 152);

        static readonly Rect Icon1 = Rect.MinMaxRect(145, 225, 218, 285);
        static readonly Rect Icon2 = Rect.MinMaxRect(145, 343, 218, 403);
        static readonly Rect Icon3 = Rect.MinMaxRect(145, 460, 218, 532);
        static readonly Rect Label1 = Rect.MinMaxRect(262, 222, 640, 290);
        static readonly Rect Label2 = Rect.MinMaxRect(262, 340, 640, 408);
        static readonly Rect Label3 = Rect.MinMaxRect(262, 458, 640, 528);

        static readonly Rect Track1 = Rect.MinMaxRect(676, 239, 1081, 275);   // 테두리 포함 트랙
        static readonly Rect Track2 = Rect.MinMaxRect(676, 358, 1081, 394);
        const float TrackInset = 9f;                                          // 테두리 안쪽 (채움 영역)
        static readonly Rect Pct1 = Rect.MinMaxRect(1110, 228, 1250, 276);
        static readonly Rect Pct2 = Rect.MinMaxRect(1110, 347, 1250, 395);

        static readonly Rect CheckBox = Rect.MinMaxRect(1010, 460, 1080, 530);
        static readonly Rect CheckLabel = Rect.MinMaxRect(1100, 462, 1240, 522);

        static readonly Rect BtnDefault = Rect.MinMaxRect(124, 598, 470, 700);
        static readonly Rect BtnApply = Rect.MinMaxRect(542, 598, 870, 700);
        static readonly Rect BtnClose = Rect.MinMaxRect(946, 598, 1270, 700);

        // ── 그래픽이 없을 때 쓰는 색 (시안에서 뽑은 색) ──
        static readonly Color Ink = new(0.17f, 0.1f, 0.07f);
        static readonly Color Cream = new(0.98f, 0.9f, 0.78f);
        static readonly Color PanelBrown = new(0.53f, 0.28f, 0.18f);
        static readonly Color RowCream = new(0.98f, 0.86f, 0.69f);
        static readonly Color TrackTan = new(0.81f, 0.65f, 0.46f);
        static readonly Color FillRed = new(0.70f, 0.30f, 0.19f);
        static readonly Color Outline = new(0.17f, 0.13f, 0.1f);

        Slider bgm, sfx;
        TextMeshProUGUI bgmText, sfxText, fullLabel;
        Image check;
        bool fullscreen;
        Action onClosed;
        RectTransform root;
        RectTransform panel;

        public static bool IsOpen { get; private set; }

        /// <summary>parent 위에 설정 창을 띄운다 (뒤는 어둡게, 클릭 막음).</summary>
        public static SettingsPanel Open(RectTransform parent, Action closed)
        {
            if (IsOpen) return null;
            GameSettings.Load();

            var dim = UIFactory.Image(parent, "SettingsDim", null, new Color(0f, 0f, 0f, 0.55f), raycast: true);
            UIFactory.Stretch(dim.rectTransform);
            var p = dim.gameObject.AddComponent<SettingsPanel>();
            p.root = dim.rectTransform;
            p.onClosed = closed;
            p.Build();
            IsOpen = true;
            return p;
        }

        void Build()
        {
            var art = GameDatabase.Art;

            // 배경
            Sprite bg = art != null ? art.settingsPanel : null;
            var panelImg = UIFactory.Image(root, "Panel", bg, PanelBrown, raycast: true);
            UIFactory.Place(panelImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, PanelWidth * H / W));
            panel = panelImg.rectTransform;
            if (bg == null) BuildFallbackBackground();

            // 제목 줄
            Picture(art != null ? art.settingsTitleIcon : null, TitleIcon, "TitleIcon");
            var title = Label(TitleText, "설정", 60, TextAnchor.MiddleCenter, Cream);
            title.characterSpacing = 4f;
            ImageButton(BtnX, art != null ? art.settingsButtonX : null, new Color(0.73f, 0.27f, 0.17f), "×", Cream, 56, Close);

            // 항목 3줄
            Picture(art != null ? art.settingsSoundIcon : null, Icon1, "SoundIcon1");
            Picture(art != null ? art.settingsSoundIcon : null, Icon2, "SoundIcon2");
            Picture(art != null ? art.settingsScreenIcon : null, Icon3, "ScreenIcon");
            Label(Label1, "배경음", 46, TextAnchor.MiddleLeft, Ink);
            Label(Label2, "효과음", 46, TextAnchor.MiddleLeft, Ink);
            Label(Label3, "전체 화면", 46, TextAnchor.MiddleLeft, Ink);

            bgm = CreateSlider(Track1, art);
            sfx = CreateSlider(Track2, art);
            bgmText = Label(Pct1, "", 42, TextAnchor.MiddleCenter, Ink);
            sfxText = Label(Pct2, "", 42, TextAnchor.MiddleCenter, Ink);
            bgm.onValueChanged.AddListener(_ => RefreshTexts());
            sfx.onValueChanged.AddListener(_ => RefreshTexts());

            // 전체 화면 체크
            var boxImg = Picture(art != null ? art.settingsCheckbox : null, CheckBox, "CheckBox", fallback: new Color(1f, 0.96f, 0.86f));
            boxImg.raycastTarget = true;
            if (art == null || art.settingsCheckbox == null) boxImg.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor = Outline;
            var boxBtn = boxImg.gameObject.AddComponent<Button>();
            boxBtn.targetGraphic = boxImg;
            boxBtn.onClick.AddListener(() => { fullscreen = !fullscreen; RefreshTexts(); });
            check = UIFactory.Image(boxImg.transform, "Check", art != null ? art.settingsCheck : null, FillRed);
            check.preserveAspect = true;
            UIFactory.Stretch(check.rectTransform);
            if (art == null || art.settingsCheck == null)
            {
                check.rectTransform.offsetMin = new Vector2(12f, 12f);
                check.rectTransform.offsetMax = new Vector2(-12f, -12f);
            }
            fullLabel = Label(CheckLabel, "", 42, TextAnchor.MiddleLeft, Ink);

            // 아래 버튼 3개
            ImageButton(BtnDefault, art != null ? art.settingsButtonDefault : null, new Color(0.86f, 0.6f, 0.36f), "기본값", Ink, 44,
                () => SetValues(GameSettings.DefaultBgm, GameSettings.DefaultSfx, GameSettings.DefaultFullscreen));
            ImageButton(BtnApply, art != null ? art.settingsButtonApply : null, new Color(0.69f, 0.42f, 0.26f), "적용", Ink, 44,
                () => GameSettings.Apply(bgm.value, sfx.value, fullscreen));
            ImageButton(BtnClose, art != null ? art.settingsButtonClose : null, new Color(0.73f, 0.27f, 0.17f), "닫기", Cream, 44, Close);

            SetValues(GameSettings.Bgm, GameSettings.Sfx, GameSettings.Fullscreen);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && UIFactory.EscConsumedFrame != Time.frameCount)
            {
                UIFactory.EscConsumedFrame = Time.frameCount;
                Close();
            }
        }

        void SetValues(float b, float s, bool full)
        {
            bgm.value = b;
            sfx.value = s;
            fullscreen = full;
            RefreshTexts();
        }

        void RefreshTexts()
        {
            bgmText.text = $"{Mathf.RoundToInt(bgm.value * 100f)}%";
            sfxText.text = $"{Mathf.RoundToInt(sfx.value * 100f)}%";
            fullLabel.text = fullscreen ? "켜짐" : "꺼짐";
            check.enabled = fullscreen;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Destroy(root.gameObject);
            onClosed?.Invoke();
        }

        void OnDestroy() => IsOpen = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsOpen = false;

        // ───────── 만들기 도우미 (좌표는 모두 시안 픽셀) ─────────

        RectTransform Area(Rect px, string name)
        {
            var rt = UIFactory.Rect(panel, name);
            UIFactory.AnchorTopLeft(rt, new Rect(px.xMin / W, px.yMin / H, px.width / W, px.height / H));
            return rt;
        }

        /// <summary>시안 글자 크기(px)를 실제 크기로 (창을 PanelWidth로 줄여서 그리므로)</summary>
        static int Size(float px) => Mathf.RoundToInt(px * PanelWidth / W);

        TextMeshProUGUI Label(Rect px, string text, float sizePx, TextAnchor align, Color color)
        {
            var t = UIFactory.Text(Area(px, "Label"), "Text", text, Size(sizePx), color, align);
            UIFactory.Stretch(t.rectTransform);
            t.fontStyle = FontStyles.Bold;
            return t;
        }

        Image Picture(Sprite sprite, Rect px, string name, Color? fallback = null)
        {
            var area = Area(px, name);
            var img = area.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            if (sprite != null)
            {
                UIFactory.SetSprite(img, sprite, Color.white);
                img.preserveAspect = img.type == Image.Type.Simple;
            }
            else
            {
                img.color = fallback ?? Color.clear; // 아이콘이 없으면 비워 둠
            }
            return img;
        }

        void ImageButton(Rect px, Sprite sprite, Color fallback, string label, Color labelColor, float sizePx, Action click)
        {
            var area = Area(px, "Button_" + label);
            var img = area.gameObject.AddComponent<Image>();
            UIFactory.SetSprite(img, sprite, fallback);
            if (sprite == null) area.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor = Outline;
            var btn = area.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => click?.Invoke());

            var t = UIFactory.Text(area, "Label", label, Size(sizePx), labelColor, TextAnchor.MiddleCenter);
            UIFactory.Stretch(t.rectTransform);
            t.fontStyle = FontStyles.Bold;
        }

        Slider CreateSlider(Rect track, Data.GameArtConfig art)
        {
            // 잡기 쉽게 트랙보다 위아래로 넓은 영역
            const float grab = 20f;
            var area = Area(Rect.MinMaxRect(track.xMin, track.yMin - grab, track.xMax, track.yMax + grab), "Slider");
            var hit = area.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            var slider = area.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.None;

            float h = track.height + grab * 2f;
            float top = grab / h, bottom = 1f - top;                       // 트랙 테두리 바깥
            float inTop = (grab + TrackInset) / h, inBottom = 1f - inTop;   // 테두리 안쪽
            float inX = TrackInset / track.width;

            // 빈 트랙
            var back = UIFactory.Image(area, "Track", art != null ? art.settingsSliderTrack : null, Outline);
            back.rectTransform.anchorMin = new Vector2(0f, top);
            back.rectTransform.anchorMax = new Vector2(1f, bottom);
            back.rectTransform.offsetMin = back.rectTransform.offsetMax = Vector2.zero;
            if (art == null || art.settingsSliderTrack == null)
            {
                var tan = UIFactory.Image(back.transform, "Inner", null, TrackTan);
                UIFactory.Stretch(tan.rectTransform);
                tan.rectTransform.offsetMin = new Vector2(5f, 5f);
                tan.rectTransform.offsetMax = new Vector2(-5f, -5f);
            }

            // 채움
            var fillArea = UIFactory.Rect(area, "Fill Area");
            fillArea.anchorMin = new Vector2(inX, inTop);
            fillArea.anchorMax = new Vector2(1f - inX, inBottom);
            fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
            var fill = UIFactory.Image(fillArea, "Fill", art != null ? art.settingsSliderFill : null, FillRed);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            // 손잡이 (시안 45x61)
            var handleArea = UIFactory.Rect(area, "Handle Slide Area");
            handleArea.anchorMin = new Vector2(inX, 0f);
            handleArea.anchorMax = new Vector2(1f - inX, 1f);
            handleArea.offsetMin = handleArea.offsetMax = Vector2.zero;
            var handle = UIFactory.Image(handleArea, "Handle", art != null ? art.settingsHandle : null, new Color(0.99f, 0.94f, 0.82f), raycast: true);
            handle.preserveAspect = true;
            handle.rectTransform.anchorMin = new Vector2(0f, 0.5f - 30.5f / h);
            handle.rectTransform.anchorMax = new Vector2(0f, 0.5f + 30.5f / h);
            handle.rectTransform.sizeDelta = new Vector2(45f * PanelWidth / W, 0f);
            if (art == null || art.settingsHandle == null) handle.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor = Outline;

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        /// <summary>창 배경 이미지가 없을 때: 갈색 테두리 + 제목 띠 + 크림색 줄 3개</summary>
        void BuildFallbackBackground()
        {
            var content = UIFactory.Image(panel, "Content", null, Cream);
            UIFactory.AnchorTopLeft(content.rectTransform, new Rect(70f / W, 165f / H, 1243f / W, 570f / H));
            foreach (var y in new[] { 198f, 320f, 440f })
            {
                var row = UIFactory.Image(panel, "Row", null, RowCream);
                UIFactory.AnchorTopLeft(row.rectTransform, new Rect(100f / W, y / H, 1182f / W, 110f / H));
            }
        }
    }
}
