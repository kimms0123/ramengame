using RamenTycoon.Core;
using RamenTycoon.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 엔딩 화면 (그래픽 시안 Slide 5, 6): 검은 배경 + 위쪽 그림 + 아래쪽 회색 대화 상자.
    /// 글자가 한 글자씩 나오고, Enter / Space / 클릭으로 다음 페이지. 마지막 페이지 뒤에는 타이틀로.
    /// 이미지: GameArtConfig.endingGood / endingNormal / endingBad (없으면 그림 없이 대화 상자만)
    /// </summary>
    public class EndingScreen : MonoBehaviour
    {
        static readonly Rect ImageRect = new Rect(0.2432f, 0.0806f, 0.5135f, 0.512f);
        static readonly Rect BoxRect = new Rect(0.15f, 0.6528f, 0.7005f, 0.2833f);

        [SerializeField] float charsPerSecond = 40f;

        RectTransform root;
        Image picture;
        TextMeshProUGUI body;
        Behaviour arrow; // 그림(▼ 이미지) 또는 TMP 글자
        string[] pages;
        int page;
        float shown;
        bool active;
        int openedFrame;

        void Awake()
        {
            var layer = UIRoot.Instance.PanelLayer;
            var black = UIFactory.Image(layer, "Ending", null, Color.black, raycast: true);
            root = black.rectTransform;
            UIFactory.Stretch(root);
            var click = black.gameObject.AddComponent<Button>();
            click.transition = Selectable.Transition.None;
            click.onClick.AddListener(Advance);

            picture = UIFactory.Image(root, "Picture", null, Color.clear);
            UIFactory.AnchorTopLeft(picture.rectTransform, ImageRect);
            picture.preserveAspect = true;

            var art = GameDatabase.Art;
            var box = UIFactory.Image(root, "TextBox", art != null ? art.endingTextBox : null, new Color(0.22f, 0.22f, 0.22f, 1f));
            UIFactory.AnchorTopLeft(box.rectTransform, BoxRect);

            body = UIFactory.Text(box.transform, "Body", "", 38, Color.white, TextAnchor.UpperLeft);
            UIFactory.Stretch(body.rectTransform);
            body.rectTransform.offsetMin = new Vector2(110f, 40f);
            body.rectTransform.offsetMax = new Vector2(-90f, -40f);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 15f;

            if (art != null && art.endingArrow != null)
            {
                var img = UIFactory.Image(box.transform, "Arrow", art.endingArrow, Color.white);
                img.preserveAspect = true;
                UIFactory.Place(img.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 20f), new Vector2(36f, 28f));
                arrow = img;
            }
            else
            {
                var t = UIFactory.Text(box.transform, "Arrow", "▼", 30, Color.white, TextAnchor.LowerRight);
                UIFactory.Stretch(t.rectTransform);
                t.rectTransform.offsetMin = new Vector2(0f, 20f);
                t.rectTransform.offsetMax = new Vector2(-28f, 0f);
                arrow = t;
            }

            root.gameObject.SetActive(false);
        }

        void Update()
        {
            var dm = DayManager.Instance;
            if (!active)
            {
                if (dm != null && dm.Phase == DayPhase.Ending) Show(dm.Ending, dm.EndingByLowSatisfaction);
                return;
            }

            if (shown < CurrentLength) shown += charsPerSecond * Time.unscaledDeltaTime;
            body.text = Visible(pages[page], Mathf.FloorToInt(shown));
            arrow.enabled = shown >= CurrentLength && Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f;

            var kb = Keyboard.current;
            if (kb != null && Time.frameCount != openedFrame &&
                (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                Advance();
        }

        int CurrentLength => pages[page].Length;

        void Show(EndingType type, bool lowSatisfaction)
        {
            var art = GameDatabase.Art;
            Sprite pic = null;
            if (art != null)
            {
                pic = type switch
                {
                    EndingType.Gourmet => art.endingGood,
                    EndingType.Normal => art.endingNormal != null ? art.endingNormal : art.titleBackground,
                    _ => art.endingBad
                };
            }
            picture.sprite = pic;
            picture.color = pic != null ? Color.white : Color.clear;

            pages = EndingScripts.For(type, lowSatisfaction);
            page = 0;
            shown = 0f;
            active = true;
            openedFrame = Time.frameCount;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
        }

        void Advance()
        {
            if (!active) return;
            if (shown < CurrentLength)
            {
                shown = CurrentLength; // 아직 글자가 나오는 중이면 먼저 전부 보여줌
                return;
            }

            page++;
            shown = 0f;
            if (page >= pages.Length)
            {
                active = false;
                if (DayManager.Instance != null) DayManager.Instance.GoToTitle();
            }
        }

        /// <summary>리치 텍스트 태그는 글자 수에서 빼고 n글자까지만 보이게 (태그는 항상 닫히도록)</summary>
        static string Visible(string text, int n)
        {
            if (n >= text.Length) return text;
            var sb = new System.Text.StringBuilder();
            int count = 0;
            bool openBold = false;
            for (int i = 0; i < text.Length && count < n; i++)
            {
                if (text[i] == '<')
                {
                    int end = text.IndexOf('>', i);
                    if (end > i)
                    {
                        string tag = text.Substring(i, end - i + 1);
                        if (tag == "<b>") openBold = true;
                        else if (tag == "</b>") openBold = false;
                        sb.Append(tag);
                        i = end;
                        continue;
                    }
                }
                sb.Append(text[i]);
                count++;
            }
            if (openBold) sb.Append("</b>");
            return sb.ToString();
        }
    }
}
