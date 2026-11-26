using RamenTycoon.Audio;
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
    /// 씬 빌더가 PanelLayer 아래에 미리 만들어 둔다(꺼진 상태) → Hierarchy에서 SettingsDim을 켜면 Scene 화면에서 편집 가능.
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

        [Header("씬에 만들어 둔 UI (빌더가 연결, 지우지 마세요)")]
        [SerializeField] RectTransform root;
        [SerializeField] RectTransform panel;
        [SerializeField] Slider bgm;
        [SerializeField] Slider sfx;
        [SerializeField] TextMeshProUGUI bgmText;
        [SerializeField] TextMeshProUGUI sfxText;
        [SerializeField] TextMeshProUGUI fullLabel;
        [SerializeField] Button checkButton;
        [SerializeField] Image check;
        [SerializeField] Button closeXButton;
        [SerializeField] Button defaultButton;
        [SerializeField] Button applyButton;
        [Tooltip("아래 오른쪽 버튼: 게임 중엔 '메인화면으로'(두 번 눌러야 이동), 메인화면에선 '닫기'")]
        [SerializeField] Button closeButton;

        [Header("아래 오른쪽 버튼")]
        [SerializeField] string toTitleLabel = "메인화면으로";
        [SerializeField] string toTitleConfirmLabel = "정말 나갈까요?";
        [SerializeField] string closeLabel = "닫기";
        [Tooltip("'정말 나갈까요?' 상태가 유지되는 시간 (초)")]
        [SerializeField] float confirmSeconds = 2.5f;

        bool fullscreen;
        bool bound;
        float quitArmTimer;
        TextMeshProUGUI quitText;
        Action onClosed;

        public static bool IsOpen { get; private set; }

        bool IsIntact() =>
            root != null && panel != null && bgm != null && sfx != null && bgmText != null && sfxText != null && fullLabel != null &&
            checkButton != null && check != null && closeXButton != null && defaultButton != null && applyButton != null && closeButton != null;

        /// <summary>parent 위에 설정 창을 띄운다 (뒤는 어둡게, 클릭 막음). 씬에 만들어 둔 창이 있으면 그걸 쓴다.</summary>
        public static SettingsPanel Open(RectTransform parent, Action closed)
        {
            if (IsOpen || parent == null) return null;
            GameSettings.Load();

            var p = parent.GetComponentInChildren<SettingsPanel>(true);
            if (p != null && !p.IsIntact())
            {
                Debug.LogWarning("[SettingsPanel] 씬의 설정 창에서 꼭 필요한 오브젝트가 지워져서 기본 모양으로 다시 만들어요.");
                UIFactory.DestroySafe(p.gameObject);
                p = null;
            }
            if (p == null) p = Create(parent);
            p.Show(closed);
            return p;
        }

        /// <summary>씬 빌더용: parent 아래에 설정 창을 새로 만들어 꺼 둔다 (기존 창은 지움)</summary>
        public static SettingsPanel Bake(RectTransform parent)
        {
            foreach (var old in parent.GetComponentsInChildren<SettingsPanel>(true)) UIFactory.DestroySafe(old.gameObject);
            var p = Create(parent);
            p.root.gameObject.SetActive(false);
            return p;
        }

        static SettingsPanel Create(RectTransform parent)
        {
            var dim = UIFactory.Image(parent, "SettingsDim (설정 창)", null, new Color(0f, 0f, 0f, 0.55f), raycast: true);
            UIFactory.Stretch(dim.rectTransform);
            var p = dim.gameObject.AddComponent<SettingsPanel>();
            p.root = dim.rectTransform;
            p.Build();
            return p;
        }

        void Show(Action closed)
        {
            onClosed = closed;
            BindOnce();
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            ResetQuit(); // 버튼 글자: 게임 중 '메인화면으로' / 메인화면 '닫기'
            AudioManager.Play("ui_open");
            SetValues(GameSettings.Bgm, GameSettings.Sfx, GameSettings.Fullscreen);
            IsOpen = true;
        }

        void BindOnce()
        {
            if (bound) return;
            bound = true;
            bgm.onValueChanged.AddListener(_ => RefreshTexts());
            sfx.onValueChanged.AddListener(_ => RefreshTexts());
            checkButton.onClick.AddListener(() => { fullscreen = !fullscreen; RefreshTexts(); });
            closeXButton.onClick.AddListener(Close);
            defaultButton.onClick.AddListener(() => SetValues(GameSettings.DefaultBgm, GameSettings.DefaultSfx, GameSettings.DefaultFullscreen));
            applyButton.onClick.AddListener(() => GameSettings.Apply(bgm.value, sfx.value, fullscreen));
            closeButton.onClick.AddListener(OnBottomButton);
            quitText = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        void Build()
        {
            var art = GameDatabase.Art;

            // 배경
            Sprite bg = art != null ? art.settingsPanel : null;
            var panelImg = UIFactory.Image(root, "Panel", bg, PanelBrown, raycast: true);
            UIFactory.Place(panelImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, PanelWidth * H / W));
            panel = panelImg.rectTransform;
            var fallbackBg = bg == null ? BuildFallbackBackground() : new GameObject[0];
            UIArtSlot.Attach(panelImg, nameof(art.settingsPanel), PanelBrown, fallbackBg);

            // 제목 줄
            Picture(art != null ? art.settingsTitleIcon : null, TitleIcon, "TitleIcon", nameof(art.settingsTitleIcon));
            var title = Label(TitleText, "설정", 60, TextAnchor.MiddleCenter, Cream);
            title.characterSpacing = 4f;
            closeXButton = ImageButton(BtnX, art != null ? art.settingsButtonX : null, nameof(art.settingsButtonX), new Color(0.73f, 0.27f, 0.17f), "×", Cream, 56);

            // 항목 3줄
            Picture(art != null ? art.settingsSoundIcon : null, Icon1, "SoundIcon1", nameof(art.settingsSoundIcon));
            Picture(art != null ? art.settingsSoundIcon : null, Icon2, "SoundIcon2", nameof(art.settingsSoundIcon));
            Picture(art != null ? art.settingsScreenIcon : null, Icon3, "ScreenIcon", nameof(art.settingsScreenIcon));
            Label(Label1, "배경음", 46, TextAnchor.MiddleLeft, Ink);
            Label(Label2, "효과음", 46, TextAnchor.MiddleLeft, Ink);
            Label(Label3, "전체 화면", 46, TextAnchor.MiddleLeft, Ink);

            bgm = CreateSlider(Track1, art);
            sfx = CreateSlider(Track2, art);
            bgmText = Label(Pct1, "", 42, TextAnchor.MiddleCenter, Ink);
            sfxText = Label(Pct2, "", 42, TextAnchor.MiddleCenter, Ink);

            // 전체 화면 체크
            var boxImg = Picture(art != null ? art.settingsCheckbox : null, CheckBox, "CheckBox", nameof(art.settingsCheckbox), fallback: new Color(1f, 0.96f, 0.86f));
            boxImg.raycastTarget = true;
            if (art == null || art.settingsCheckbox == null) boxImg.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor = Outline;
            checkButton = boxImg.gameObject.AddComponent<Button>();
            checkButton.targetGraphic = boxImg;
            check = UIFactory.Image(boxImg.transform, "Check", art != null ? art.settingsCheck : null, FillRed);
            check.preserveAspect = true;
            UIFactory.Stretch(check.rectTransform);
            if (art == null || art.settingsCheck == null)
            {
                check.rectTransform.offsetMin = new Vector2(12f, 12f);
                check.rectTransform.offsetMax = new Vector2(-12f, -12f);
            }
            UIArtSlot.Attach(check, nameof(art.settingsCheck), FillRed);
            fullLabel = Label(CheckLabel, "", 42, TextAnchor.MiddleLeft, Ink);

            // 아래 버튼 3개
            defaultButton = ImageButton(BtnDefault, art != null ? art.settingsButtonDefault : null, nameof(art.settingsButtonDefault), new Color(0.86f, 0.6f, 0.36f), "기본값", Ink, 44);
            applyButton = ImageButton(BtnApply, art != null ? art.settingsButtonApply : null, nameof(art.settingsButtonApply), new Color(0.69f, 0.42f, 0.26f), "적용", Ink, 44);
            closeButton = ImageButton(BtnClose, art != null ? art.settingsButtonClose : null, nameof(art.settingsButtonClose), new Color(0.73f, 0.27f, 0.17f), toTitleLabel, Cream, 40);

            SetValues(GameSettings.Bgm, GameSettings.Sfx, GameSettings.Fullscreen);
        }

        /// <summary>게임 씬인지 (메인화면이면 false)</summary>
        static bool InGame => DayManager.Instance != null;

        /// <summary>
        /// 게임 중: 메인화면으로 (진행이 사라지므로 두 번 눌러야 이동)
        /// 메인화면: 설정 창 닫기 (게임 종료는 메인화면의 '나가기' 버튼)
        /// </summary>
        void OnBottomButton()
        {
            if (!InGame)
            {
                Close();
                return;
            }
            if (quitArmTimer <= 0f)
            {
                quitArmTimer = confirmSeconds;
                if (quitText != null) quitText.text = toTitleConfirmLabel;
                return;
            }

            // 창을 닫는 콜백(일시정지 해제 등)은 부르지 않고 바로 메인화면으로
            IsOpen = false;
            onClosed = null;
            root.gameObject.SetActive(false);
            DayManager.Instance.GoToTitle();
        }

        void ResetQuit()
        {
            quitArmTimer = 0f;
            if (quitText != null) quitText.text = InGame ? toTitleLabel : closeLabel;
        }

        void Update()
        {
            if (quitArmTimer > 0f)
            {
                quitArmTimer -= Time.unscaledDeltaTime;
                if (quitArmTimer <= 0f) ResetQuit();
            }

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
            ResetQuit();
            AudioManager.Play("ui_close");
            root.gameObject.SetActive(false);
            var callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        void OnDestroy()
        {
            if (root != null && root.gameObject.activeSelf) IsOpen = false;
        }

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

        Image Picture(Sprite sprite, Rect px, string name, string artField, Color? fallback = null)
        {
            var area = Area(px, name);
            var img = area.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            if (sprite != null)
            {
                UIFactory.SetSprite(img, sprite, Color.white);
                img.preserveAspect = img.type == Image.Type.Simple;
            }
            else
            {
                img.color = fallback ?? Color.clear; // 아이콘이 없으면 비워 둠
            }
            UIArtSlot.Attach(img, artField, fallback ?? Color.clear);
            return img;
        }

        Button ImageButton(Rect px, Sprite sprite, string artField, Color fallback, string label, Color labelColor, float sizePx)
        {
            var area = Area(px, "Button_" + label);
            var img = area.gameObject.AddComponent<Image>();
            UIFactory.SetSprite(img, sprite, fallback);
            if (sprite == null) area.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor = Outline;
            UIArtSlot.Attach(img, artField, fallback);
            var btn = area.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var t = UIFactory.Text(area, "Label", label, Size(sizePx), labelColor, TextAnchor.MiddleCenter);
            UIFactory.Stretch(t.rectTransform);
            t.fontStyle = FontStyles.Bold;
            return btn;
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
            GameObject tanGo = null;
            if (art == null || art.settingsSliderTrack == null)
            {
                var tan = UIFactory.Image(back.transform, "Inner (그림 없을 때만)", null, TrackTan);
                UIFactory.Stretch(tan.rectTransform);
                tan.rectTransform.offsetMin = new Vector2(5f, 5f);
                tan.rectTransform.offsetMax = new Vector2(-5f, -5f);
                tanGo = tan.gameObject;
            }
            UIArtSlot.Attach(back, nameof(art.settingsSliderTrack), Outline, tanGo);

            // 채움
            var fillArea = UIFactory.Rect(area, "Fill Area");
            fillArea.anchorMin = new Vector2(inX, inTop);
            fillArea.anchorMax = new Vector2(1f - inX, inBottom);
            fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
            var fill = UIFactory.Image(fillArea, "Fill", art != null ? art.settingsSliderFill : null, FillRed);
            UIArtSlot.Attach(fill, nameof(art.settingsSliderFill), FillRed);
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
            UIArtSlot.Attach(handle, nameof(art.settingsHandle), new Color(0.99f, 0.94f, 0.82f));

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        /// <summary>창 배경 이미지가 없을 때: 갈색 테두리 + 제목 띠 + 크림색 줄 3개</summary>
        GameObject[] BuildFallbackBackground()
        {
            var list = new System.Collections.Generic.List<GameObject>();
            var content = UIFactory.Image(panel, "Content (그림 없을 때만)", null, Cream);
            UIFactory.AnchorTopLeft(content.rectTransform, new Rect(70f / W, 165f / H, 1243f / W, 570f / H));
            list.Add(content.gameObject);
            foreach (var y in new[] { 198f, 320f, 440f })
            {
                var row = UIFactory.Image(panel, "Row (그림 없을 때만)", null, RowCream);
                UIFactory.AnchorTopLeft(row.rectTransform, new Rect(100f / W, y / H, 1182f / W, 110f / H));
                list.Add(row.gameObject);
            }
            return list.ToArray();
        }
    }
}