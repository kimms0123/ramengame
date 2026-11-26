using RamenTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 인게임 HUD (그래픽 시안 기준).
    /// - 위쪽 가운데 나무 바: "DAY 1 | 3분 29초" + 코인 "130 / 400 C" + 만족도
    /// - 오른쪽 위: 설정(톱니바퀴) → 설정 창 (열려 있는 동안 게임 멈춤)
    /// - 오른쪽: 주문서(빌지) 항상 표시
    /// - 아래: 조작 안내
    /// 이미지: GameArtConfig.hudBar / coinIcon / heartIcon / settingsIcon
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class HudView : BakedView
    {
        const float BarWidth = 940f;

        [Header("씬에 만들어 둔 UI (빌더가 연결, 지우지 마세요)")]
        [SerializeField] TextMeshProUGUI dayText;
        [SerializeField] TextMeshProUGUI coinText;
        [SerializeField] TextMeshProUGUI satText;
        [SerializeField] Image satIcon;
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] OrderMemoView memo;
        [SerializeField] Button settingsButton;

        [Header("만족도 얼굴")]
        [Tooltip("만족도가 이 값 이상이면 웃는 얼굴")]
        [SerializeField] float happyAbove = 70f;
        [Tooltip("만족도가 이 값 미만이면 찡그린 얼굴 (그 사이는 보통 얼굴)")]
        [SerializeField] float sadBelow = 40f;
        [Tooltip("얼굴 옆에 숫자도 보여줄지")]
        [SerializeField] bool showSatisfactionNumber = true;
        [Tooltip("비워 두면 GameArtConfig의 Satisfaction High/Mid/Low 사용")]
        [SerializeField] Sprite happyFace, neutralFace, sadFace;

        [Header("색")]
        [SerializeField] Color normalTextColor = Color.white;
        [Tooltip("남은 시간이 30초 이하이거나 돈이 마이너스일 때 글자 색")]
        [SerializeField] Color warningTextColor = new(1f, 0.55f, 0.45f);

        bool pausedBySettings;

        protected override bool IsIntact() =>
            dayText != null && coinText != null && satText != null && satIcon != null && statusText != null && memo != null && settingsButton != null;

        protected override void Bind()
        {
            settingsButton.onClick.AddListener(OpenSettings);
        }

        protected override void Build()
        {
            var layer = Root.HudLayer;
            var art = GameDatabase.Art;

            // ── 나무 바 ──
            Sprite barSprite = art != null ? art.hudBar : null;
            float barHeight = barSprite != null ? BarWidth * barSprite.rect.height / barSprite.rect.width : 84f;
            var barFallback = new Color(0.38f, 0.23f, 0.13f, 0.92f);
            var bar = Track(UIFactory.Image(layer, "TopBar", barSprite, barFallback));
            UIArtSlot.Attach(bar, nameof(art.hudBar), barFallback);
            UIFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, barSprite != null ? 14f : -10f), new Vector2(BarWidth, barHeight));

            // 바 안쪽 갈색 영역(이미지 기준 위에서 33%~73%)에 글자를 놓는다
            var inner = UIFactory.Rect(bar.transform, "Inner");
            if (barSprite != null) UIFactory.AnchorTopLeft(inner, new Rect(0.05f, 0.33f, 0.93f, 0.40f));
            else UIFactory.Stretch(inner);

            dayText = UIFactory.Text(inner, "DayTime", "DAY 1  │  4분 30초", 34, Color.white, TextAnchor.MiddleLeft);
            dayText.fontStyle = FontStyles.Bold;
            UIFactory.AnchorTopLeft(dayText.rectTransform, new Rect(0.07f, 0f, 0.45f, 1f));

            var coin = UIFactory.Image(inner, "CoinIcon", art != null ? art.coinIcon : null, new Color(1f, 0.8f, 0.25f));
            coin.preserveAspect = true;
            UIArtSlot.Attach(coin, nameof(art.coinIcon), new Color(1f, 0.8f, 0.25f));
            UIFactory.AnchorTopLeft(coin.rectTransform, new Rect(0.53f, 0.12f, 0.045f, 0.76f));
            coinText = UIFactory.Text(inner, "Coins", "60 / 400 C", 34, Color.white, TextAnchor.MiddleLeft);
            coinText.fontStyle = FontStyles.Bold;
            UIFactory.AnchorTopLeft(coinText.rectTransform, new Rect(0.59f, 0f, 0.25f, 1f));

            // 만족도 얼굴 (그림은 Update에서 만족도에 따라 바꿈)
            Sprite face = art != null ? (art.satisfactionMid != null ? art.satisfactionMid : art.heartIcon) : null;
            satIcon = UIFactory.Image(inner, "SatisfactionFace", face, new Color(1f, 0.8f, 0.3f));
            satIcon.preserveAspect = true;
            UIFactory.AnchorTopLeft(satIcon.rectTransform, new Rect(0.848f, 0.06f, 0.045f, 0.88f));
            satText = UIFactory.Text(inner, "Satisfaction", "70", 28, Color.white, TextAnchor.MiddleLeft);
            UIFactory.AnchorTopLeft(satText.rectTransform, new Rect(0.895f, 0f, 0.1f, 1f));

            // ── 설정 버튼 ──
            Sprite gearSprite = art != null ? art.settingsIcon : null;
            var gear = Track(UIFactory.Button(layer, "SettingsButton", gearSprite != null ? "" : "설정", null, gearSprite, new Vector2(80f, 80f), 26));
            UIFactory.Place((RectTransform)gear.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(80f, 80f));
            ((Image)gear.targetGraphic).preserveAspect = true;
            UIArtSlot.Attach((Image)gear.targetGraphic, nameof(art.settingsIcon), UIFactory.Accent,
                gearSprite == null ? gear.GetComponentInChildren<TextMeshProUGUI>().gameObject : null); // 톱니 그림이 들어오면 "설정" 글자 숨김
            settingsButton = gear;

            // ── 상태 문구 / 조작 안내 ──
            statusText = Track(UIFactory.Text(layer, "Status", "", 30, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleCenter));
            UIFactory.Place(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(barHeight + 4f)), new Vector2(900f, 44f));
            UIFactory.SetOutline(statusText, new Color(0f, 0f, 0f, 0.8f));

            var hint = Track(UIFactory.Text(layer, "Hint", "WASD 이동    E / Space 상호작용    Esc 설정    F1 배속", 26, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter));
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(1200f, 40f));
            UIFactory.SetOutline(hint, new Color(0f, 0f, 0f, 0.8f));

            // ── 주문서 (시안: 화면 오른쪽, 홀 위에 항상 표시) ──
            memo = Track(OrderMemoView.Create(layer, new Vector2(0.745f, 0.21f), 310f, "HudOrderMemo")); // 시안처럼 홀 오른쪽 안쪽
        }

        void Update()
        {
            var dm = DayManager.Instance;
            var eco = Economy.Instance;
            var cfg = GameDatabase.Days;

            if (dm != null)
            {
                int sec = Mathf.CeilToInt(dm.TimeLeft);
                string time = sec >= 60 ? $"{sec / 60}분 {sec % 60:00}초" : $"{sec}초";
                dayText.text = $"DAY {dm.Day}  │  {time}";
                dayText.color = dm.Phase == DayPhase.Business && sec <= 30 ? warningTextColor : normalTextColor;

                string status = "";
                if (dm.Phase == DayPhase.Closing) status = "영업 종료 — 남은 손님을 마저 응대하세요";
                else if (dm.Phase == DayPhase.Business && dm.WaitingOutside > 0) status = $"밖에서 기다리는 손님 {dm.WaitingOutside}명";
                statusText.text = status;
            }

            if (eco != null)
            {
                int debt = cfg != null ? cfg.debt : 400;
                coinText.text = $"{eco.Coins} / {debt} C";
                coinText.color = eco.Coins < 0 ? warningTextColor : normalTextColor;
                satText.text = Mathf.RoundToInt(eco.Satisfaction).ToString();
                if (satText.gameObject.activeSelf != showSatisfactionNumber) satText.gameObject.SetActive(showSatisfactionNumber);
                UpdateFace(eco.Satisfaction);
            }

            // 영업 준비/정산 패널, 고명 화면, 설정 창이 떠 있을 때는 주문서 숨김
            bool showMemo = !InputGate.GameplayBlocked;
            if (memo.gameObject.activeSelf != showMemo) memo.gameObject.SetActive(showMemo);

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && UIFactory.EscConsumedFrame != Time.frameCount
                && !SettingsPanel.IsOpen && CanOpenSettings())
            {
                UIFactory.EscConsumedFrame = Time.frameCount;
                OpenSettings();
            }
        }

        void UpdateFace(float satisfaction)
        {
            var art = GameDatabase.Art;
            Sprite high = happyFace != null ? happyFace : art != null ? art.satisfactionHigh : null;
            Sprite mid = neutralFace != null ? neutralFace : art != null ? art.satisfactionMid : null;
            Sprite low = sadFace != null ? sadFace : art != null ? art.satisfactionLow : null;

            Sprite s = satisfaction >= happyAbove ? high : satisfaction < sadBelow ? low : mid;
            if (s == null) s = mid != null ? mid : art != null ? art.heartIcon : null;
            if (s != null && satIcon.sprite != s)
            {
                satIcon.sprite = s;
                satIcon.color = Color.white;
            }
        }

        bool CanOpenSettings()
        {
            var dm = DayManager.Instance;
            bool screenOpen = (ToppingScreen.Instance != null && ToppingScreen.Instance.IsOpen) || UpgradePanel.IsOpen;
            return !screenOpen && (dm == null || dm.Phase == DayPhase.Business || dm.Phase == DayPhase.Closing);
        }

        void OpenSettings()
        {
            if (SettingsPanel.IsOpen || !CanOpenSettings()) return;

            // 설정 창이 떠 있는 동안 게임을 멈춘다
            pausedBySettings = !InputGate.GameplayBlocked;
            if (pausedBySettings)
            {
                InputGate.GameplayBlocked = true;
                if (GameClock.Instance != null) GameClock.Instance.Paused = true;
            }
            SettingsPanel.Open(Root.PanelLayer, OnSettingsClosed);
        }

        void OnSettingsClosed()
        {
            if (!pausedBySettings) return;
            pausedBySettings = false;
            InputGate.GameplayBlocked = false;
            if (GameClock.Instance != null) GameClock.Instance.Paused = false;
        }
    }
}