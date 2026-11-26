using System.Text;
using RamenTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 하루 사이 화면.
    /// - 영업 준비(Prep): 시안(Slide 7)처럼 검은 화면에 "DAY N" → 잠시 후 사라지며 영업 시작
    /// - 정산(Settlement): 하루 결산 패널 → [다음 날] / [최종 정산]
    /// 엔딩은 EndingScreen이 따로 맡는다.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class DayPanels : BakedView
    {
        [Header("DAY 전환")]
        [Tooltip("\"DAY N\" 글자가 떠 있는 시간 (초)")]
        [SerializeField] float holdSeconds = 1.4f;
        [Tooltip("검은 화면이 사라지는 시간 (초)")]
        [SerializeField] float fadeSeconds = 0.6f;

        [Header("씬에 만들어 둔 UI (빌더가 연결, 지우지 마세요)")]
        [SerializeField] Image black;
        [SerializeField] TextMeshProUGUI dayText;
        [SerializeField] TextMeshProUGUI daySub;
        [SerializeField] RectTransform dim;
        [SerializeField] TextMeshProUGUI title;
        [SerializeField] TextMeshProUGUI body;
        [SerializeField] Button primaryButton;
        [SerializeField] TextMeshProUGUI primaryLabel;

        float transitionTimer = -1f;

        DayPhase? shownPhase;
        int shownDay = -1;
        int lastPressFrame = -1;

        protected override bool IsIntact() =>
            black != null && dayText != null && daySub != null && dim != null && title != null && body != null && primaryButton != null && primaryLabel != null;

        protected override void Awake()
        {
            base.Awake();
            // 씬에서 편집하려고 켜 둔 채로 저장해도 시작할 때는 숨긴다
            dim.gameObject.SetActive(false);
            black.gameObject.SetActive(false);
        }

        protected override void Bind()
        {
            primaryButton.onClick.AddListener(OnPrimary);
        }

        protected override void Build()
        {
            var art = GameDatabase.Art;
            var layer = Root.PanelLayer;

            // ── 정산 패널 ──
            var dimImg = Track(UIFactory.Image(layer, "Dim (정산)", null, new Color(0f, 0f, 0f, 0.6f), raycast: true));
            dim = dimImg.rectTransform;
            UIFactory.Stretch(dim);

            var panel = UIFactory.Image(dim, "Panel", art != null ? art.panelBackground : null, UIFactory.Panel);
            UIArtSlot.Attach(panel, nameof(art.panelBackground), UIFactory.Panel);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 680f));
            Color textColor = art != null && art.panelBackground != null ? UIFactory.Ink : Color.white;

            title = UIFactory.Text(panel.transform, "Title", "", 64, textColor, TextAnchor.UpperCenter);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(840f, 90f));
            body = UIFactory.Text(panel.transform, "Body", "", 32, textColor, TextAnchor.UpperLeft);
            UIFactory.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 380f));
            body.lineSpacing = 15f;

            var primary = UIFactory.Button(panel.transform, "Primary", "다음 날", null, art != null ? art.buttonSprite : null, new Vector2(340f, 90f), 36);
            UIArtSlot.Attach((Image)primary.targetGraphic, nameof(art.buttonSprite), UIFactory.Accent);
            primaryButton = primary;
            UIFactory.Place((RectTransform)primary.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(340f, 90f));
            primaryLabel = primary.GetComponentInChildren<TextMeshProUGUI>();
            dim.gameObject.SetActive(false);

            // ── DAY 전환 (검은 화면) ──
            black = Track(UIFactory.Image(layer, "DayTransition", null, Color.black, raycast: true));
            UIFactory.Stretch(black.rectTransform);
            dayText = UIFactory.Text(black.transform, "Day", "DAY 1", 130, Color.white, TextAnchor.MiddleCenter);
            dayText.fontStyle = FontStyles.Bold;
            UIFactory.Stretch(dayText.rectTransform);
            daySub = UIFactory.Text(black.transform, "Sub", "", 30, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter);
            UIFactory.Place(daySub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(1200f, 50f));
            black.gameObject.SetActive(false);
        }

        void Update()
        {
            var dm = DayManager.Instance;
            if (dm == null) return;

            if (shownPhase != dm.Phase || shownDay != dm.Day)
            {
                shownPhase = dm.Phase;
                shownDay = dm.Day;
                Refresh(dm);
            }

            UpdateTransition(dm);

            var kb = Keyboard.current;
            if (dim.gameObject.activeSelf && !SettingsPanel.IsOpen && kb != null &&
                (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                OnPrimary();
        }

        void Refresh(DayManager dm)
        {
            dim.gameObject.SetActive(false);

            switch (dm.Phase)
            {
                case DayPhase.Prep:
                    StartTransition(dm);
                    break;

                case DayPhase.Settlement:
                    ShowSettlement(dm);
                    break;

                case DayPhase.Ending:
                    // 이어하기로 바로 엔딩에 들어온 경우 DAY 전환 화면을 끈다
                    transitionTimer = -1f;
                    black.gameObject.SetActive(false);
                    break;
            }
        }

        // ───────── DAY 전환 ─────────

        void StartTransition(DayManager dm)
        {
            var cfg = GameDatabase.Days;
            var s = cfg != null ? cfg.Get(dm.Day) : null;
            dayText.text = $"DAY {dm.Day}";
            daySub.text = s != null ? $"오늘의 손님 {s.customers}명  ·  임대료 {s.rent}C" : "";
            SetTransitionAlpha(1f);
            black.gameObject.SetActive(true);
            black.transform.SetAsLastSibling();
            transitionTimer = 0f;
        }

        void UpdateTransition(DayManager dm)
        {
            if (transitionTimer < 0f) return;
            if (SettingsPanel.IsOpen) return;

            transitionTimer += Time.unscaledDeltaTime;
            if (transitionTimer < holdSeconds) return;

            // 글자가 떠 있는 동안 준비 → 사라지기 시작할 때 영업 시작 (페이드 중에 화면이 보임)
            if (dm.Phase == DayPhase.Prep) dm.StartBusiness();

            float t = (transitionTimer - holdSeconds) / Mathf.Max(0.01f, fadeSeconds);
            SetTransitionAlpha(1f - t);
            if (t >= 1f)
            {
                transitionTimer = -1f;
                black.gameObject.SetActive(false);
            }
        }

        void SetTransitionAlpha(float a)
        {
            a = Mathf.Clamp01(a);
            black.color = new Color(0f, 0f, 0f, a);
            dayText.color = new Color(1f, 1f, 1f, a);
            daySub.color = new Color(1f, 1f, 1f, 0.7f * a);
            black.raycastTarget = a > 0.5f;
        }

        // ───────── 정산 ─────────

        void ShowSettlement(DayManager dm)
        {
            var cfg = GameDatabase.Days;
            var st = dm.LastSettlement;
            var sb = new StringBuilder();
            title.text = $"DAY {st.day} 정산";
            sb.AppendLine($"매출              +{st.revenue} C");
            sb.AppendLine($"재료비            -{st.ingredientCost} C");
            sb.AppendLine($"임대료            -{st.rent} C");
            if (st.upgradeCost > 0) sb.AppendLine($"업그레이드        -{st.upgradeCost} C");
            sb.AppendLine($"<b>순이익            {(st.net >= 0 ? "+" : "")}{st.net} C</b>");
            sb.AppendLine();
            sb.AppendLine($"응대한 손님      {st.served}명  (실수 {st.mistakes})");
            sb.AppendLine($"놓친 손님         {st.lost}명");
            sb.AppendLine($"만족도            {Mathf.RoundToInt(st.satisfaction)}");
            if (cfg != null) sb.AppendLine($"보유 코인         {st.coinsAfter} / {cfg.debt} C");
            if (SaveSystem.LastSaveOk)
            {
                sb.AppendLine();
                sb.Append("<size=80%><color=#7a5a3a>자동 저장되었어요 · 타이틀에서 이어하기</color></size>");
            }
            body.text = sb.ToString();
            bool last = cfg != null && dm.Day >= cfg.TotalDays;
            primaryLabel.text = last ? "최종 정산" : "다음 날";
            dim.gameObject.SetActive(true);
        }

        void OnPrimary()
        {
            // Enter 키와 버튼 Submit이 같은 프레임에 겹치면 두 단계가 넘어가지 않게 한 번만 처리
            if (lastPressFrame == Time.frameCount) return;
            lastPressFrame = Time.frameCount;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

            var dm = DayManager.Instance;
            if (dm != null && dm.Phase == DayPhase.Settlement) dm.NextDay();
        }
    }
}