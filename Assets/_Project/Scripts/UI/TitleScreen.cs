using TMPro;
using RamenTycoon.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 타이틀 화면: 가게 배경 + [이어하기] [시작하기] [나가기] + 오른쪽 위 설정(톱니바퀴).
    /// [이어하기]는 자동 저장 파일이 없으면 흐리게 막혀 있다. 저장이 있는데 [시작하기]를 누르면 한 번 더 확인한다.
    /// 씬 빌더가 UI를 씬에 미리 만들어 둔다 → TitleCanvas 아래 오브젝트를 Scene 화면에서 직접 옮기고 고칠 수 있다.
    /// 버튼 위치 기본값은 그래픽 시안(Slide 3) 기준.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class TitleScreen : BakedView
    {
        [Tooltip("시작하기를 누르면 열 씬 이름 (File > Build Profiles의 씬 목록에 있어야 함)")]
        [SerializeField] string gameSceneName = "LastOrder";

        [Tooltip("로고 그림(title_logo)이 없을 때 'LAST ORDER' 글자를 띄울지 (시안은 배경 그림만)")]
        [SerializeField] bool showTitleText = false;

        [Header("이어하기 / 새로 시작")]
        [SerializeField] string continueLabel = "이어하기";
        [Tooltip("이어하기 버튼 글자 뒤에 저장된 날을 붙일지 (예: 이어하기 DAY 3)")]
        [SerializeField] bool showSavedDay = true;
        [Tooltip("저장이 있을 때 시작하기를 처음 누르면 바뀌는 글자 (한 번 더 누르면 새 게임)")]
        [SerializeField] string newGameConfirmLabel = "새로 할까요?";
        [SerializeField] float confirmSeconds = 3f;
        [Tooltip("저장이 없을 때 이어하기 버튼 틀에 곱해지는 색 (회색일수록 어둡게)")]
        [SerializeField] Color lockedFrameTint = new(0.72f, 0.69f, 0.66f);
        [Tooltip("저장이 없을 때 이어하기 글자·아이콘 색")]
        [SerializeField] Color lockedContentColor = new(0.5f, 0.46f, 0.43f);

        [Tooltip("비어 있으면 Play 때 시작하기 버튼을 복사해서 그 위에 만든다. 씬에 미리 만들려면 ⋮ > 이어하기 버튼 만들기")]
        [SerializeField] Button continueButton;

        [Header("씬에 만들어 둔 UI (빌더가 연결, 지우지 마세요)")]
        [SerializeField] RectTransform canvas;
        [SerializeField] RectTransform panelLayer;
        [SerializeField] Button startButton;
        [SerializeField] Button exitButton;
        [SerializeField] Button settingsButton;

        // 시안 기준 버튼 위치 (왼쪽 위 0~1, 폭/높이 비율)
        static readonly Rect StartRect = new Rect(0.4042f, 0.7056f, 0.1938f, 0.0972f);
        static readonly Rect ExitRect = new Rect(0.4042f, 0.8194f, 0.1938f, 0.0954f);
        static readonly Rect ContinueRect = new Rect(0.4042f, 0.5918f, 0.1938f, 0.0972f);

        SaveData save;
        float confirmTimer;
        string startLabelText;
        TextMeshProUGUI startLabel;

        protected override void Awake()
        {
            GameSettings.Load();
            InputGate.GameplayBlocked = false;
            UIRoot.EnsureEventSystem();
            base.Awake();
        }

        protected override bool IsIntact() =>
            canvas != null && panelLayer != null && startButton != null && exitButton != null && settingsButton != null;

        protected override void Bind()
        {
            save = SaveSystem.Peek();
            if (continueButton == null) continueButton = CloneAbove(startButton, exitButton, "ContinueButton");
            continueButton.onClick.AddListener(ContinueGame);
            SetContinueAvailable(save != null);

            startLabel = Label(startButton);
            if (startLabel != null) startLabelText = startLabel.text;

            startButton.onClick.AddListener(StartGame);
            exitButton.onClick.AddListener(QuitGame);
            settingsButton.onClick.AddListener(OpenSettings);
        }

        /// <summary>설정 창이 붙을 레이어 (씬 빌더가 설정 창을 미리 만들 때 사용)</summary>
        public RectTransform PanelLayer => panelLayer;

        protected override void Build()
        {
            var art = GameDatabase.Art;
            canvas = Track(UIFactory.CreateCanvas(transform, "TitleCanvas", 10));

            var bgColor = new Color(0.35f, 0.25f, 0.35f);
            var bg = UIFactory.Image(canvas, "Background", art != null ? art.titleBackground : null, bgColor);
            UIFactory.Stretch(bg.rectTransform);
            UIArtSlot.Attach(bg, nameof(art.titleBackground), bgColor);
            // 로고: 그림이 있으면 그림, 없으면 TMP 글자
            var logoRect = new Rect(0.2f, 0.06f, 0.6f, 0.2f);
            if (art != null && art.titleLogo != null)
            {
                var logo = UIFactory.Image(canvas, "TitleLogo", art.titleLogo, Color.white);
                logo.preserveAspect = true;
                UIFactory.AnchorTopLeft(logo.rectTransform, logoRect);
            }
            else if (showTitleText)
            {
                var title = UIFactory.Text(canvas, "Title", "LAST ORDER", 130, new Color(1f, 0.93f, 0.8f), TextAnchor.MiddleCenter);
                title.fontStyle = FontStyles.Bold;
                UIFactory.SetOutline(title, new Color(0.22f, 0.1f, 0.08f), 0.3f);
                UIFactory.AnchorTopLeft(title.rectTransform, logoRect);
            }

            Sprite frame = art != null ? art.titleButton : null;
            startButton = MakeButton("StartButton", StartRect, frame, art != null ? art.titleStartIcon : null, nameof(art.titleStartIcon), "시작하기");
            exitButton = MakeButton("ExitButton", ExitRect, frame, art != null ? art.titleExitIcon : null, nameof(art.titleExitIcon), "나가기");
            continueButton = MakeButton("ContinueButton", ContinueRect, frame, art != null ? art.titleStartIcon : null, nameof(art.titleStartIcon), continueLabel);

            bool hasGear = art != null && art.settingsIcon != null;
            var gear = UIFactory.Button(canvas, "SettingsButton", hasGear ? "" : "설정", null, art != null ? art.settingsIcon : null, new Vector2(90f, 90f), 28);
            UIFactory.Place((RectTransform)gear.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(90f, 90f));
            ((Image)gear.targetGraphic).preserveAspect = true;
            UIArtSlot.Attach((Image)gear.targetGraphic, nameof(art.settingsIcon), UIFactory.Accent,
                hasGear ? null : gear.GetComponentInChildren<TextMeshProUGUI>().gameObject);
            settingsButton = gear;

            panelLayer = UIFactory.Rect(canvas, "PanelLayer");
            UIFactory.Stretch(panelLayer);
        }

        /// <summary>버튼 = 틀 이미지 + 왼쪽 아이콘 + TMP 글자 (시안 Slide 3 배치)</summary>
        Button MakeButton(string objName, Rect rect, Sprite frame, Sprite icon, string iconField, string label)
        {
            var btn = UIFactory.Button(canvas, objName, "", null, frame, Vector2.zero, 44);
            UIFactory.AnchorTopLeft((RectTransform)btn.transform, rect);
            var bg = (Image)btn.targetGraphic;
            var frameFallback = new Color(0.99f, 0.91f, 0.78f);
            if (frame == null) bg.color = frameFallback;
            UIArtSlot.Attach(bg, "titleButton", frameFallback);
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.93f, 0.8f);
            colors.pressedColor = new Color(0.85f, 0.78f, 0.68f);
            btn.colors = colors;

            // 아이콘 칸은 그림이 없어도 만들어 둔다 (투명 → 나중에 그림을 넣으면 보임)
            var ic = UIFactory.Image(btn.transform, "Icon", icon, Color.clear);
            ic.preserveAspect = true;
            UIFactory.AnchorTopLeft(ic.rectTransform, new Rect(0.08f, 0.2f, 0.2f, 0.6f));
            UIArtSlot.Attach(ic, iconField, Color.clear);

            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = label;
            text.color = UIFactory.Ink;
            text.fontStyle = TMPro.FontStyles.Bold;
            UIFactory.AnchorTopLeft(text.rectTransform, new Rect(0.28f, 0f, 0.66f, 1f));
            return btn;
        }

        void Update()
        {
            if (confirmTimer > 0f)
            {
                confirmTimer -= Time.unscaledDeltaTime;
                if (confirmTimer <= 0f && startLabel != null) startLabel.text = startLabelText;
            }

            var kb = Keyboard.current;
            if (kb == null || SettingsPanel.IsOpen) return;
            // Enter: 저장이 있으면 이어하기, 없으면 시작하기
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                if (save != null && confirmTimer <= 0f) ContinueGame();
                else StartGame();
            }
        }

        int LastDay => GameDatabase.Days != null ? GameDatabase.Days.TotalDays : 5;

        void StartGame()
        {
            if (SettingsPanel.IsOpen) return;
            // 저장이 있으면 한 번 더 눌러야 새 게임 (저장은 덮어써짐)
            if (save != null && confirmTimer <= 0f)
            {
                confirmTimer = confirmSeconds;
                if (startLabel != null) startLabel.text = newGameConfirmLabel;
                RamenTycoon.Audio.AudioManager.Play("error", 0.4f);
                return;
            }
            SaveSystem.Delete();
            SaveSystem.PrepareNewGame();
            SceneManager.LoadScene(gameSceneName);
        }

        void ContinueGame()
        {
            if (SettingsPanel.IsOpen) return;
            if (!SaveSystem.PrepareContinue())
            {
                save = null;
                SetContinueAvailable(false);
                return;
            }
            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>저장이 있으면 누를 수 있게, 없으면 흐리게 막는다 (버튼은 항상 보임)</summary>
        Color? labelColor, iconColor;

        void SetContinueAvailable(bool available)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = available;

            // 반투명이면 뒤 배경이 비쳐서 깨져 보이므로, 불투명한 채로 회색으로만 칠한다
            var group = continueButton.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1f;
            var colors = continueButton.colors;
            colors.disabledColor = lockedFrameTint;
            continueButton.colors = colors;

            var cl = Label(continueButton);
            if (cl != null)
            {
                labelColor ??= cl.color;
                cl.color = available ? labelColor.Value : lockedContentColor;
                cl.text = available && showSavedDay ? $"{continueLabel} <size=70%>DAY {Mathf.Min(save.day, LastDay)}</size>" : continueLabel;
            }
            var icon = continueButton.transform.Find("Icon") is Transform it ? it.GetComponent<Image>() : null;
            if (icon != null && icon.sprite != null)
            {
                iconColor ??= icon.color;
                icon.color = available ? iconColor.Value : lockedContentColor;
            }
        }

        static TextMeshProUGUI Label(Button b) => b != null ? b.GetComponentInChildren<TextMeshProUGUI>(true) : null;

        /// <summary>source 버튼을 복사해서, source와 below 사이 간격만큼 위에 놓는다</summary>
        static Button CloneAbove(Button source, Button below, string objName)
        {
            var copy = Instantiate(source, source.transform.parent);
            copy.name = objName;
            copy.onClick.RemoveAllListeners();

            // 두 버튼의 실제 화면 위치(부모 기준)로 간격을 재서, below 반대쪽으로 한 칸 옮긴다.
            // 간격이 버튼 높이보다 좁아도 겹치지 않게 최소 '높이 + 15%'만큼 띄운다.
            var src = (RectTransform)source.transform;
            var low = (RectTransform)below.transform;
            var parent = src.parent;
            Vector3 srcCenter = parent.InverseTransformPoint(src.TransformPoint(src.rect.center));
            Vector3 lowCenter = parent.InverseTransformPoint(low.TransformPoint(low.rect.center));
            float height = src.rect.height * src.localScale.y;
            float step = Mathf.Max(Mathf.Abs(srcCenter.y - lowCenter.y), height * 1.15f);
            float dir = srcCenter.y >= lowCenter.y ? 1f : -1f;
            var rt = (RectTransform)copy.transform;
            rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, dir * step);
            return copy;
        }

#if UNITY_EDITOR
        [ContextMenu("이어하기 버튼 만들기 (씬에 저장해서 직접 옮길 수 있게)")]
        void EditorCreateContinueButton()
        {
            if (continueButton != null) { Debug.Log("[TitleScreen] 이미 이어하기 버튼이 있어요."); return; }
            if (startButton == null || exitButton == null) { Debug.LogWarning("[TitleScreen] 시작하기/나가기 버튼이 없어요."); return; }
            continueButton = CloneAbove(startButton, exitButton, "ContinueButton");
            var label = Label(continueButton);
            if (label != null) label.text = continueLabel;
            UnityEditor.Undo.RegisterCreatedObjectUndo(continueButton.gameObject, "Create Continue Button");
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            UnityEditor.Selection.activeGameObject = continueButton.gameObject;
        }
#endif

        void QuitGame()
        {
            if (SettingsPanel.IsOpen) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OpenSettings() => SettingsPanel.Open(panelLayer, null);
    }
}