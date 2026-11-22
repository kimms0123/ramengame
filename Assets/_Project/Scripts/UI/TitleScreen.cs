using RamenTycoon.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 타이틀 화면: 가게 배경 + [시작하기] [나가기] + 오른쪽 위 설정(톱니바퀴).
    /// 씬에는 GameDatabase(그림 설정용)와 이 컴포넌트만 있으면 된다. UI는 Play할 때 코드로 만든다.
    /// 버튼 위치는 그래픽 시안(Slide 3) 기준.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] string gameSceneName = "LastOrder";

        // 시안 기준 버튼 위치 (왼쪽 위 0~1, 폭/높이 비율)
        static readonly Rect StartRect = new Rect(0.4042f, 0.7056f, 0.1938f, 0.0972f);
        static readonly Rect ExitRect = new Rect(0.4042f, 0.8194f, 0.1938f, 0.0954f);

        RectTransform canvas;
        RectTransform panelLayer;

        void Awake()
        {
            GameSettings.Load();
            InputGate.GameplayBlocked = false;
            canvas = UIFactory.CreateCanvas(transform, "TitleCanvas", 10);
            UIRoot.EnsureEventSystem();
            Build();
        }

        void Build()
        {
            var art = GameDatabase.Art;

            var bg = UIFactory.Image(canvas, "Background", art != null ? art.titleBackground : null, new Color(0.35f, 0.25f, 0.35f));
            UIFactory.Stretch(bg.rectTransform);
            if (art == null || art.titleBackground == null)
            {
                var title = UIFactory.Text(canvas, "Title", "LAST ORDER", 120, Color.white, TextAnchor.MiddleCenter);
                UIFactory.AnchorTopLeft(title.rectTransform, new Rect(0f, 0.2f, 1f, 0.25f));
            }

            Sprite frame = art != null ? art.titleButton : null;
            MakeButton("StartButton", StartRect, frame, art != null ? art.titleStartIcon : null, "시작하기", StartGame);
            MakeButton("ExitButton", ExitRect, frame, art != null ? art.titleExitIcon : null, "나가기", QuitGame);

            var gear = UIFactory.Button(canvas, "SettingsButton", art != null && art.settingsIcon != null ? "" : "설정",
                OpenSettings, art != null ? art.settingsIcon : null, new Vector2(90f, 90f), 28);
            UIFactory.Place((RectTransform)gear.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(90f, 90f));
            if (art != null && art.settingsIcon != null) ((Image)gear.targetGraphic).preserveAspect = true;

            panelLayer = UIFactory.Rect(canvas, "PanelLayer");
            UIFactory.Stretch(panelLayer);
        }

        /// <summary>버튼 = 틀 이미지 + 왼쪽 아이콘 + TMP 글자 (시안 Slide 3 배치)</summary>
        void MakeButton(string objName, Rect rect, Sprite frame, Sprite icon, string label, System.Action click)
        {
            var btn = UIFactory.Button(canvas, objName, "", click, frame, Vector2.zero, 44);
            UIFactory.AnchorTopLeft((RectTransform)btn.transform, rect);
            var bg = (Image)btn.targetGraphic;
            if (frame == null) bg.color = new Color(0.99f, 0.91f, 0.78f);
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.93f, 0.8f);
            colors.pressedColor = new Color(0.85f, 0.78f, 0.68f);
            btn.colors = colors;

            if (icon != null)
            {
                var ic = UIFactory.Image(btn.transform, "Icon", icon, Color.white);
                ic.preserveAspect = true;
                UIFactory.AnchorTopLeft(ic.rectTransform, new Rect(0.08f, 0.2f, 0.2f, 0.6f));
            }

            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = label;
            text.color = UIFactory.Ink;
            text.fontStyle = TMPro.FontStyles.Bold;
            UIFactory.AnchorTopLeft(text.rectTransform, new Rect(0.28f, 0f, 0.66f, 1f));
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || SettingsPanel.IsOpen) return;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) StartGame();
        }

        void StartGame()
        {
            if (SettingsPanel.IsOpen) return;
            SceneManager.LoadScene(gameSceneName);
        }

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
